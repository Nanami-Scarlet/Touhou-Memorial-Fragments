using BulletPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;

public class BossController : EntityControllerBase
{
    private SingleBossInitData _bossData;

    public List<BulletEmitter> _emitters;
    private BulletReceiver _receiver;

    private Sequence _sequence;

    public override void Init(EntityData data)
    {
        base.Init(data);

        _receiver = GetComponent<BulletReceiver>();

        _bossData = (SingleBossInitData)data;

        transform.localPosition = _bossData.BornPos;
        _receiver.enabled = false;

        MessageMgr.Single.AddListener(MsgEvent.EVENT_PLAY_CARD_ANIM, PlayCardAnim);
        //MessageMgr.Single.AddListener(MsgEvent.EVENT_CANCEL_CARD_ANIM, CancelCardAnim);
    }

    public override void Update()
    {
        base.Update();
    }

    private void OnDestroy()
    {
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_PLAY_CARD_ANIM, PlayCardAnim);
        //MessageMgr.Single.RemoveListener(MsgEvent.EVENT_CANCEL_CARD_ANIM, CancelCardAnim);
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

    public void Move()
    {
        _sequence.Kill();
        Vector3 pos = _bossData.FinalMovePos;

        float offset = (transform.localPosition - pos).magnitude;
        transform.DOLocalMove(pos, offset / 4).SetEase(Ease.Linear)
            .OnUpdate(() => 
            {
                _anim.SetBool("Card", false);
                MessageMgr.Single.RemoveListener(MsgEvent.EVENT_PLAY_CARD_ANIM, PlayCardAnim);

            })
            .OnComplete(() => MessageMgr.Single.AddListener(MsgEvent.EVENT_PLAY_CARD_ANIM, PlayCardAnim));
    }

    public void Move(Vector3 pos)
    {
        _sequence.Kill();

        float offset = (transform.localPosition - pos).magnitude;
        transform.DOLocalMove(pos, offset / 4).SetEase(Ease.Linear)
             .OnUpdate(() =>
             {
                 _anim.SetBool("Card", false);
                 MessageMgr.Single.RemoveListener(MsgEvent.EVENT_PLAY_CARD_ANIM, PlayCardAnim);

             })
            .OnComplete(() => MessageMgr.Single.AddListener(MsgEvent.EVENT_PLAY_CARD_ANIM, PlayCardAnim));
    }

    public void ResetCard(SingleBossCardData data)
    {
        List<List<Vector3>> path = data.Path;
        List<float> dur = data.Duration;
        List<float> delay = data.Delay;
        List<EmitterProfile> emitter = data.Emitters;
        List<Vector3> emitterPos = data.EmittersPos;
        float mt = data.MoveTime;

        for(int i = 0; i < _emitters.Count; ++i)
        {
            if(_emitters[i].emitterProfile != null)
            {
                _emitters[i].Kill();
                _emitters[i].emitterProfile = null;
            }
        }

        for(int i = 0; i < emitterPos.Count; ++i)
        {
            _emitters[i].transform.localPosition = emitterPos[i];
            _emitters[i].emitterProfile = emitter[i];

            if(_emitters[i].emitterProfile != null)
            {
                BulletEmitter e = _emitters[i];

                e.Play();
            }
        }

        _sequence.Kill();
        _sequence = DOTween.Sequence();
        float tot = mt;
        for(int i = 0; i < path.Count; ++i)
        {
            _sequence.Insert(tot, transform.DOPath(path[i].ToArray(), dur[i]).SetEase(Ease.Linear)
                 .OnUpdate(() =>
                 {
                     _anim.SetBool("Card", false);
                     MessageMgr.Single.RemoveListener(MsgEvent.EVENT_PLAY_CARD_ANIM, PlayCardAnim);

                 })
                .OnComplete(() => MessageMgr.Single.AddListener(MsgEvent.EVENT_PLAY_CARD_ANIM, PlayCardAnim)));
            tot += delay[i] + dur[i];
        }

        _sequence.PlayForward();
    }

    private void PlayCardAnim(object[] args)
    {
        _anim.SetBool("Card", true);
    }

    //private void CancelCardAnim(object[] args)
    //{
    //    _anim.SetBool("Card", false);
    //}

    public void StopCard()
    {
        for (int i = 0; i < _emitters.Count; ++i)
        {
            if (_emitters[i].emitterProfile != null)
            {
                _emitters[i].Kill();
                _emitters[i].emitterProfile = null;
            }
        }
    }

    public void SetReceiver(bool pre)
    {
        _receiver.enabled = pre;
    }
}
