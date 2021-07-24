using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PauseView : ViewBase
{
    public Text[] _options;

    public int MAX_INDEX { get; set; }

    private Dictionary<Text, Color> _dicTextColor;

    public override void InitAndChild()
    {
        MAX_INDEX = 2;

        _dicTextColor = new Dictionary<Text, Color>()
        {
            { _options[0], Color.black },
            { _options[1], Color.black },
            { _options[2], new Color(0.3f, 0.3f, 0.2f) },
        };
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
        text.color = Color.white;
        text.transform.DOShakePosition(1, 1);
    }

    private void UnSelected(Text text)
    {
        text.color = _dicTextColor[text];
    }
}
