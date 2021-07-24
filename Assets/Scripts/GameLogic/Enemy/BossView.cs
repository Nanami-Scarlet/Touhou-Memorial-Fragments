using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class BossView : EntityViewBase
{
    public Transform _transMagic;
    private SpriteRenderer _renderer;
    private Sequence _sequence;

    private bool _isPlaying = false;
    private float _rotX = 40;
    private float _rotY = -40;
    private float _rotZ = -360;

    public void Init()
    {
        _renderer = _transMagic.GetComponent<SpriteRenderer>();
        _sequence = DOTween.Sequence();

        _sequence.Join(DOTween.To(() => _rotX, (x) => _rotX = x, -40, 5).SetEase(Ease.Linear));
        _sequence.Join(DOTween.To(() => _rotY, (y) => _rotY = y, 40, 10).SetEase(Ease.Linear));
        _sequence.Join(DOTween.To(() => _rotZ, (z) => _rotZ = z, 0, 10).SetEase(Ease.Linear));

        _sequence.SetLoops(-1, LoopType.Yoyo);
    }

    private void Update()
    {
        if(_isPlaying)
        {
            //注意：虽说尽量不要在Update里new变量
            //但是我测试的辉夜一非跑不跑这里的Update帧率差不多
            _transMagic.localEulerAngles = new Vector3(_rotX, _rotY, _rotZ);
        }
    }

    public void PlayMagicBGAnim()
    {
        _renderer.DOKill();
        _renderer.DOFade(0.7f, 1);

        _sequence.Play();
        _isPlaying = true;
    }

    public void StopMagicBGAnim()
    {
        _renderer.DOKill();
        _renderer.DOFade(0, 0.5f).OnComplete(() =>
        {
            _sequence.Pause();
            _isPlaying = false;
        });
    }
}
