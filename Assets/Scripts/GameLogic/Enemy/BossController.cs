using BulletPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;

public class BossController : MonoBehaviour
{
    private Animator _anim;
    private Vector3 _lastPos;

    private SingleBossInitData _bossData;

    public List<BulletEmitter> _emitters;
    private BulletReceiver _receiver;

    private Sequence _sequence;

    public void Init(SingleBossInitData data)
    {
        _bossData = data;

        transform.localPosition = data.BornPos;

        _anim = GetComponent<Animator>();
        _receiver = GetComponent<BulletReceiver>();
        _receiver.enabled = false;

        //MessageMgr.Single.AddListener(MsgEvent.EVENT_CLEAR_ENEMY_BULLET, KillBullet);
    }

    private void Update()
    {
        if (!GameStateModel.Single.IsPause)
        {
            _anim.SetInteger("Speed", GetOffsetX());
            _lastPos = transform.position;
        }

        //todo:如果Boss出界，应该setactive
    }

    //private void OnDestroy()
    //{
    //    transform.DOKill();
    //}

    private int GetOffsetX()
    {
        if (transform.position.x == _lastPos.x)
        {
            return 0;
        }

        return transform.position.x - _lastPos.x < 0 ? -1 : 1;
    }

    public void Appear(Action cb)
    {
        transform.DOPath(_bossData.AppearPath.ToArray(), 1f).SetEase(Ease.Linear).OnComplete(() => 
        {
            TimeMgr.Single.AddTimeTask(() => 
            {
                cb();
            }, 0.2f, TimeUnit.Second);
        });
    }

    public void ResetCard(SingleBossCardData data)
    {
        List<List<Vector3>> path = data.Path;
        List<float> dur = data.Duration;
        List<float> delay = data.Delay;
        List<EmitterProfile> emitter = data.Emitters;

        for(int i = 0; i < _emitters.Count; ++i)
        {
            if(_emitters[i].emitterProfile != null)
            {
                _emitters[i].Kill();
                _emitters[i].emitterProfile = null;
            }
        }

        for(int i = 0; i < emitter.Count; ++i)
        {
            _emitters[i].emitterProfile = emitter[i];

            if(_emitters[i].emitterProfile != null)
            {
                _emitters[i].Play();
            }
        }

        _sequence.Kill();
        _sequence = DOTween.Sequence();
        float tot = 0;
        for(int i = 0; i < path.Count; ++i)
        {
            tot += delay[i];
            _sequence.Insert(tot, transform.DOPath(path[i].ToArray(), dur[i]));
        }

        _sequence.PlayForward();
    }

    private void KillBullet(object[] args)
    {
        StopCard(true);
    }

    public void StopCard(bool isPlayerDie = false)
    {
        for (int i = 0; i < _emitters.Count; ++i)
        {
            if (_emitters[i].emitterProfile != null)
            {
                if (isPlayerDie)
                {
                    _emitters[i].Kill(KillOptions.AllBulletsButRoot);
                }
                else
                {
                    _emitters[i].Kill();
                    _emitters[i].emitterProfile = null;
                }
            }
        }
    }

    public void Move(Vector3 pos)
    {
        transform.DOLocalMove(pos, 1.5f).SetEase(Ease.Linear);
    }

    public void SetReceiver(bool pre)
    {
        _receiver.enabled = pre;
    }
}
