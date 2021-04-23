using BulletPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBehaviour : BehaviourBase
{
    public EnemyView _view;
    public EnemyController _controller;
    private bool _isDead = false;

    public override void Hurt(Bullet bullet, Vector3 hitPoint)
    {
        base.Hurt(bullet, hitPoint);

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

            PlayerModel.Single.MemoryProcess += Random.Range(1, 4);
            if (PlayerModel.Single.MemoryProcess >= 100)
            {
                PlayerModel.Single.MemoryProcess -= 100;
                ++PlayerModel.Single.MemoryFragment;
            }
            MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_UPDATE_MEMORY);

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

    public void SetDead()       //当妖精自动越界时设置死亡
    {
        _isDead = true;
    }
}
