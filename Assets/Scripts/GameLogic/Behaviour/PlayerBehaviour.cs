using BulletPro;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBehaviour : BehaviourBase
{
    public BulletReceiver _receiver;

    public void Init()
    {
        MessageMgr.Single.AddListener(MsgEvent.EVENT_TWINKLE_SELF, TwinkleSelf);
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
            AudioMgr.Single.PlayGameEff(AudioType.Graze);

            MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_UPDATE_POINT);
            MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_UPDATE_GRAZE);

            bullet.IsGrazed = true;
        }
    }

    private void ReBirth()
    {
        _receiver.enabled = false;
        PlayerModel.Single.State = PlayerState.INVINCIBLE;

        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_TWINKLE_SELF);
    }

    private void TwinkleSelf(object[] args)
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();

        renderer.color = new Color(1, 1, 1, 0);
        renderer.DOFade(1, 0.15f).SetLoops(20, LoopType.Restart).OnComplete(() =>
        {
            PlayerModel.Single.State = PlayerState.NORMAL;
            _receiver.enabled = true;
        });
    }
}
