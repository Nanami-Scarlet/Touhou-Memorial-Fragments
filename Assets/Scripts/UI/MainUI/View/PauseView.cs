using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PauseView : ViewBase
{
    public Text[] _options;

    public int MAX_INDEX { get; set; }

    public override void InitChild()
    {
        MAX_INDEX = 3;
    }

    public override void Show()
    {
        for (int i = 0; i < _options.Length; ++i)
        {
            if (i == 0)
            {
                Selected(_options[i]);
                continue;
            }
            UnSelected(_options[i]);
        }
    }

    public override void UpdateFun()
    {
        foreach (Text text in _options)
        {
            UnSelected(text);
        }

        Selected(_options[GameStateModel.Single.PauseOption]);
    }

    private void Selected(Text text)
    {
        text.color = Const.ColorSelect;
        text.transform.DOShakePosition(1, 1);
    }

    private void UnSelected(Text text)
    {
        text.color = Const.ColorUnSelect;
    }
}
