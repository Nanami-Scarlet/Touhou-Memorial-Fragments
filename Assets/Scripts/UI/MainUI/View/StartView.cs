using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StartView : ViewBase
{ 
    public Transform _transTitle;
    public Image _imgSacrifice;
    public Text[] _options;
    
    public int MAX_INDEX { get; private set; }

    private Dictionary<Text, Color> _dicTextColor;

    public override void InitAndChild()
    {
        MAX_INDEX = _options.Length;

        _dicTextColor = new Dictionary<Text, Color>()
        {
            { _options[0], Color.black },
            { _options[1], new Color(0.3f, 0.3f, 0.2f) },
            { _options[2], new Color(0.3f, 0.3f, 0.2f) },
            { _options[3], new Color(0.3f, 0.3f, 0.2f) },
            { _options[4], Color.black },
            { _options[5], Color.black },
        };
    }

    public override void Show()
    {
        ResetAnim();
        PlayAnim();
    }

    public override void UpdateFun()
    {
        foreach(Text text in _options)
        {
            UnSelected(text);
        }

        Selected(_options[GameStateModel.Single.SelectedOption]);
    }

    private void PlayAnim()
    {
        _imgSacrifice.transform.DOLocalRotate(Vector3.zero, 1f);
        _imgSacrifice.DOFade(1, 1);

        _transTitle.DOLocalMoveX(0, 1);

        foreach (Text text in _options)
        {
            text.DOFade(1, 1.5f);
        }
    }

    private void Selected(Text text)
    {
        text.color = Color.white;
        text.transform.DOShakePosition(0.5f, 1);
    }

    private void UnSelected(Text text)
    {
        text.color = _dicTextColor[text];
    }

    private void ResetAnim()
    {
        _imgSacrifice.color = new Color(1, 1, 1, 0);
        _imgSacrifice.transform.localEulerAngles = Vector3.forward * 90;
    }
}
