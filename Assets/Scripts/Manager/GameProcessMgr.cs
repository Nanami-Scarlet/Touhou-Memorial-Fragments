using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class GameProcessMgr : MonoBehaviour
{
    private EnemySpawnMgr _enemyspawnMgr;
    private BossSpawner _bossSpawner;

    private Dictionary<string, GameObject> _dicTypeBoss = new Dictionary<string, GameObject>();
    private Dictionary<string, BossController> _dicTypeBossCtl = new Dictionary<string, BossController>();
    private Dictionary<string, BossBehaviour> _dicTypeBossBH = new Dictionary<string, BossBehaviour>();
    private Dictionary<int, Action<Action>> _dicIDAction;
    private Queue<TimeTask> _queTimeTask = new Queue<TimeTask>();

    private bool _isTimeUP = false;
    private TimeTask _curTimeTask;
    private int _tid;

    public void Init()
    {
        MessageMgr.Single.AddListener(MsgEvent.EVENT_CHAT_CALLBACK, OnChatCallBack);
        MessageMgr.Single.AddListener(MsgEvent.EVENT_SET_TIMEUP, SetTimeUP);

        _dicIDAction = new Dictionary<int, Action<Action>>()
        {
            {
                0,
                (callBack) => {
                    //代号B1（辉夜）Boss登场   下同
                    _dicTypeBoss["B1"].GetComponent<BossController>().Appear(callBack);
                }
            },

            {
                1,
                (callBack) => {
                    _dicTypeBoss["B2"].GetComponent<BossController>().Appear(callBack);
                }
            },

            {
                2,
                (callBack) => {
                    _dicTypeBoss["B3"].GetComponent<BossController>().Appear(callBack);
                }
            },

            {
                3,
                (callBack) => {
                    _dicTypeBoss["B4"].GetComponent<BossController>().Appear(callBack);
                }
            },
        };

        _enemyspawnMgr = gameObject.AddComponent<EnemySpawnMgr>();
        _enemyspawnMgr.Init();

        _bossSpawner = gameObject.AddComponent<BossSpawner>();
        _bossSpawner.Init();

        //CoroutineMgr.Single.Execute(OnState("stage1_1"));
        StartCoroutine(GameProcess());
    }

    private void OnDestroy()
    {
        GameModel.Single.EnemyCount = 0;

        PoolMgr.Single.ClearPool();
        TimeMgr.Single.ClearAllTask();

        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_CHAT_CALLBACK, OnChatCallBack);
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_SET_TIMEUP, SetTimeUP);
    }

    private IEnumerator GameProcess()
    {
        switch (GameStateModel.Single.GameMode)
        {
            case Mode.NORMAL:
                yield return OnStageUp("stage1_1");
                yield return OnStageBoss("stage_B1");
                yield return OnStageDown("stage1_2");
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
                TimeTask timeTask = new TimeTask(tid, Time.time + enemyData.Delay, () =>
                {
                    _enemyspawnMgr.Spawn(enemyData);
                    ++GameModel.Single.EnemyCount;

                    _queTimeTask.Dequeue();
                    if (_queTimeTask.Count > 0)
                    {
                        _curTimeTask = _queTimeTask.Peek();

                        _tid = TimeMgr.Single.AddTimeTask(() =>
                        {
                            TimeMgr.Single.AddTimeTask(_curTimeTask);
                        }, enemyData.Delay, TimeUnit.Second);       //间隔之后才产生下一只妖精
                    }
                    else
                    {
                        CancelInvoke(nameof(CheckEnemyCount));
                    }
                });

                _queTimeTask.Enqueue(timeTask);
            }
        }

        yield return new WaitForSeconds(4f);

        _curTimeTask = _queTimeTask.Peek();
        TimeMgr.Single.AddTimeTask(_curTimeTask);

        InvokeRepeating(nameof(CheckEnemyCount), 0.5f, 0.1f);

        while (_queTimeTask.Count > 0 || GameModel.Single.EnemyCount > 0)
        {
            yield return new WaitForEndOfFrame();
        }
    }

    private IEnumerator OnStageDown(string stageName)
    {
        StageData stageData = DataMgr.Single.GetStageData(stageName);

        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_BGM_SETTING, stageName);

        for (int i = 1; i < stageData.ListWaveEnemy.Count; ++i)
        {
            WaveData waveData = stageData.ListWaveEnemy[i];

            for (int j = 0; j < waveData.ListEnemy.Count; ++j)
            {
                EnemyData enemyData = waveData.ListEnemy[j];

                int tid = TimeMgr.Single.GetTid();
                TimeTask timeTask = new TimeTask(tid, Time.time + enemyData.Delay, () =>
                {
                    _enemyspawnMgr.Spawn(enemyData);
                    ++GameModel.Single.EnemyCount;

                    _queTimeTask.Dequeue();
                    if (_queTimeTask.Count > 0)
                    {
                        _curTimeTask = _queTimeTask.Peek();

                        _tid = TimeMgr.Single.AddTimeTask(() =>
                        {
                            TimeMgr.Single.AddTimeTask(_curTimeTask);
                        }, enemyData.Delay, TimeUnit.Second);       //间隔之后才产生下一只妖精
                    }
                    else
                    {
                        CancelInvoke(nameof(CheckEnemyCount));
                    }
                });

                _queTimeTask.Enqueue(timeTask);
            }
        }

        _curTimeTask = _queTimeTask.Peek();
        TimeMgr.Single.AddTimeTask(_curTimeTask);

        InvokeRepeating(nameof(CheckEnemyCount), 0.5f, 0.1f);

        while (_queTimeTask.Count > 0 || GameModel.Single.EnemyCount > 0)
        {
            yield return new WaitForEndOfFrame();
        }
    }

    private IEnumerator OnStageBoss(string stageName)
    {
        BossData bossData = DataMgr.Single.GetBossData(stageName);
        for(int i = 0; i < bossData.BossType.Count; ++i)
        {
            string bossType = bossData.BossType[i];
            Vector3 bornPos = bossData.BornPos[i];
            List<Vector3> appearPath = bossData.AppearPath[i];

            SingleBossInitData data = new SingleBossInitData(bossType, bornPos, appearPath);
            GameObject bossGO = _bossSpawner.Spawn(data);

            MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_ADD_HPBAR, bossGO);
            _dicTypeBoss.Add(bossType, bossGO);
            _dicTypeBossCtl.Add(bossType, bossGO.GetComponent<BossController>());
            _dicTypeBossBH.Add(bossType, bossGO.GetComponent<BossBehaviour>());
        }
        OnStageChat(stageName);

        yield return new WaitForSeconds(2f);       //预留一定的时间，防止对话没有开始，就开始战斗

        yield return new WaitUntil(() => !GameStateModel.Single.IsChating);         //对话结束

        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_BGM_SETTING, stageName);
        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_SHOW_BOSS_NAME_CARD, new BossNameCard(bossData.Name, bossData.Cards.Count));

        for(int i = 0/**/; i < bossData.Cards.Count - 1; ++i)
        {
            CardData cardData = bossData.Cards[i];

            /////////////////////////////
            yield return OnBossNormalCard(bossData, cardData);
            yield return OnBossCard(bossData, cardData);

            MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_SHOW_BOSS_NAME_CARD, new BossNameCard(bossData.Name, bossData.Cards.Count - i - 1));
        }

        yield return OnBossFinalCard(bossData, bossData.Cards[bossData.Cards.Count - 1]);
    }

    private void OnStageChat(string stageName)
    {
        UIManager.Single.Show(Paths.PREFAB_CHAT_VIEW, false);
        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_SHOW_DIALOG, stageName);
    }

    private IEnumerator OnBossNormalCard(BossData bossData, CardData cardData)
    {
        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_SET_TIMER, cardData.NormalTime);       //非符

        for (int j = 0; j < bossData.BossType.Count; ++j)
        {
            string type = bossData.BossType[j];

            BaseCardHPData singleCardData = new BaseCardHPData(cardData.NormalHP, cardData.CardHP, cardData.BarIndex,
                _dicTypeBoss[type].transform);
            MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_SET_HPBAR, singleCardData);

            _dicTypeBossBH[type].SetBehaviour(cardData.NormalHP + cardData.CardHP);               //非符

            yield return new WaitForSeconds(2f);        //等待血条动画
            _dicTypeBossCtl[type].SetReceiver(true);

            _dicTypeBossCtl[type].ResetCard(new SingleBossCardData(cardData.NormalPath[j], cardData.NormalDuration[j], cardData.NormalDelay[j], cardData.NormalEmitter[j]));
        }

        yield return new WaitUntil(() =>
        {
            for (int j = 0; j < bossData.BossType.Count; ++j)
            {
                string type = bossData.BossType[j];

                if (_dicTypeBossBH[type].HP <= cardData.CardHP)       //在规定时间内击破
                {
                    return true;
                }
            }

            return _isTimeUP;                                   //是否超时
        });

        _isTimeUP = false;
    }

    private IEnumerator OnBossCard(BossData bossData, CardData cardData)
    {
        for (int j = 0; j < cardData.CurBoss.Count; ++j)
        {
            MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_SET_CARD_PIC, cardData.CurBoss[j]);
        }
        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_MOVE_TIMER, -20);
        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_SET_TIMER, cardData.CardTime);
        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_PLAY_CARD_INFO_ANIM, cardData.CardName);
        AudioMgr.Single.PlayGameEff(AudioType.Card);
        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_PLAY_CARD_PIC_ANIM);

        for (int j = 0; j < bossData.BossType.Count; ++j)
        {
            string type = bossData.BossType[j];
            
            _dicTypeBossCtl[type].Move(cardData.CardInitPos);
        }

        for (int j = 0; j < cardData.CurBoss.Count; ++j)
        {
            string type = cardData.CurBoss[j];

            _dicTypeBossBH[type].SetBehaviour(cardData.CardHP);         //符卡
            MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_SET_HPBAR_VIEW, new HPData(_dicTypeBoss[type], cardData.CardHP));
            _dicTypeBossCtl[type].ResetCard(new SingleBossCardData(cardData.CardPath[j], cardData.CardDuration[j], cardData.CardDelay[j], cardData.CardEmitter[j]));
        }

        yield return new WaitUntil(() =>
        {
            for (int j = 0; j < cardData.CurBoss.Count; ++j)
            {
                string type = bossData.BossType[j];

                if (_dicTypeBossBH[type].HP <= 0)
                {
                    return true;
                }
            }

            if (_isTimeUP)
            {
                _isTimeUP = false;
                return true;
            }

            return false;
        });

        //todo:显示收卡数据UI
        for (int j = 0; j < bossData.BossType.Count; ++j)
        {
            string type = bossData.BossType[j];

            _dicTypeBossCtl[type].SetReceiver(false);
        }
        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_MOVE_CARD_INFO_RIGHT);
        yield return new WaitForSeconds(1);             //等待动画完成
    }

    private IEnumerator OnBossFinalCard(BossData bossData, CardData cardData)
    {
        for (int j = 0; j < cardData.CurBoss.Count; ++j)
        {
            MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_SET_CARD_PIC, cardData.CurBoss[j]);
        }
        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_MOVE_TIMER, -20);
        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_SET_TIMER, cardData.CardTime);
        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_PLAY_CARD_INFO_ANIM, cardData.CardName);
        AudioMgr.Single.PlayGameEff(AudioType.Card);
        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_PLAY_CARD_PIC_ANIM);

        for (int j = 0; j < bossData.BossType.Count; ++j)
        {
            string type = bossData.BossType[j];

            _dicTypeBossCtl[type].Move(cardData.CardInitPos);
        }

        for (int j = 0; j < cardData.CurBoss.Count; ++j)
        {
            string type = cardData.CurBoss[j];

            _dicTypeBossBH[type].IsFinalCard = true;
            _dicTypeBossBH[type].SetBehaviour(cardData.CardHP);         //符卡
            MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_SET_HPBAR, new BaseCardHPData(cardData.NormalHP, cardData.CardHP, cardData.BarIndex, _dicTypeBoss[type].transform));
        }

        yield return new WaitForSeconds(1.5f);
        for (int j = 0; j < cardData.CurBoss.Count; ++j)
        {
            string type = cardData.CurBoss[j];

            _dicTypeBossCtl[type].ResetCard(new SingleBossCardData(cardData.CardPath[j], cardData.CardDuration[j], cardData.CardDelay[j], cardData.CardEmitter[j]));
            _dicTypeBossCtl[type].SetReceiver(true);
        }

        yield return new WaitUntil(() =>
        {
            for (int j = 0; j < cardData.CurBoss.Count; ++j)
            {
                string type = bossData.BossType[j];

                if (_dicTypeBossBH[type].HP <= 0)
                {
                    return true;
                }
            }

            if (_isTimeUP)
            {
                return true;
            }

            return false;
        });

        //todo:显示收卡数据UI
        Camera.main.DOShakePosition(2, 1);
        AudioMgr.Single.PlayGameEff(AudioType.GetFinalCard);
        for (int j = 0; j < bossData.BossType.Count; ++j)
        {
            string type = bossData.BossType[j];

            _dicTypeBossBH[type].IsFinalCard = false;
            _dicTypeBossCtl[type].SetReceiver(false);
            _dicTypeBossCtl[type].StopCard();
            _dicTypeBossBH[type].Dead();
            MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_HIDE_HP_VIEW, _dicTypeBoss[type]);
        }

        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_HIDE_TIMER);
        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_MOVE_CARD_INFO_RIGHT);
        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_HIDE_BOSS_NAME_CARD);
        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_COMPLETE_TIMER);
        AudioMgr.Single.StopBGM();
        yield return new WaitForSeconds(3);
    }

    private void OnChatCallBack(object[] args)
    {
        ChatCallBack callBack = (ChatCallBack)args[0];

        if (callBack.ID != -1)
        {
            _dicIDAction[callBack.ID](callBack.CallBack);
        }
    }

    private void SetTimeUP(object[] args)
    {
        _isTimeUP = (bool)args[0];
    }

    private void CheckEnemyCount()
    {
        if(GameModel.Single.EnemyCount <= 0)
        {
            _curTimeTask = _queTimeTask.Peek();
            //TimeMgr.Single.AddTimeTask(_curTimeTask);
            TimeMgr.Single.RemoveTimeTask(_tid);            //这里必须要去掉上次的tid，可以用多线程冲突来理解
            _curTimeTask.CallBack();
        }
    }
}
