using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DegreeView : ViewBase
{
    public Transform _transTitle;
    public Image _imgChoice;
    public Text[] _options;

    private Sequence _sequence;
    private int _curIndex = 0;

    public int MAX_INDEX { get; private set; }

    public override void InitChild()
    {
        ResetAnim();
        PlayAnim();
        MAX_INDEX = 2;
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
    }
}
