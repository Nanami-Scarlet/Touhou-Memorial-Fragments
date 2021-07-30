using BulletPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBehaviour : EntityBehaviourBase
{
    public EnemyView _view;
    public EnemyController _controller;

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
            _controller.DieController();
            _view.DieView();
            AudioMgr.Single.PlayGameEff(AudioType.EnemyDead);

            SpawnItems();

            PlayerModel.Single.MemoryProcess += Random.Range(1, 4);
            MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_UPDATE_MEMORY);

            GetComponent<BulletReceiver>().enabled = false;
            //等待粒子系统播放完毕
            TimeMgr.Single.AddTimeTask(() =>
            {
                if (!IsDead)             //有的妖精被符卡击破，所以这里需要判一下
                {
                    EnemySpawnMgr.DeSpawn(gameObject);
                    IsDead = true;
                }
            }, 0.7f, TimeUnit.Second);
        }
    }
}
