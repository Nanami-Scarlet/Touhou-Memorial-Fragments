using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ThanksView : ViewBase
{
    public Image _imgBg;
    public Text _txtDetail;

    public override void InitAndChild()
    {
        
    }

    public override void Show()
    {
        base.Show();
        ResetAnim();

        _imgBg.DOFade(1, 1).SetEase(Ease.Linear);
        _txtDetail.transform.DOLocalMoveY(2000, 50f).SetEase(Ease.Linear);
    }

    private void ResetAnim()
    {
        _txtDetail.transform.localPosition = Vector3.up * -5550;

        Color t = _imgBg.color;
        _imgBg.color = new Color(t.r, t.g, t.b, 0);
    }
}
