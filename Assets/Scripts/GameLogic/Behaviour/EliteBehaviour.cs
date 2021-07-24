using BulletPro;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EliteBehaviour : EntityBehaviourBase
{
    public EnemyView _view;
    public EliteController _controller;

    private bool _isSpawnItem = false;
    public bool _isDead = false;

    public override void Hurt(Bullet bullet, Vector3 hitPoint)
    {
        base.Hurt(bullet, hitPoint);
        HP -= bullet.moduleParameters.GetInt("_CardPower");

        if (HP <= 0 && !_isSpawnItem)
        {
            Dead();
            _isSpawnItem = true;
        }
    }

    public override void Dead()
    {
        if (!_isDead)
        {
            _view.DieView();
            AudioMgr.Single.PlayGameEff(AudioType.EnemyDead);

            _controller.KillFire();
            _controller.MovePath.Kill();
            SpawnItems();

            PlayerModel.Single.MemoryProcess += Random.Range(10, 20);
            MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_UPDATE_MEMORY);

            GetComponent<BulletReceiver>().enabled = false;
            TimeMgr.Single.AddTimeTask(() =>
            {
                if (!_isDead)
                {
                    EnemySpawnMgr.DeSpawn(gameObject);
                    _isDead = true;
                }
            }, 0.7f, TimeUnit.Second);
        }
    }

    public void ResetBehaiour()
    {
        _isSpawnItem = false;
        _isDead = false;
    }
}
