using BulletPro;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private float _inchSpeed = 1.5f;
    private float _normalSpeed = 6f;
    //private int _dirKeyDownCount = 0;
    //避免按键系统同时要监听两个按键
    private bool _isPrssShift = false;      //慢速开火需要同时按下Z与Shift

    private MoveComponet _move;
    private Animator _anim;

    public List<Transform> _listManaLevelTrans;
    public List<BulletEmitter> _listMainShot;
    private Dictionary<string, Action<Collider2D>> _dicItemTagAction;
    private Dictionary<int, YinEmitter> _dicLevelYinEmitter = new Dictionary<int, YinEmitter>();
    public BulletEmitter _cardShot;
    public ParticleSystem _cardEff;

    public BulletEmitter _wolfEmitter;
    private bool _isWolfShoot = false;

    public BulletEmitter _otterEmitter;

    public void Init()
    {
        PlayerModel.Single.Mana = 100;
        PlayerModel.Single.IsGetItem = false;
        PlayerModel.Single.State = PlayerState.NORMAL;
        PlayerModel.Single.MemoryProcess = 0;
        PlayerModel.Single.MemoryFragment = 0;
        PlayerModel.Single.Graze = 0;

        _dicItemTagAction = new Dictionary<string, Action<Collider2D>>()
        {
            { "PItem", (other) =>
            {
                if (PlayerModel.Single.Mana < 400)
                {
                    GameModel.Single.Score += Const.MANA_SCORE;
                    ++PlayerModel.Single.Mana;
                    //PlayerModel.Single.Mana += 30;

                    MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_UPDATE_MANA);
                    MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_CHECK_MANA);
                }
                else
                {
                    GameModel.Single.Score += Const.MAX_MANA_SCORE;
                }

                MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_UPDATE_SCORE);

                AudioMgr.Single.PlayGameEff(AudioType.Items);
                other.gameObject.GetComponent<Item>().ResetItem();
                PoolMgr.Single.Despawn(other.gameObject);
            } },

            { "PointItem", (other) =>
            {
                GameModel.Single.Score += Const.POINT_SCORE;

                MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_UPDATE_SCORE);

                AudioMgr.Single.PlayGameEff(AudioType.Items);
                other.gameObject.GetComponent<Item>().ResetItem();
                PoolMgr.Single.Despawn(other.gameObject);
            } },

            { "LifeItem", (other) =>
            {
                if(GameStateModel.Single.GameMode == Mode.LUNATIC)
                {
                    GameModel.Single.Score += Const.LIFE_SCORE;

                    MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_GET_LIFT);
                }

                MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_UPDATE_SCORE);
                other.gameObject.GetComponent<Item>().ResetItem();
                PoolMgr.Single.Despawn(other.gameObject);

            } },

            { "BombItem", (other) =>
            {
                GameModel.Single.Score += Const.BOMB_SCORE;

                MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_GET_BOMB);
                MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_UPDATE_SCORE);

                other.gameObject.GetComponent<Item>().ResetItem();
                PoolMgr.Single.Despawn(other.gameObject);
            } }
        };

        _move = GetComponent<MoveComponet>();
        _anim = GetComponent<Animator>();

        InputMgr.Single.AddGameListener(KeyCode.UpArrow);
        InputMgr.Single.AddGameListener(KeyCode.DownArrow);
        InputMgr.Single.AddGameListener(KeyCode.RightArrow);
        InputMgr.Single.AddGameListener(KeyCode.LeftArrow);
        InputMgr.Single.AddGameListener(KeyCode.LeftShift);
        InputMgr.Single.AddGameListener(KeyCode.Z);
        InputMgr.Single.AddGameListener(KeyCode.X);
        InputMgr.Single.AddGameListener(KeyCode.A);
        InputMgr.Single.AddGameListener(KeyCode.S);
        InputMgr.Single.AddGameListener(KeyCode.D);
        InputMgr.Single.AddGameListener(KeyCode.F);

        MessageMgr.Single.AddListener(KeyCode.UpArrow, MoveUp, InputState.PRESS);
        MessageMgr.Single.AddListener(KeyCode.DownArrow, MoveDown, InputState.PRESS);
        MessageMgr.Single.AddListener(KeyCode.RightArrow, MoveRight, InputState.PRESS);
        MessageMgr.Single.AddListener(KeyCode.LeftArrow, MoveLeft, InputState.PRESS);
        MessageMgr.Single.AddListener(KeyCode.LeftShift, Inch, InputState.PRESS);
        MessageMgr.Single.AddListener(KeyCode.Z, Fire, InputState.PRESS);

        MessageMgr.Single.AddListener(KeyCode.X, ReleaseCard, InputState.DOWN);
        MessageMgr.Single.AddListener(KeyCode.A, OnWolfShot, InputState.DOWN);
        MessageMgr.Single.AddListener(KeyCode.S, OnOtterShot, InputState.DOWN);
        MessageMgr.Single.AddListener(KeyCode.D, SummonBomb, InputState.DOWN);
        MessageMgr.Single.AddListener(KeyCode.F, SummonLife, InputState.DOWN);
        //MessageMgr.Single.AddListener(KeyCode.UpArrow, OnDirKeyDown, InputState.DOWN);
        //MessageMgr.Single.AddListener(KeyCode.DownArrow, OnDirKeyDown, InputState.DOWN);
        //MessageMgr.Single.AddListener(KeyCode.RightArrow, OnDirKeyDown, InputState.DOWN);
        //MessageMgr.Single.AddListener(KeyCode.LeftArrow, OnDirKeyDown, InputState.DOWN);

        MessageMgr.Single.AddListener(KeyCode.UpArrow, OnDirKeyUp, InputState.UP);
        MessageMgr.Single.AddListener(KeyCode.DownArrow, OnDirKeyUp, InputState.UP);
        MessageMgr.Single.AddListener(KeyCode.RightArrow, OnDirKeyUp, InputState.UP);
        MessageMgr.Single.AddListener(KeyCode.LeftArrow, OnDirKeyUp, InputState.UP);
        MessageMgr.Single.AddListener(KeyCode.LeftShift, OnShiftUp, InputState.UP);
        MessageMgr.Single.AddListener(KeyCode.Z, StopFire, InputState.UP);

        _move.Speed = _normalSpeed;

        for (int i = 0; i < 4; ++i)
        {
            _dicLevelYinEmitter[i] = new YinEmitter()
            {
                NormalEmitters = new List<BulletEmitter>(),
                FoucusEmitters = new List<BulletEmitter>()
            };

            for (int j = 0; j < i + 1; ++j)
            {
                Transform shot = _listManaLevelTrans[i].GetChild(j).GetChild(1);
                _dicLevelYinEmitter[i].NormalEmitters.Add(shot.GetChild(0).GetComponent<BulletEmitter>());
                _dicLevelYinEmitter[i].FoucusEmitters.Add(shot.GetChild(1).GetComponent<BulletEmitter>());
            }
        }

        _isWolfShoot = false;

        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_UPDATE_MANA);
        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_CHECK_MANA);
        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_UPDATE_MEMORY);
    }

    //private void Update()
    //{
    //    //_move.Speed = _normalSpeed;
    //    //if (_dirKeyDownCount % 2 == 0)
    //    //{
    //    //    _move.Speed = Mathf.Sqrt(_move.Speed);
    //    //}

    //    Debug.Log(_move.Speed);
    //}

    private void OnTriggerEnter2D(Collider2D other)
    {
        _dicItemTagAction[other.tag](other);
    }

    private void OnDestroy()
    {
        InputMgr.Single.RemoveGameListener(KeyCode.UpArrow);
        InputMgr.Single.RemoveGameListener(KeyCode.DownArrow);
        InputMgr.Single.RemoveGameListener(KeyCode.RightArrow);
        InputMgr.Single.RemoveGameListener(KeyCode.LeftArrow);
        InputMgr.Single.RemoveGameListener(KeyCode.LeftShift);
        InputMgr.Single.RemoveGameListener(KeyCode.Z);
        InputMgr.Single.RemoveGameListener(KeyCode.X);
        InputMgr.Single.RemoveGameListener(KeyCode.A);
        InputMgr.Single.RemoveGameListener(KeyCode.S);
        InputMgr.Single.RemoveGameListener(KeyCode.F);

        MessageMgr.Single.RemoveListener(KeyCode.UpArrow, MoveUp, InputState.PRESS);
        MessageMgr.Single.RemoveListener(KeyCode.DownArrow, MoveDown, InputState.PRESS);
        MessageMgr.Single.RemoveListener(KeyCode.RightArrow, MoveRight, InputState.PRESS);
        MessageMgr.Single.RemoveListener(KeyCode.LeftArrow, MoveLeft, InputState.PRESS);
        MessageMgr.Single.RemoveListener(KeyCode.LeftShift, Inch, InputState.PRESS);
        MessageMgr.Single.RemoveListener(KeyCode.Z, Fire, InputState.PRESS);

        MessageMgr.Single.RemoveListener(KeyCode.X, ReleaseCard, InputState.DOWN);
        MessageMgr.Single.RemoveListener(KeyCode.A, OnWolfShot, InputState.DOWN);
        MessageMgr.Single.RemoveListener(KeyCode.S, OnOtterShot, InputState.DOWN);
        MessageMgr.Single.RemoveListener(KeyCode.D, SummonBomb, InputState.DOWN);
        MessageMgr.Single.RemoveListener(KeyCode.F, SummonLife, InputState.DOWN);
        //MessageMgr.Single.RemoveListener(KeyCode.UpArrow, OnDirKeyDown, InputState.DOWN);
        //MessageMgr.Single.RemoveListener(KeyCode.DownArrow, OnDirKeyDown, InputState.DOWN);
        //MessageMgr.Single.RemoveListener(KeyCode.RightArrow, OnDirKeyDown, InputState.DOWN);
        //MessageMgr.Single.RemoveListener(KeyCode.LeftArrow, OnDirKeyDown, InputState.DOWN);

        MessageMgr.Single.RemoveListener(KeyCode.UpArrow, OnDirKeyUp, InputState.UP);
        MessageMgr.Single.RemoveListener(KeyCode.DownArrow, OnDirKeyUp, InputState.UP);
        MessageMgr.Single.RemoveListener(KeyCode.RightArrow, OnDirKeyUp, InputState.UP);
        MessageMgr.Single.RemoveListener(KeyCode.LeftArrow, OnDirKeyUp, InputState.UP);
        MessageMgr.Single.RemoveListener(KeyCode.LeftShift, OnShiftUp, InputState.UP);
        MessageMgr.Single.RemoveListener(KeyCode.Z, StopFire, InputState.UP);
    }

    private void MoveUp(object[] args)
    {
        if (GameUtil.JudgeBorderUp(transform.position))
        {
            _move.Move(Vector3.up);
        }

        if (transform.position.y > 1.8f)
        {
            PlayerModel.Single.IsGetItem = true;
        }
    }

    private void MoveDown(object[] args)
    {
        if (GameUtil.JudgeBorderDown(transform.position))
        {
            _move.Move(Vector3.down);
        }

        if (transform.position.y <= 1.8f)
        {
            PlayerModel.Single.IsGetItem = false;
        }
    }

    private void MoveRight(object[] args)
    {
        if (GameUtil.JudgeBorderRight(transform.position))
        {
            _anim.SetInteger("Speed", 1);
            _move.Move(Vector3.right);
        }
    }

    private void MoveLeft(object[] args)
    {
        if (GameUtil.JudgeBorderLeft(transform.position))
        {
            _anim.SetInteger("Speed", -1);
            _move.Move(Vector3.left);
        }
    }

    private void Inch(object[] args)
    {
        _isPrssShift = true;
        _move.Speed = _inchSpeed;
    }

    private void FocusFire()
    {
        int level = GameUtil.GetManaLevel();

        if (!_isWolfShoot)
        {
            foreach (var emitter in _listMainShot)
            {
                emitter.Play();
            }

            foreach (var emitter in _dicLevelYinEmitter[level].NormalEmitters)
            {
                emitter.Stop();
            }

            foreach (var emitter in _dicLevelYinEmitter[level].FoucusEmitters)
            {
                emitter.Play();
            }

            return;
        }

        _wolfEmitter.Play();
    }

    private void NormalFire()
    {
        int level = GameUtil.GetManaLevel();

        if (!_isWolfShoot)
        {
            foreach (var emitter in _listMainShot)
            {
                emitter.Play();
            }

            foreach (var emitter in _dicLevelYinEmitter[level].FoucusEmitters)
            {
                emitter.Stop();
            }

            foreach (var emitter in _dicLevelYinEmitter[level].NormalEmitters)
            {
                emitter.Play();
            }

            return;
        }

        _wolfEmitter.Play();
    }

    private void Fire(object[] args)
    {
        if (!_isPrssShift)
        {
            NormalFire();

            return;
        }

        FocusFire();
    }

    private void StopFire(object[] args)
    {
        int level = GameUtil.GetManaLevel();

        if (!_isWolfShoot)
        {
            foreach (var emitter in _listMainShot)
            {
                emitter.Stop();
            }

            foreach (var emitter in _dicLevelYinEmitter[level].NormalEmitters)
            {
                emitter.Stop();
            }

            foreach (var emitter in _dicLevelYinEmitter[level].FoucusEmitters)
            {
                emitter.Stop();
            }

            return;
        }

        _wolfEmitter.Stop();
    }

    //private void OnDirKeyDown(object[] args)
    //{
    //    ++_dirKeyDownCount;
    //}

    private void OnDirKeyUp(object[] args)
    {
        _anim.SetInteger("Speed", 0);
        //if (_dirKeyDownCount > 0)
        //{
        //    --_dirKeyDownCount;
        //}
    }

    private void OnShiftUp(object[] args)
    {
        _isPrssShift = false;
        _move.Speed = _normalSpeed;
    }

    private void OnWolfShot(object[] args)
    {
        _isWolfShoot = true;
        --PlayerModel.Single.MemoryFragment;
        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_UPDATE_MEMORY);
        RemoveFunctionListener();

        TimeMgr.Single.AddTimeTask(() =>
        {
            AddFunctionListener();
            InputMgr.Single.UpdateKeyState();
            _isWolfShoot = false;
        }, 6, TimeUnit.Second);
    }

    private void SummonBomb(object[] args)
    {
        --PlayerModel.Single.MemoryFragment;
        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_UPDATE_MEMORY);

        int count = UnityEngine.Random.Range(1, 3);

        for(int i = 0; i < count; ++i)
        {
            GameObject item = PoolMgr.Single.Spawn("BombFragment");

            float posX = UnityEngine.Random.Range(-4.5f, 2.9f);
            item.GetComponent<Item>().Summon(new Vector3(posX, 5.1f, 0));
        }
    }

    private void SummonLife(object[] args)
    {
        --PlayerModel.Single.MemoryFragment;
        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_UPDATE_MEMORY);

        GameObject item = PoolMgr.Single.Spawn("LifeFragment");

        float posX = UnityEngine.Random.Range(-4.5f, 2.9f);
        item.GetComponent<Item>().Summon(new Vector3(posX, 5.1f, 0));
    }

    private void OnOtterShot(object[] args)
    {
        --PlayerModel.Single.MemoryFragment;
        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_UPDATE_MEMORY);
        RemoveFunctionListener();
        _otterEmitter.Play();

        TimeMgr.Single.AddTimeTask(() => 
        {
            AddFunctionListener();
            _otterEmitter.Kill();
        }, 6, TimeUnit.Second);
    }

    private void ReleaseCard(object[] args)
    {
        if (PlayerModel.Single.Bomb > 0)
        {
            _cardShot.Boot();           //初始化状态然后重新播放
            PlayerModel.Single.State = PlayerState.INVINCIBLE;
            _cardEff.Play();

            AudioMgr.Single.PlayGameEff(AudioType.ReleaseBomb);
            MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_PLAYER_USE_BOMB);
            MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_TWINKLE_SELF, 40);
            MessageMgr.Single.RemoveListener(KeyCode.X, ReleaseCard, InputState.DOWN);
            PlayerModel.Single.IsGetItem = true;

            TimeMgr.Single.AddTimeTask(() =>
            {
                AudioMgr.Single.StopGameEff(AudioType.ReleaseBomb);
                MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_CLEAR_ENEMY_BULLET);
                MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_RELEASE_CARD);
                MessageMgr.Single.AddListener(KeyCode.X, ReleaseCard, InputState.DOWN);
            }, 3f, TimeUnit.Second);

            TimeMgr.Single.AddTimeTask(() => 
            {
                PlayerModel.Single.IsGetItem = false;
            }, 4f, TimeUnit.Second);
        }
    }

    private void AddFunctionListener()
    {
        MessageMgr.Single.AddListener(KeyCode.A, OnWolfShot, InputState.DOWN);
        MessageMgr.Single.AddListener(KeyCode.S, OnOtterShot, InputState.DOWN);
    }

    private void RemoveFunctionListener()
    {
        MessageMgr.Single.RemoveListener(KeyCode.A, OnWolfShot, InputState.DOWN);
        MessageMgr.Single.RemoveListener(KeyCode.S, OnOtterShot, InputState.DOWN);
    }
}
