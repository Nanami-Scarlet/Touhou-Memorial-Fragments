using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class DynamicView : ViewBase
{
    private Dictionary<string, HPBar> _dicIDHPBar = new Dictionary<string, HPBar>();

    public Text _textBGMInfo;
    public Transform _hpItems;

    public Text _txtBossName;
    public Image[] _imgCards;

    public Transform _transTimer;
    public Text _txtSec;
    public Text _txtMili;

    public Transform _transCard;
    public Transform _transCardInfo;
    public Text _txtCardName;
    public Image _imgCardLine;
    public Text _txtBonusLabel;
    public Text _txtBonusDetail;
    public Text _txtHistoryLabel;
    public Text _txtHistoryDetail;

    public Image[] _imgCardPics;
    private Dictionary<string, CardPicData> _dicCardPicData;
    private Dictionary<string, Sprite> _dicNameSprite;

    private Color _clrRed = Color.red;
    private Color _clrWhite = Color.white;

    public override void InitAndChild()
    {
        _dicCardPicData = DataMgr.Single.GetCardPicData();

        _dicNameSprite = DataMgr.Single.GetNameSpriteData();
    }

    public override void Show()
    {
        base.Show();

        ResetCardAnim();
        SetTimerActive(false);
        ResetAnim();
    }

    private void ResetAnim()
    {
        _textBGMInfo.color = new Color(1, 1, 1, 1);
        _textBGMInfo.transform.localPosition = Vector3.right * 132;

        for(int i = 0; i < _imgCardPics.Length; ++i)
        {
            _imgCardPics[i].enabled = false;
        }
    }

    public void BGMSetting(string stage)
    {
        AudioData data = DataMgr.Single.GetBGMData(stage);
        AudioMgr.Single.PlayBGM(stage + "_BGM", data.Volume);
        _textBGMInfo.text = "BGM:" + data.Name;

        _textBGMInfo.transform.DOLocalMoveX(-420f, 1).OnComplete(() =>
        {
            TimeMgr.Single.AddTimeTask(() =>
            {
                _textBGMInfo.DOFade(0, 0.5f).OnComplete(() =>
                {
                    ResetAnim();
                });
            }, 3f, TimeUnit.Second);
        });
    }

    public void AddHPBar(string goName)
    {
        if (!_dicIDHPBar.ContainsKey(goName))
        {
            GameObject barGo = LoadMgr.Single.LoadPrefabAndInstantiate(Paths.PREFAB_HPBAR, _hpItems);
            barGo.transform.localPosition = Vector3.left * 500;
            HPBar hpBar = barGo.GetComponent<HPBar>();
            hpBar.Init();

            _dicIDHPBar.Add(goName, hpBar);
        }
    }

    public void SetHPBar(BaseCardHPData data, bool isFinalCard)
    {
        Transform boss = data.TransBoss;
        float normalHP = data.NormalHP;
        float cardHP = data.CardHP;
        int barIndex = data.BarIndex;

        GameObject bossGo = data.TransBoss.gameObject;

        if (JudgeContainsKey(bossGo))
        {
            _dicIDHPBar[bossGo.name].SetHPBar(boss, normalHP, cardHP, barIndex, isFinalCard);
        }
    }

    public void ClearAllHPBar()
    {
        foreach(var pair in _dicIDHPBar)
        {
            pair.Value.transform.localPosition = Vector3.left * 500;
            pair.Value.enabled = false;
        }
    }

    public void SetHPView(HPData data)
    {
        if (JudgeContainsKey(data.BossGO))
        {
            _dicIDHPBar[data.BossGO.name].SetHPView(data.CurHP);
        }
    }

    public void HideHPView(GameObject go)
    {
        if (JudgeContainsKey(go))
        {
            _dicIDHPBar[go.name].transform.localPosition = Vector3.left * 500;
            _dicIDHPBar[go.name].IsFollow = false;
        }
    }

    private bool JudgeContainsKey(GameObject bossGo)
    {
        if (!_dicIDHPBar.ContainsKey(bossGo.name) && !GameStateModel.Single.IsPause)
        {
            Debug.LogError("该Boss没有血条，Boss类型为：" + bossGo.name);
            return false;
        }

        return true;
    }

    public void ShowBossNameCard(BossNameCard data)
    {
        _txtBossName.enabled = true;
        _txtBossName.text = data.Name;

        for(int i = 0; i < _imgCards.Length; ++i)
        {
            _imgCards[i].enabled = false;
        }

        for(int i = 0; i < data.CardCount - 1; ++i)
        {
            _imgCards[i].enabled = true;
        }
    }

    public void HideBossNameCard()
    {
        _txtBossName.enabled = false;

        for (int i = 0; i < _imgCards.Length; ++i)
        {
            _imgCards[i].enabled = false;
        }
    }

    public void SetTimeText(int sec, int mili, bool isRed)
    {
        _txtSec.text = sec + ".";
        _txtMili.text = string.Format("{0:D2}", mili);

        if (isRed)
        {
            _txtSec.color = _clrRed;
            _txtMili.color = _clrRed;
        }
        else
        {
            _txtSec.color = _clrWhite;
            _txtMili.color = _clrWhite;
        }
    }

    public void SetTimerActive(bool pre)
    {
        _txtSec.enabled = pre;
        _txtMili.enabled = pre;
    }

    public void MoveTimer(int posY)
    {
        _transTimer.DOLocalMoveY(posY, 1);
    }

    public void PlayCardInfoAnim(string cardName)
    {
        _txtCardName.text = cardName;

        Sequence sequence = DOTween.Sequence();
        sequence.Insert(0, _txtCardName.DOFade(1, 1f));
        sequence.Insert(0.5f, _imgCardLine.DOFade(1, 0.5f));
        sequence.Insert(0.5f, _imgCardLine.transform.DOLocalMoveY(-16, 0.5f));
        sequence.Insert(0.5f, _txtBonusLabel.DOFade(1, 1));
        sequence.Insert(0.5f, _txtBonusDetail.DOFade(1, 1));
        sequence.Insert(0.5f, _txtHistoryLabel.DOFade(1, 1));
        sequence.Insert(0.5f, _txtHistoryDetail.DOFade(1, 1));
        sequence.Insert(1.5f, _transCard.DOLocalMoveY(44, 1f));

        sequence.PlayForward();
    }

    public void MoveCardInfoRight()
    {
        Sequence sequence = DOTween.Sequence();

        sequence.Join(_transCard.DOLocalMoveX(1300, 1));
        sequence.Join(_transCardInfo.DOLocalMoveX(1300, 1));
        sequence.OnComplete(() => ResetCardAnim());

        sequence.Play();
    }

    private void ResetCardAnim()
    {
        _transCard.localPosition = new Vector3(73, -775, 0);
        _transCardInfo.localPosition = new Vector3(-2.5f, 7, 0);
        _imgCardLine.transform.localPosition = new Vector3(-53, -35);

        Color t = _txtCardName.color;
        _txtCardName.color = new Color(t.r, t.g, t.b, 0);
        t = _imgCardLine.color;
        _imgCardLine.color = new Color(t.r, t.g, t.b, 0);
        t = _txtBonusLabel.color;
        _txtBonusLabel.color = new Color(t.r, t.g, t.b, 0);
        t = _txtHistoryLabel.color;
        _txtHistoryLabel.color = new Color(t.r, t.g, t.b, 0);
        t = _txtBonusDetail.color;
        _txtBonusDetail.color = new Color(t.r, t.g, t.b, 0);
        t = _txtHistoryDetail.color;
        _txtHistoryDetail.color = new Color(t.r, t.g, t.b, 0);
    }

    public void SetCardPic(string bossType)
    {
        CardPicData data = _dicCardPicData[bossType];

        int index = data.Index;
        _imgCardPics[index].enabled = true;
        _imgCardPics[index].rectTransform.sizeDelta = data.PicSize;
        _imgCardPics[index].sprite = _dicNameSprite[data.PicName];
    }

    public void PlayCardPicAnim()
    {
        _imgCardPics[0].rectTransform.localPosition = new Vector3(585, 328, 0);
        Sequence sequence1 = DOTween.Sequence();
        sequence1.Insert(0f, _imgCardPics[0].transform.DOLocalMove(new Vector3(185, 0, 0), 0.5f)).SetEase(Ease.Linear);
        sequence1.Insert(2f, _imgCardPics[0].transform.DOLocalMove(new Vector3(-185, -738, 0), 0.5f)).SetEase(Ease.Linear);
        sequence1.OnComplete(() => _imgCardPics[0].enabled = false);

        _imgCardPics[1].rectTransform.localPosition = new Vector3(-585, 328, 0);
        Sequence sequence2 = DOTween.Sequence();
        sequence2.Insert(0f, _imgCardPics[1].transform.DOLocalMove(new Vector3(-185, 0, 0), 0.5f)).SetEase(Ease.Linear);
        sequence2.Insert(2f, _imgCardPics[1].transform.DOLocalMove(new Vector3(185, -738, 0), 0.5f)).SetEase(Ease.Linear);
        sequence2.OnComplete(() => _imgCardPics[1].enabled = false);

        sequence1.PlayForward();
        sequence2.PlayForward();
    }
}
