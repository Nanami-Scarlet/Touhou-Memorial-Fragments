using BulletPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossBehaviour : EntityBehaviourBase
{
    //public bool IsFinalCard { get; set; }

    public override void Hurt(Bullet bullet, Vector3 hitPoint)
    {
        base.Hurt(bullet, hitPoint);
        if (!GameStateModel.Single.IsFinalCard)
        {
            HP -= bullet.moduleParameters.GetInt("_CardPower");
        }

        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_SET_HPBAR_VIEW, new HPData(gameObject, HP));
    }

    public override void Dead()
    {
        BossSpawnMgr.DeSpawn(gameObject);
    }
}
