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

    private Color _selected = Color.white;
    private Color _unselected = Color.black;
    
    public int MAX_INDEX { get; private set; }

    public override void InitChild()
    {
        ReSetAnim();
        PlayAnim();

        MAX_INDEX = _options.Length;


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

        _transTitle.DOLocalMove(new Vector3(-295, 302.16f, 0), 1);

        foreach(Text text in _options)
        {
            text.DOFade(1, 1.5f);
        }
    }

    private void Selected(Text text)
    {
        text.color = _selected;
        text.transform.DOShakePosition(1, 2);
    }

    private void UnSelected(Text text)
    {
        text.color = _unselected;
    }

    private void ReSetAnim()
    {
        _imgSacrifice.color = new Color(1, 1, 1, 0);
        _imgSacrifice.transform.localRotation = new Quaternion(0, 0, 0.7071068f, 0.7071068f);
    }
}
