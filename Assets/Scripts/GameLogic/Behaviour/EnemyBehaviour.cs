using BulletPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBehaviour : BehaviourBase
{
    public EnemyView _view;
    public EnemyController _controller;
    public bool _isDead = false;

    private bool _isSpawnItem = false;

    public bool IsDead
    {
        get
        {
            return _isDead;
        }

        set
        {
            _isDead = value;
        }
    }

    public override void Hurt(Bullet bullet, Vector3 hitPoint)
    {
        base.Hurt(bullet, hitPoint);
        HP -= bullet.moduleParameters.GetInt("_CardPower");

        if (HP <= 0 && !_isSpawnItem)
        {
            //SpawnItems();
            Dead();
            _isSpawnItem = true;
        }
    }

    public override void Dead()
    {
        if (!_isDead)
        {
            _controller.DieController();
            _view.DieView();

            SpawnItems();

            PlayerModel.Single.MemoryProcess += Random.Range(1, 4);
            MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_UPDATE_MEMORY);

            GetComponent<BulletReceiver>().enabled = false;
            //等待粒子系统播放完毕
            TimeMgr.Single.AddTimeTask(() =>
            {
                if (!_isDead)             //有的妖精被符卡击破，所以这里需要判一下
                {
                    EnemySpawnMgr.DeSpawn(gameObject);
                    _isDead = true;
                }
            }, 0.7f, TimeUnit.Second);

            //_isDead = true;
        }
    }

    public void ResetBehaiour()
    {
        _isSpawnItem = false;
        _isDead = false;
    }
}
