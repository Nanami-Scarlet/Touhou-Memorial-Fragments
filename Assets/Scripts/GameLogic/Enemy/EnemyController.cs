using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    private Animator _anim;
    private Vector3 _lastPos;

    public void Init(EnemyData enemyData)
    {
        _anim = GetComponent<Animator>();

        transform.position = enemyData.BornPos;
        _lastPos = transform.position;

        List<List<Vector3>> path = enemyData.Path;

        transform.DOPath(path[0].ToArray(), enemyData.PathDurUP).SetEase(Ease.Linear);
        TimeMgr.Single.AddTimeTask(() =>
        {
            if (path.Count > 1)
            {
                TimeMgr.Single.AddTimeTask(() =>
                {
                    transform.DOPath(path[1].ToArray(), enemyData.PathDurDown).SetEase(Ease.Linear).OnComplete(() => PoolMgr.Single.DeSpawn(gameObject));
                }, enemyData.PauseTime, TimeUnit.Second);
            }
            else
            {
                PoolMgr.Single.DeSpawn(gameObject);
            }

        }, enemyData.PathDurUP, TimeUnit.Second);   //上去一段路程得停止一段时间
    }

    private void Update()
    {
        _anim.SetInteger("Speed", GetOffsetX());
        _lastPos = transform.position;
    }

    private int GetOffsetX()
    {
        if(transform.position.x == _lastPos.x)
        {
            return 0;
        }

        return transform.position.x - _lastPos.x < 0 ? -1 : 1;
    }
}
