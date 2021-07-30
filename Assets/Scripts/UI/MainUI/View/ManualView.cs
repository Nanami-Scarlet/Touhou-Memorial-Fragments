using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class ManualView : ViewBase
{
    public Text[] _txtOpation;
    public Transform _transOptionsDetail;

    private Color _selectedColor = new Color(1, 0.3f, 0.6f, 1);
    private Color _unselectedColor = new Color(0.3f, 0.3f, 0.3f, 1);

    public Transform _transTitle;
    public Transform _transOpation;
    public Transform[] _transTextDetail;

    public int MAX_INDEX { get; set; }

    public override void InitAndChild()
    {
        MAX_INDEX = 4;
    }

    public override void Show()
    {
        base.Show();

        ResetAnim();
        PlayAnim();
    }

    public void UpdateView(bool pre)
    {
        for(int i = 0; i < _txtOpation.Length; ++i)
        {
            _txtOpation[i].color = _unselectedColor;
        }

        _txtOpation[GameStateModel.Single.ManualOPation].color = _selectedColor;

        int offset = pre ? 700 : -700; 
        _transOptionsDetail.DOBlendableMoveBy(Vector3.up * offset, 0.5f).SetUpdate(true);
    }

    private void PlayAnim()
    {
        _transTitle.DOLocalMoveY(0, 0.8f).SetUpdate(true);
        _transOpation.DOLocalMoveX(0, 0.8f).SetUpdate(true);

        for (int i = 0; i < _transOptionsDetail.childCount; ++i)
        {
            Transform childTrans = _transOptionsDetail.GetChild(i);

            for (int j = 0; j < childTrans.childCount; ++j)
            {
                Transform trans = childTrans.GetChild(j);

                Image image = trans.GetComponent<Image>();
                if (image != null)
                {
                    image.DOFade(1, 0.8f).SetUpdate(true);
                }

                Text text = trans.GetComponent<Text>();
                if (text != null)
                {
                    text.DOFade(1, 0.8f).SetUpdate(true);
                }
            }
        }
    }

    private void ResetAnim()
    {
        _transTitle.localPosition = Vector3.up * 200;
        _transOpation.localPosition = Vector3.left * 400;

        Color t;
        for(int i = 0; i < _transOptionsDetail.childCount; ++i)
        {
            Transform childTrans = _transOptionsDetail.GetChild(i);

            for(int j = 0; j < childTrans.childCount; ++j)
            {
                Transform trans = childTrans.GetChild(j);

                Image image = trans.GetComponent<Image>();
                if (image != null)
                {
                    t = image.color;
                    image.color = new Color(t.r, t.g, t.b, 0);
                }

                Text text = trans.GetComponent<Text>();
                if (text != null)
                {
                    t = text.color;
                    text.color = new Color(t.r, t.g, t.b, 0);
                }
            }
        }
    }
}
