using BulletPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float _inchSpeed = 1.5f;
    public float _normalSpeed = 6f;
                                            //避免按键系统同时要监听两个按键
    private bool _isPrssShift = false;      //慢速开火需要同时按下Z与Shift

    private MoveComponet _move;
    private Animator _anim;

    public List<Transform> _listManaLevelTrans;
    public List<BulletEmitter> _listMainShot;
    private Dictionary<int, YinEmitter> _dicLevelYinEmitter = new Dictionary<int, YinEmitter>();
    public BulletEmitter _cardShot;
    public ParticleSystem _cardEff;

    public void Init()
    {
        PlayerModel.Single.Mana = 100;
        PlayerModel.Single.Graze = 0;

        _move = GetComponent<MoveComponet>();
        _anim = GetComponent<Animator>();

        InputMgr.Single.AddGameListener(KeyCode.UpArrow);
        InputMgr.Single.AddGameListener(KeyCode.DownArrow);
        InputMgr.Single.AddGameListener(KeyCode.RightArrow);
        InputMgr.Single.AddGameListener(KeyCode.LeftArrow);
        InputMgr.Single.AddGameListener(KeyCode.LeftShift);
        InputMgr.Single.AddGameListener(KeyCode.Z);
        InputMgr.Single.AddGameListener(KeyCode.X);

        MessageMgr.Single.AddListener(KeyCode.UpArrow, MoveUp, InputState.PRESS);
        MessageMgr.Single.AddListener(KeyCode.DownArrow, MoveDown, InputState.PRESS);
        MessageMgr.Single.AddListener(KeyCode.RightArrow, MoveRight, InputState.PRESS);
        MessageMgr.Single.AddListener(KeyCode.LeftArrow, MoveLeft, InputState.PRESS);
        MessageMgr.Single.AddListener(KeyCode.LeftShift, Inch, InputState.PRESS);
        MessageMgr.Single.AddListener(KeyCode.Z, Fire, InputState.PRESS);
        MessageMgr.Single.AddListener(KeyCode.X, ReleaseCard, InputState.DOWN);

        MessageMgr.Single.AddListener(KeyCode.UpArrow, OnDirKeyUp, InputState.UP);
        MessageMgr.Single.AddListener(KeyCode.DownArrow, OnDirKeyUp, InputState.UP);
        MessageMgr.Single.AddListener(KeyCode.RightArrow, OnDirKeyUp, InputState.UP);
        MessageMgr.Single.AddListener(KeyCode.LeftArrow, OnDirKeyUp, InputState.UP);
        MessageMgr.Single.AddListener(KeyCode.LeftShift, OnShiftUp, InputState.UP);
        MessageMgr.Single.AddListener(KeyCode.Z, StopFire, InputState.UP);

        _move.Speed = _normalSpeed;

        for(int i = 0; i < 4; ++i)
        {
            _dicLevelYinEmitter[i] = new YinEmitter()
            {
                NormalEmitters = new List<BulletEmitter>(),
                FoucusEmitters = new List<BulletEmitter>()
            };
            
            for(int j = 0; j < i + 1; ++j)
            {
                Transform shot = _listManaLevelTrans[i].GetChild(j).GetChild(1);
                _dicLevelYinEmitter[i].NormalEmitters.Add(shot.GetChild(0).GetComponent<BulletEmitter>());
                _dicLevelYinEmitter[i].FoucusEmitters.Add(shot.GetChild(1).GetComponent<BulletEmitter>());
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("ItemPoint"))
        {
            GameModel.Single.Score += Const.POINT_SCORE;
        }
        else if (other.CompareTag("ItemP"))
        {
            if (PlayerModel.Single.Mana < 400)
            {
                GameModel.Single.Score += Const.MANA_SCORE;
                ++PlayerModel.Single.Mana;
                //PlayerModel.Single.Mana += 30;

                MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_UPDATE_MANA, PlayerModel.Single.Mana);
                MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_CHECK_MANA);
            }
            else
            {
                GameModel.Single.Score += Const.MAX_MANA_SCORE;
            }
        }

        if (other.CompareTag("ItemPoint") || other.CompareTag("ItemP"))     //防止和妖精的可发送弹幕冲突
        {
            MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_UPDATE_SCORE, GameModel.Single.Score);

            AudioMgr.Single.PlayGameEff(AudioType.Items);
            other.gameObject.GetComponent<Item>().ResetItem();
            PoolMgr.Single.Despawn(other.gameObject);
        }
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

        MessageMgr.Single.RemoveListener(KeyCode.UpArrow, MoveUp, InputState.PRESS);
        MessageMgr.Single.RemoveListener(KeyCode.DownArrow, MoveDown, InputState.PRESS);
        MessageMgr.Single.RemoveListener(KeyCode.RightArrow, MoveRight, InputState.PRESS);
        MessageMgr.Single.RemoveListener(KeyCode.LeftArrow, MoveLeft, InputState.PRESS);
        MessageMgr.Single.RemoveListener(KeyCode.LeftShift, Inch, InputState.PRESS);
        MessageMgr.Single.RemoveListener(KeyCode.Z, Fire, InputState.PRESS);

        MessageMgr.Single.RemoveListener(KeyCode.UpArrow, OnDirKeyUp, InputState.UP);
        MessageMgr.Single.RemoveListener(KeyCode.DownArrow, OnDirKeyUp, InputState.UP);
        MessageMgr.Single.RemoveListener(KeyCode.RightArrow, OnDirKeyUp, InputState.UP);
        MessageMgr.Single.RemoveListener(KeyCode.LeftArrow, OnDirKeyUp, InputState.UP);
        MessageMgr.Single.RemoveListener(KeyCode.LeftShift, OnShiftUp, InputState.UP);
        MessageMgr.Single.RemoveListener(KeyCode.Z, StopFire, InputState.UP);

        MessageMgr.Single.RemoveListener(KeyCode.X, ReleaseCard, InputState.DOWN);
    }

    private void MoveUp(object[] args)
    {
        if (GameUtil.JudgeBorderUp(transform.position))
        {
            _move.Move(Vector3.up);
        }

        if(transform.position.y > 1.8f)
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
    }
        
    private void NormalFire()
    {
        int level = GameUtil.GetManaLevel();

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
    }

    private void Fire(object[] args)
    {
        if (!_isPrssShift)
        {
            NormalFire();
        }
        else
        {
            FocusFire();
        }
    }

    private void StopFire(object[] args)
    {
        int level = GameUtil.GetManaLevel();

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
    }

    private void OnDirKeyUp(object[] args)
    {
        _anim.SetInteger("Speed", 0);
    }

    private void OnShiftUp(object[] args)
    {
        _isPrssShift = false;
        _move.Speed = _normalSpeed;
    }

    private void ReleaseCard(object[] args)
    {
        if (PlayerModel.Single.Bomb > 0)
        {
            _cardShot.Boot();           //初始化状态然后重新播放
            --PlayerModel.Single.Bomb;
            PlayerModel.Single.State = PlayerState.INVINCIBLE;
            _cardEff.Play();

            AudioMgr.Single.PlayGameEff(AudioType.ReleaseBomb);
            MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_UPDATE_BOMB);
            MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_TWINKLE_SELF);
            MessageMgr.Single.RemoveListener(KeyCode.X, ReleaseCard, InputState.DOWN);

            TimeMgr.Single.AddTimeTask(() => 
            {
                AudioMgr.Single.StopGameEff(AudioType.ReleaseBomb);
                MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_CLEAR_ENEMY_BULLET);
                MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_RELEASE_CARD);
                MessageMgr.Single.AddListener(KeyCode.X, ReleaseCard, InputState.DOWN);
            }, 2f, TimeUnit.Second);
        }
    }
}
