using BulletPro;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EliteBehaviour : EntityBehaviourBase
{
    public EnemyView _view;
    public EliteController _controller;

    public override void Hurt(Bullet bullet, Vector3 hitPoint)
    {
        base.Hurt(bullet, hitPoint);
        HP -= bullet.moduleParameters.GetInt("_CardPower");

        if (HP <= 0 && !IsSpawnItem)
        {
            Dead();
            IsSpawnItem = true;
        }
    }

    public override void Dead()
    {
        if (!IsDead)
        {
            _view.DieView();
            AudioMgr.Single.PlayGameEff(AudioType.EnemyDead);

            _controller.KillFire();
            _controller.MovePath.Kill();
            _controller.enabled = false;
            SpawnItems();

            PlayerModel.Single.MemoryProcess += Random.Range(10, 20);
            MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_UPDATE_MEMORY);

            GetComponent<BulletReceiver>().enabled = false;
            TimeMgr.Single.AddTimeTask(() =>
            {
                if (!IsDead)
                {
                    EliteSpawnMgr.DeSpawn(gameObject);
                    IsDead = true;
                }
            }, 0.7f, TimeUnit.Second);
        }
    }
}
