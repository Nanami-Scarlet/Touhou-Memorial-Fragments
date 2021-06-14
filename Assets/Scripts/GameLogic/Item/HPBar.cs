using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class HPBar : MonoBehaviour
{
    private float _normalHP;
    private float _cardHP;
    private int _index;
    private bool _isFinalCard = false;

    private bool _isFollow = false;
    public bool IsFollow
    {
        get
        {
            return _isFollow;
        }

        set
        {
            _isFollow = value;

            for (int i = 0; i < _imgBars.Length; ++i)
            {
                _imgBars[i].enabled = _isFollow;
            }
        }
    }

    private RectTransform _rectSelf;
    private Transform _transBoss;

    public Image[] _imgBars;

    public void Init()
    {
        _rectSelf = GetComponent<RectTransform>();
    }

    private void Update()
    {
        if (IsFollow)
        {
            _rectSelf.localPosition = Camera.main.WorldToScreenPoint(_transBoss.position);
        }
    }

    public void SetHPBar(Transform boss, float normalHP, float cardHP, int hpIndex, bool isFinalCard)
    {
        _transBoss = boss;
        _normalHP = normalHP;
        _cardHP = cardHP;
        _index = hpIndex;
        _isFinalCard = isFinalCard;

        _imgBars[1].enabled = false;
        _imgBars[2].enabled = false;
        _imgBars[hpIndex].enabled = true;

        enabled = true;
        IsFollow = true;
        _imgBars[hpIndex].fillAmount = 0;
        _imgBars[hpIndex].DOFillAmount(1, 1.5f);
    }

    public void SetHPView(int curHP)
    {
        float ratio;
        if (!_isFinalCard)
        {
            if (curHP > _cardHP)            //非符
            {
                ratio = Const.NORMAL_RATIO / _normalHP * (curHP - _cardHP) + Const.CARD_RATIO;
            }
            else
            {
                ratio = Const.CARD_RATIO / _cardHP * curHP;
            }

            _imgBars[_index].fillAmount = ratio / 100;
        }
        else
        {
            ratio = curHP / _cardHP;

            _imgBars[_index].fillAmount = ratio;
        }
    }
}
