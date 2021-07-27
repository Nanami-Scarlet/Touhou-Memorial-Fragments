using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class GameProcessMgr : MonoBehaviour
{
    private EnemySpawnMgr _enemyspawnMgr;
    private BossSpawnMgr _bossSpawnMgr;
    private EliteSpawnMgr _eliteSpawnMgr;
    private Coroutine _coroutine;

    private Animator _bgAnim;

    private Dictionary<string, int> _dicBossTypeIndex = new Dictionary<string, int>();
    private Dictionary<string, GameObject> _dicTypeBoss = new Dictionary<string, GameObject>();
    private Dictionary<string, BossView> _dicBossView = new Dictionary<string, BossView>();
    private Dictionary<string, BossController> _dicTypeBossCtl = new Dictionary<string, BossController>();
    private Dictionary<string, BossBehaviour> _dicTypeBossBH = new Dictionary<string, BossBehaviour>();
    private Dictionary<int, Action<Action, string>> _dicIDAction;
    private Queue<TimeTask> _queTimeTask = new Queue<TimeTask>();

    private bool _isTimeUP = false;
    private TimeTask _curTimeTask;
    private int _tid;

    private float _cardTimeSpan;

    public void Init()
    {
        MessageMgr.Single.AddListener(MsgEvent.EVENT_CHAT_CALLBACK, OnChatCallBack);
        MessageMgr.Single.AddListener(MsgEvent.EVENT_SET_TIMEUP, SetTimeUP);
        MessageMgr.Single.AddListener(MsgEvent.EVENT_RESTART_GAME, RestartGame);

        _dicIDAction = new Dictionary<int, Action<Action, string>>()
        {
            {
                0,
                (callBack, strArgs) =>
                {
                    _dicTypeBossCtl[strArgs].Appear(callBack);
                }
            },

            {
                1,
                (callBack, strArgs) =>
                {
                    _dicTypeBossCtl[strArgs].Appear(callBack);
                    foreach(var pair in _dicTypeBossCtl)
                    {
                        if(pair.Key.Equals(strArgs))
                        {
                            continue;
                        }

                        pair.Value.Move();
                    }
                }
            },

            {
                2,
                (callBack, strArgs) => 
                {
                    MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_SET_ENDING_PIC, strArgs);
                    callBack();
                }
            },

            {
                3,
                (callBack, strArgs) =>
                {
                    MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_BGM_SETTING, strArgs);
                    callBack();
                }
            },

            {
                4,
                (callBack, strArgs) =>
                {
                    callBack();
                    UIManager.Single.Show(strArgs);
                }
            }
        };

        _enemyspawnMgr = gameObject.AddComponent<EnemySpawnMgr>();
        _enemyspawnMgr.Init();

        _bossSpawnMgr = gameObject.AddComponent<BossSpawnMgr>();
        _bossSpawnMgr.Init();

        _eliteSpawnMgr = gameObject.AddComponent<EliteSpawnMgr>();
        _eliteSpawnMgr.Init();

        GameObject bg = GameObject.Find("BG");
        _bgAnim = bg.GetComponent<Animator>();

        _bgAnim.SetInteger("Stage", GameModel.Single.StageNum);

        _coroutine = StartCoroutine(GameProcess());
    }

    private void Update()
    {
        if(GameStateModel.Single.IsCard)
        {
            _cardTimeSpan += Time.deltaTime;
        }
    }

    private void OnDestroy()
    {
        GameModel.Single.EnemyCount = 0;

        PoolMgr.Single.ClearPool();
        TimeMgr.Single.ClearAllTask();

        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_CHAT_CALLBACK, OnChatCallBack);
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_SET_TIMEUP, SetTimeUP);
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_RESTART_GAME, RestartGame);
    }

    private IEnumerator GameProcess()
    {
        switch (GameStateModel.Single.GameMode)
        {
            case Mode.NORMAL:
            case Mode.LUNATIC:
                yield return OnStageUp("stage1_1");
                yield return OnStageBoss("stage_B1");
                yield return OnStageDown("stage1_2");
                yield return OnStageBoss("stage_B2");
                yield return PrepareNextStage();
                yield return OnStageElite("stage2_2", true);
                yield return OnStageBoss("stage_B4");
                yield return new WaitForSeconds(1);
                yield return OnStageChat("Ending");
                break;

           //todo:要加上其他的模式
        }
    }

    private IEnumerator OnStageUp(string stageName)
    {
        StageData stageData = DataMgr.Single.GetStageData(stageName);

        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_STAGE_ANIM);
        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_BGM_SETTING, stageName);

        for (int i = 0; i < stageData.ListWaveEnemy.Count; ++i)
        {
            WaveData waveData = stageData.ListWaveEnemy[i];

            for (int j = 0; j < waveData.ListEnemy.Count; ++j)
            {
                EnemyData enemyData = waveData.ListEnemy[j];

                int tid = TimeMgr.Single.GetTid();
                TimeTask timeTask = new TimeTask(tid, 0, () =>
                { 
                    _enemyspawnMgr.Spawn(enemyData);
                    ++GameModel.Single.EnemyCount;

                    _queTimeTask.Dequeue();
                    if (_queTimeTask.Count > 0)
                    {
                        _curTimeTask = _queTimeTask.Peek();

                        _tid = TimeMgr.Single.AddTimeTask(() =>
                        {
                            _tid = TimeMgr.Single.AddTimeTask(_curTimeTask);
                        }, enemyData.Delay, TimeUnit.Second);       //间隔之后才产生下一只妖精
                    }
                });

                _queTimeTask.Enqueue(timeTask);
            }
        }

        yield return new WaitForSeconds(4f);

        _curTimeTask = _queTimeTask.Peek();
        TimeMgr.Single.AddTimeTask(_curTimeTask);

        InvokeRepeating(nameof(CheckEnemyCount), 4f, 0.1f);

        yield return new WaitUntil(() => 
        {
            if(_queTimeTask.Count == 0 && GameModel.Single.EnemyCount == 0)
            {
                CancelInvoke(nameof(CheckEnemyCount));

                return true;
            }

            return false;
        });

        _queTimeTask.Clear();

        yield return new WaitForSeconds(3);
    }

    private IEnumerator OnStageDown(string stageName)
    {
        StageData stageData = DataMgr.Single.GetStageData(stageName);

        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_BGM_SETTING, stageName);

        for (int i = 0; i < stageData.ListWaveEnemy.Count; ++i)
        {
            WaveData waveData = stageData.ListWaveEnemy[i];

            for (int j = 0; j < waveData.ListEnemy.Count; ++j)
            {
                EnemyData enemyData = waveData.ListEnemy[j];

                int tid = TimeMgr.Single.GetTid();
                TimeTask timeTask = new TimeTask(tid, 0, () =>
                {
                    _enemyspawnMgr.Spawn(enemyData);
                    ++GameModel.Single.EnemyCount;

                    _queTimeTask.Dequeue();
                    if (_queTimeTask.Count > 0)
                    {
                        _curTimeTask = _queTimeTask.Peek();

                        _tid = TimeMgr.Single.AddTimeTask(() =>
                        {
                            _tid = TimeMgr.Single.AddTimeTask(_curTimeTask);
                        }, enemyData.Delay, TimeUnit.Second);       //间隔之后才产生下一只妖精
                    }
                });

                _queTimeTask.Enqueue(timeTask);
            }
        }

        _curTimeTask = _queTimeTask.Peek();
        TimeMgr.Single.AddTimeTask(_curTimeTask);

        InvokeRepeating(nameof(CheckEnemyCount), 0.5f, 0.1f);

        yield return new WaitUntil(() =>
        {
            if (_queTimeTask.Count == 0 && GameModel.Single.EnemyCount == 0)
            {
                CancelInvoke(nameof(CheckEnemyCount));

                return true;
            }

            return false;
        });

        _queTimeTask.Clear();

        yield return new WaitForSeconds(3);
    }

    private IEnumerator OnStageBoss(string stageName)
    {
        BossData bossData = DataMgr.Single.GetBossData(stageName);

        _dicBossTypeIndex.Clear();
        for(int i = 0; i < bossData.BossType.Count; ++i)
        {
            string bossType = bossData.BossType[i];
            Vector3 bornPos = bossData.BornPos[i];
            List<Vector3> appearPath = bossData.AppearPath[i];
            Vector3 finalInitPos = bossData.FinalMovePath[i];

            SingleBossInitData data = new SingleBossInitData(bossType, bornPos, appearPath, finalInitPos);
            GameObject bossGO = _bossSpawnMgr.Spawn(data);

            MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_ADD_HPBAR, bossGO);
            _dicBossTypeIndex.Add(bossType, i);
            _dicTypeBoss.Add(bossType, bossGO);
            _dicBossView.Add(bossType, bossGO.GetComponent<BossView>());
            _dicTypeBossCtl.Add(bossType, bossGO.GetComponent<BossController>());
            _dicTypeBossBH.Add(bossType, bossGO.GetComponent<BossBehaviour>());
        }

        yield return OnStageChat(stageName);

        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_BGM_SETTING, stageName);
        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_SHOW_BOSS_NAME_CARD, new BossNameCard(bossData.Name, bossData.Cards.Count));

        for (int i = 0; i < bossData.Cards.Count - 1; ++i)
        {
            CardData cardData = bossData.Cards[i];

            /////////////////////////////
            yield return OnBossNormalCard(bossData, cardData);
            yield return OnBossCard(bossData, cardData);

            MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_SHOW_BOSS_NAME_CARD, new BossNameCard(bossData.Name, bossData.Cards.Count - i - 1));
        }

        yield return OnBossFinalCard(bossData, bossData.Cards[bossData.Cards.Count - 1]);

        yield return new WaitForSeconds(2f);
    }

    private IEnumerator OnStageChat(string stageName)
    {
        UIManager.Single.Show(Paths.PREFAB_CHAT_VIEW, false);
        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_SHOW_DIALOG, stageName);

        yield return new WaitForSeconds(2f);       //预留一定的时间，防止对话没有开始，就开始战斗

        yield return new WaitUntil(() => !GameStateModel.Single.IsChating);         //对话结束
    }

    private IEnumerator OnBossNormalCard(BossData bossData, CardData cardData)
    {
        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_SET_TIMER, cardData.NormalTime);                           //非符

        for (int i = 0; i < bossData.BossType.Count; ++i)
        {
            string type = bossData.BossType[i];

            _dicTypeBossCtl[type].Move(cardData.NormalInitPos[i]);

            BaseCardHPData baseCardHPData = new BaseCardHPData(cardData.NormalHP, cardData.CardHP, cardData.BarIndex,
                _dicTypeBoss[type].transform);
            MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_SET_HPBAR, baseCardHPData);
            _dicTypeBossCtl[type].SetReceiver(false);

            for (int j = 0; j < cardData.NormalBoss.Count; ++j)
            {
                string curType = cardData.NormalBoss[j];

                if (curType.Equals(type))
                {
                    MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_SHOW_HP_VIEW, _dicTypeBoss[type]);
                    //MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_SET_HPBAR, baseCardHPData);

                    _dicBossView[type].PlayMagicBGAnim();
                }
            }

            _dicTypeBossBH[type].SetBehaviour(cardData.NormalHP + cardData.CardHP,
                cardData.NormalP, cardData.NormalPoint, cardData.NormalLife, cardData.NormalBomb);
        }

        yield return new WaitForSeconds(2f);

        for (int i = 0; i < bossData.BossType.Count; ++i)
        {
            string type = bossData.BossType[i];

            _dicTypeBossCtl[type].SetReceiver(true);

            _dicTypeBossCtl[type].ResetCard(new SingleBossCardData(cardData.NormalPath[i], cardData.NormalMoveTime[i], cardData.NormalDuration[i], 
                cardData.NormalDelay[i], cardData.NormalEmitter[i], cardData.NormalEmitterPos[i]));
        }

        yield return new WaitUntil(() =>
        {
            for (int i = 0; i < cardData.NormalBoss.Count; ++i)
            {
                string type = cardData.NormalBoss[i];

                if (_dicTypeBossBH[type].HP <= cardData.CardHP)       //在规定时间内击破
                {
                    return true;
                }
            }

            return _isTimeUP;                                       //是否超时
        });

        for (int i = 0; i < cardData.NormalBoss.Count; ++i)
        {
            string type = cardData.NormalBoss[i];

            _dicTypeBossCtl[type].StopCard();
        }

        if (!_isTimeUP)                                             //如果是击破，会有掉落物
        {
            AudioMgr.Single.PlayGameEff(AudioType.Bonus);
            for (int i = 0; i < cardData.NormalBoss.Count; ++i)
            {
                string type = cardData.NormalBoss[i];

                _dicTypeBossBH[type].SpawnItems();
                _dicBossView[type].StopMagicBGAnim();
                MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_HIDE_HP_VIEW, _dicTypeBoss[type]);
            }
        }

        _isTimeUP = false;
    }

    private IEnumerator OnBossCard(BossData bossData, CardData cardData)
    {
        for (int i = 0; i < cardData.CardBoss.Count; ++i)
        {
            string type = cardData.CardBoss[i];

            MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_SHOW_HP_VIEW, _dicTypeBoss[type]);
            _dicBossView[type].PlayMagicBGAnim();
            MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_SET_CARD_PIC, type);
        }
        AudioMgr.Single.PlayGameEff(AudioType.Card);
        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_MOVE_TIMER, -20);
        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_SET_TIMER, cardData.CardTime);
        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_PLAY_CARD_INFO_ANIM, cardData.CardName);
        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_PLAY_CARD_PIC_ANIM);

        for (int i = 0; i < bossData.BossType.Count; ++i)
        {
            string type = bossData.BossType[i];

            _dicTypeBossCtl[type].Move(cardData.CardInitPos[i]);
            _dicTypeBossCtl[type].SetReceiver(false);
        }

        yield return new WaitForSeconds(2f);
        GameModel.Single.CardBonus = cardData.CardBonus;
        GameStateModel.Single.IsCard = true;
        _cardTimeSpan = 0;

        for (int i = 0; i < cardData.CardBoss.Count; ++i)
        {
            string type = cardData.CardBoss[i];

            int index = _dicBossTypeIndex[type];
            _dicTypeBossBH[type].SetBehaviour(cardData.CardHP, cardData.CardP, cardData.CardPoint, cardData.CardLife, cardData.CardBomb);
            MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_SET_HPBAR_VIEW, new HPData(_dicTypeBoss[type], cardData.CardHP));
            _dicTypeBossCtl[type].ResetCard(new SingleBossCardData(cardData.CardPath[index], cardData.CardMoveTime[index], cardData.CardDuration[index],
                cardData.CardDelay[index], cardData.CardEmitter[index], cardData.CardEmitterPos[index]));
            _dicTypeBossCtl[type].SetReceiver(true);
        }

        yield return new WaitUntil(() =>
        {
            for (int i = 0; i < cardData.CardBoss.Count; ++i)
            {
                string type = cardData.CardBoss[i];

                if (_dicTypeBossBH[type].HP <= 0)
                {
                    return true;
                }
            }

            return _isTimeUP;
        });

        GameStateModel.Single.IsCard = false;
        GetCardInfo getCardInfo = null;

        for (int i = 0; i < cardData.CardBoss.Count; ++i)
        {
            string type = cardData.CardBoss[i];

            _dicTypeBossCtl[type].StopCard();
        }

        if (!_isTimeUP)
        {
            AudioMgr.Single.PlayGameEff(AudioType.GetBomb);
            getCardInfo = new GetCardInfo(0, GameModel.Single.CardBonus, _cardTimeSpan);

            PlayerModel.Single.MemoryProcess += UnityEngine.Random.Range(20, 30);
            MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_UPDATE_MEMORY);

            PlayerModel.Single.MAX_GET_POINT += cardData.MaxPoint;
            MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_UPDATE_POINT);

            for (int i = 0; i < cardData.CardBoss.Count; ++i)
            {
                string type = cardData.CardBoss[i];

                _dicTypeBossBH[type].SpawnItems();
            }
        }
        else
        {
            getCardInfo = new GetCardInfo(1, 0, _cardTimeSpan);

            AudioMgr.Single.PlayGameEff(AudioType.TimeUP);
        }

        GameModel.Single.Score += GameModel.Single.CardBonus;
        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_UPDATE_SCORE);
        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_PLAY_GET_CARD_INFO_ANIM, getCardInfo);
        _isTimeUP = false;

        for (int i = 0; i < bossData.BossType.Count; ++i)
        {
            string type = bossData.BossType[i];

            _dicTypeBossCtl[type].SetReceiver(false);
            _dicBossView[type].StopMagicBGAnim();
            MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_HIDE_HP_VIEW, _dicTypeBoss[type]);
        }

        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_MOVE_CARD_INFO_RIGHT);
    }

    private IEnumerator OnBossFinalCard(BossData bossData, CardData cardData)
    {
        for (int i = 0; i < cardData.CardBoss.Count; ++i)
        {
            string type = cardData.CardBoss[i];

            MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_SET_CARD_PIC, type);
            _dicBossView[type].PlayMagicBGAnim();
            MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_SHOW_HP_VIEW, _dicTypeBoss[type]);
        }
        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_MOVE_TIMER, -20);
        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_SET_TIMER, cardData.CardTime);
        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_PLAY_CARD_INFO_ANIM, cardData.CardName);
        AudioMgr.Single.PlayGameEff(AudioType.Card);
        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_PLAY_CARD_PIC_ANIM);

        for (int i = 0; i < bossData.BossType.Count; ++i)
        {
            string type = bossData.BossType[i];

            _dicTypeBossCtl[type].Move(cardData.CardInitPos[i]);
        }

        GameStateModel.Single.IsFinalCard = true;

        for (int i = 0; i < cardData.CardBoss.Count; ++i)
        {
            string type = cardData.CardBoss[i];

            _dicTypeBossBH[type].SetBehaviour(cardData.CardHP);         //符卡
            MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_SET_HPBAR, new BaseCardHPData(cardData.NormalHP, cardData.CardHP, cardData.BarIndex, _dicTypeBoss[type].transform));
        }

        yield return new WaitForSeconds(2);
        GameStateModel.Single.IsCard = true;
        _cardTimeSpan = 0;
        GameModel.Single.CardBonus = cardData.CardBonus;

        for (int i = 0; i < cardData.CardBoss.Count; ++i)
        {
            string type = cardData.CardBoss[i];

            int index = _dicBossTypeIndex[type];
            _dicTypeBossCtl[type].ResetCard(new SingleBossCardData(cardData.CardPath[index], cardData.CardMoveTime[index], cardData.CardDuration[index], 
                cardData.CardDelay[index], cardData.CardEmitter[index], cardData.CardEmitterPos[index]));
            _dicTypeBossCtl[type].SetReceiver(true);
        }

        yield return new WaitUntil(() =>
        {
            for (int i = 0; i < cardData.CardBoss.Count; ++i)
            {
                string type = cardData.CardBoss[i];

                if (_dicTypeBossBH[type].HP <= 0)
                {
                    return true;
                }
            }

            return _isTimeUP;
        });

        GameStateModel.Single.IsCard = false;
        GetCardInfo getCardInfo;

        if (!_isTimeUP)
        {
            AudioMgr.Single.PlayGameEff(AudioType.GetBomb);

            getCardInfo = new GetCardInfo(0, GameModel.Single.CardBonus, _cardTimeSpan);

            PlayerModel.Single.MemoryProcess += UnityEngine.Random.Range(30, 40);
            MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_UPDATE_MEMORY);

            PlayerModel.Single.MAX_GET_POINT += cardData.MaxPoint;
            MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_UPDATE_POINT);

            for (int i = 0; i < cardData.CardBoss.Count; ++i)
            {
                string type = cardData.CardBoss[i];

                _dicTypeBossBH[type].SpawnItems();
            }

            PlayerModel.Single.IsGetItem = true;
        }
        else
        {
            getCardInfo = new GetCardInfo(1, 0, _cardTimeSpan);

            AudioMgr.Single.PlayGameEff(AudioType.TimeUP);
        }

        GameModel.Single.Score += GameModel.Single.CardBonus;
        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_UPDATE_SCORE);
        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_PLAY_GET_CARD_INFO_ANIM, getCardInfo);
        _isTimeUP = false;

        Camera.main.DOShakePosition(2, 1);
        AudioMgr.Single.PlayGameEff(AudioType.GetFinalCard);
        GameStateModel.Single.IsFinalCard = false;

        for (int i = 0; i < bossData.BossType.Count; ++i)
        {
            string type = bossData.BossType[i];

            _dicTypeBossCtl[type].SetReceiver(false);
            _dicTypeBossCtl[type].StopCard();
            _dicTypeBossBH[type].Dead();
            _dicBossView[type].StopMagicBGAnim();
            MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_HIDE_HP_VIEW, _dicTypeBoss[type]);
        }

        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_HIDE_TIMER);
        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_MOVE_CARD_INFO_RIGHT);
        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_HIDE_BOSS_NAME_CARD);
        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_COMPLETE_TIMER);
        AudioMgr.Single.StopBGM();

        yield return new WaitForSeconds(2f);
        PlayerModel.Single.IsGetItem = false;
    }

    private IEnumerator OnStageElite(string stageName, bool isUp)
    {
        List<ElitleData> elitles = DataMgr.Single.GetElitleData(stageName);

        if (isUp)
        {
            MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_STAGE_ANIM);
            MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_BGM_SETTING, stageName);

            yield return new WaitForSeconds(4f);
        }

        for(int i = 0; i < elitles.Count; ++i)
        {
            ElitleData elitle = elitles[i];

            _eliteSpawnMgr.Spawn(elitle);

            yield return new WaitUntil(() => GameModel.Single.EnemyCount == 0);
            yield return new WaitForSeconds(0.8f);
        }

        yield return new WaitForSeconds(2);
    }

    private IEnumerator PrepareNextStage()
    {
        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_PLAY_STAGE_CLEAR_ANIM);

        yield return new WaitForSeconds(4.5f);

        GameModel.Single.Score += Const.CLEAR_BOUNS;
        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_UPDATE_SCORE);

        ++GameModel.Single.StageNum;
        _bgAnim.SetInteger("Stage", GameModel.Single.StageNum);
        InputMgr.Single.UpdateFun();
    }

    private void OnChatCallBack(object[] args)
    {
        ChatCallBack callBack = (ChatCallBack)args[0];

        if (callBack.ID != -1)
        {
            _dicIDAction[callBack.ID](callBack.CallBack, callBack.StrArg);
        }
    }

    private void SetTimeUP(object[] args)
    {
        _isTimeUP = (bool)args[0];
    }

    private void RestartGame(object[] args)
    {
        StopCoroutine(_coroutine);

        _coroutine = StartCoroutine(GameProcess());
    }

    private void CheckEnemyCount()
    {
        if(GameModel.Single.EnemyCount <= 0)
        {
            _curTimeTask = _queTimeTask.Peek();
            TimeMgr.Single.RemoveTimeTask(_tid);            //这里必须要去掉上次的tid
            _curTimeTask.CallBack();
        }
    }
}
