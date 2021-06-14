using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DegreeView : ViewBase
{
    public Transform _transTitle;
    public Image _imgChoice;
    public Text[] _txtOptions;

    private Outline _olNormal;
    private Outline _olLunatic;

    private Sequence _sequence;
    private int _curIndex = 0;

    private Color _clrNormal = new Color(0, 0.4f, 1);
    private Color _clrLunatic = new Color(0.89f, 0, 1);

    public int MAX_INDEX { get; private set; }

    public override void InitAndChild()
    {
        MAX_INDEX = 2;

        _olNormal = _txtOptions[0].gameObject.GetComponent<Outline>();
        _olLunatic = _txtOptions[1].gameObject.GetComponent<Outline>();
    }

    public override void Show()
    {
        ResetAnim();
        PlayAnim();
    }

    public override void UpdateFun()
    {
        int offset = _curIndex - GameStateModel.Single.RankOption;
        
        _imgChoice.transform.DOBlendableLocalMoveBy(new Vector3(0, offset * 300, 0), 0.3f);

        _curIndex = GameStateModel.Single.RankOption;
    }

    private void PlayAnim()
    {
        _sequence = DOTween.Sequence();
        _sequence.Append(_transTitle.DOLocalMoveY(373, 0.5f));
        _sequence.Append(_imgChoice.DOFade(0.21f, 0.3f));
        _sequence.PlayForward();
    }

    private void ResetAnim()
    {
        _transTitle.transform.localPosition = new Vector3(0, 850, 0);
        _imgChoice.color = new Color(1, 1, 1, 0);


        _txtOptions[0].color = Color.white;
        _txtOptions[0].color = Color.white;
        _olNormal.effectColor = _clrNormal;
        _olLunatic.effectColor = _clrLunatic;
    }
}
