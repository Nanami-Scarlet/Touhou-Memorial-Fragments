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

        if (HP <= 0 && !_isSpawnItem)
        {
            //SpawnItems();
            Dead();
            _isSpawnItem = true;
        }

        GameModel.Single.Score += Const.BULLET_SCORE;
        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_UPDATE_SCORE, GameModel.Single.Score);
    }

    public override void Dead()
    {
        if (!_isDead)
        {
            _controller.DieController();
            _view.DieView();

            SpawnItems();

            if (PlayerModel.Single.MemoryFragment < 3)
            {
                PlayerModel.Single.MemoryProcess += Random.Range(1, 4);
                if (PlayerModel.Single.MemoryProcess >= 100)
                {
                    PlayerModel.Single.MemoryProcess -= 100;
                    ++PlayerModel.Single.MemoryFragment;
                }
                MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_UPDATE_MEMORY);
            }

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
