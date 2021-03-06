using BulletPro;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : EntityControllerBase
{
    private BulletEmitter _emitter;
    private EnemyData _enemyData;
    private List<int> _listTimeID = new List<int>();

    private EntityBehaviourBase _behaviour;

    public override void Init(EntityData enemyData)
    {
        base.Init(enemyData);

        _behaviour = GetComponent<EnemyBehaviour>();
        _emitter = GetComponent<BulletEmitter>();

        _enemyData = (EnemyData)enemyData;

        _emitter.emitterProfile = _enemyData.Emitter;
        transform.localPosition = _enemyData.BornPos;
        _lastPos = transform.position;

        List<List<Vector3>> path = _enemyData.Path;

        MessageMgr.Single.AddListener(MsgEvent.EVENT_CLEAR_ENEMY_BULLET, KillBullet);

        if(path.Count > 1)                              //这么写为了能够提高代码的可读性
        {
            DoPathA(path, _enemyData);                  //A方法：动画会暂停一段时间并且发弹幕，暂停时间过后继续做路径动画且停止弹幕

            return;
        }

        DoPathB(path, _enemyData);                  //B方法：一遍做路径动画，且不会暂停，在动画开始的一定时间后发射弹幕
    }

    public override void Update()
    {
        base.Update();

        if (!GameUtil.JudgeEnemyShot(_lastPos))
        {
            _emitter.Pause();
        }
    }

    private void OnDestroy()
    {
        transform.DOKill();

        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_CLEAR_ENEMY_BULLET, KillBullet);
    }

    private void DoPathA(List<List<Vector3>> path, EnemyData enemyData)
    {
        transform.DOPath(path[0].ToArray(), enemyData.PathDurUP).SetEase(Ease.Linear);
        int tid1 =  TimeMgr.Single.AddTimeTask(() =>
        {
            _emitter.Play();

            int tid2 = TimeMgr.Single.AddTimeTask(() =>
            {
                _emitter.Stop();
                transform.DOPath(path[1].ToArray(), enemyData.PathDurDown).SetEase(Ease.Linear).OnComplete(() =>
                {
                    _behaviour.IsDead = true;
                    EnemySpawnMgr.DeSpawn(gameObject);
                });

            }, enemyData.PauseTime, TimeUnit.Second);
            _listTimeID.Add(tid2);

        }, enemyData.PathDurUP, TimeUnit.Second);
        _listTimeID.Add(tid1);
    }

    private void DoPathB(List<List<Vector3>> path, EnemyData enemyData)
    {
        transform.DOPath(path[0].ToArray(), enemyData.PathDurUP + enemyData.PathDurDown).SetEase(Ease.Linear).OnComplete(() =>
        {
            EnemySpawnMgr.DeSpawn(gameObject);
            _behaviour.IsDead = true;
        });

        int tid3 = TimeMgr.Single.AddTimeTask(() =>
        {
            _emitter.Play();
        }, enemyData.PathDurUP, TimeUnit.Second);

        _listTimeID.Add(tid3);
    }

    private void KillBullet(object[] args)
    {
        _emitter.Kill(KillOptions.AllBulletsButRoot);
    }

    public void DieController()
    {
        foreach (int tid in _listTimeID)
        {
            TimeMgr.Single.RemoveTimeTask(tid);
        }

        enabled = false;

        _emitter.Stop();
    }
}
