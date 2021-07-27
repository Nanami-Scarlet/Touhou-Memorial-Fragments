using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using BulletPro;

public class EliteController : EntityControllerBase
{
    private ElitleData _elitleData;

    public List<BulletEmitter> _emitters;

    public Sequence MovePath { get; set; }

    public override void Init(EntityData data)
    {
        base.Init(data);

        _elitleData = (ElitleData)data;
        transform.localPosition = _elitleData.BornPos;

        for(int i = 0; i < _elitleData.EmitterPos.Count; ++i)
        {
            _emitters[i].transform.localPosition = _elitleData.EmitterPos[i];
            _emitters[i].emitterProfile = _elitleData.Emitters[i];
        }

        Move(_elitleData.AppearPath, Fire);
        GetComponent<SpriteRenderer>().enabled = true;
        SetMovePath();
    }

    public override void Update()
    {
        base.Update();
    }

    private void Move(Vector3 pos, Action cb = null)
    {
        float offset = (transform.localPosition - pos).magnitude;
        transform.DOLocalMove(pos, offset / 4).SetEase(Ease.Linear).OnComplete(() => 
        {
            if (cb != null)
            {
                cb();
            }
        });
    }

    private void Exit(Vector3 pos)
    {
        MovePath.Kill();

        float offset = (transform.localPosition - pos).magnitude;
        transform.DOLocalMove(pos, offset / 4).SetEase(Ease.Linear).OnComplete(() =>
        {
            PoolMgr.Single.Despawn(gameObject);
            --GameModel.Single.EnemyCount;
        });
    }

    private void Fire()
    {
        for (int i = 0; i < _elitleData.EmitterPos.Count; ++i)
        {
            _emitters[i].Play();
        }
    }

    public void KillFire()
    {
        for (int i = 0; i < _elitleData.EmitterPos.Count; ++i)
        {
            _emitters[i].Kill();
        }
    }

    private void SetMovePath()
    {
        MovePath.Kill();
        List<List<Vector3>> path = _elitleData.MovePath;
        List<float> delay = _elitleData.Delay;
        List<float> dur = _elitleData.Duration;
        float tl = _elitleData.TimeLimit;
        float mt = _elitleData.MoveTime;

        MovePath = DOTween.Sequence();
        float tot = mt;
        for(int i = 0; i < path.Count; ++i)
        {
            MovePath.Insert(tot, transform.DOPath(path[i].ToArray(), dur[i]).SetEase(Ease.Linear));
            tot += delay[i] + dur[i];
        }

        MovePath.InsertCallback(tl, () =>
        {
            KillFire();

            Exit(_elitleData.ExitPath);
        });

        MovePath.PlayForward();
    }
}
