using BulletPro;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBehaviour : EntityBehaviourBase
{
    public BulletReceiver _receiver;
    public PlayerView _view;
    private float _timeSpan = 0f;


    public void Init()
    {
        MessageMgr.Single.AddListener(MsgEvent.EVENT_TWINKLE_SELF, TwinkleSelf);
    }

    private void Update()
    {
        _timeSpan += Time.deltaTime;
        if (_timeSpan > 1)
        {
            _timeSpan = 0;
            PlayerModel.Single.GodProcess -= 6;
            MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_UPDATE_GOD);

            PlayerModel.Single.IsGetItem = false;
            if (PlayerModel.Single.GodProcess >= 80)
            {
                PlayerModel.Single.IsGetItem = true;
            }
        }
    }

    private void OnDestroy()
    {
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_TWINKLE_SELF, TwinkleSelf);

        transform.DOKill();
    }

    public override void Hurt(Bullet bullet, Vector3 hitPoint)
    {
        if (PlayerModel.Single.State == PlayerState.NORMAL)
        {
            Dead();
        }
    }

    public override void Dead()
    {
        AudioMgr.Single.PlayGameEff(AudioType.PlayerDead);
        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_CLEAR_ENEMY_BULLET);

        PlayerModel.Single.GodProcess -= 50;
        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_UPDATE_GOD);

        if (GameStateModel.Single.IsCard && GameModel.Single.CardBonus > 1000)
        {
            GameModel.Single.CardBonus -= 1000;
        }

        if (GameStateModel.Single.GameDegree == Degree.LUNATIC)
        {
            MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_PLAYER_USE_LIFE);

            transform.position = Const.DEAD_POS;
            transform.DOLocalMoveY(Const.BORN_POS.y, 1f);
        }

        ReBirth();
    }

    public void OnGraze(Bullet bullet, Vector3 hitPoint)
    {
        if(!GameStateModel.Single.IsPause && !GameStateModel.Single.IsChating && !bullet.IsGrazed)
        {
            ++PlayerModel.Single.Graze;
            _view.PlayGrazeEffect();
            AudioMgr.Single.PlayGameEff(AudioType.Graze);

            PlayerModel.Single.GodProcess += 8;
            MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_UPDATE_GOD);

            MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_UPDATE_POINT);
            MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_UPDATE_GRAZE);

            bullet.IsGrazed = true;
        }
    }

    private void ReBirth()
    {
        _receiver.enabled = false;
        PlayerModel.Single.State = PlayerState.INVINCIBLE;

        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_TWINKLE_SELF, 20);
    }

    private void TwinkleSelf(object[] args)
    {
        int times = (int)args[0];

        SpriteRenderer renderer = GetComponent<SpriteRenderer>();

        renderer.color = new Color(1, 1, 1, 0);
        //无敌时间 = times * 0.15
        //放B：无敌6s    掉残：无敌3s
        renderer.DOKill();
        renderer.DOFade(1, 0.15f).SetLoops(times, LoopType.Restart).OnComplete(() =>
        {
            PlayerModel.Single.State = PlayerState.NORMAL;
            _receiver.enabled = true;
        });
    }
}
