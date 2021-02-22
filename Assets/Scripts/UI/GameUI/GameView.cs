using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameView : ViewBase
{
    public float _animSpeed = 0.3f;

    public Transform _transRank;

    #region HighScore
    public Transform _transHighScore;
    private Text _txtHighLabel;
    private Text _txtHighNum;
    private Image _imgHighLine;
    #endregion

    #region CurrentScore
    public Transform _transCurScore;
    private Text _txtCurLabel;
    private Text _txtCurNum;
    private Image _imgCurLine;
    #endregion

    #region Life
    public Transform _transLife;
    private Text _txtLifeLabel;
    private Transform _transLifeItem;
    public Transform[] _transLifeItems;
    private Image _imgLifeLine;
    private Text _txtLifeFragment;
    private Text _txtLifeNum;
    private Text _txtLifeTotal;
    #endregion

    #region Bomb
    public Transform _transBomb;
    private Text _txtBombLabel;
    private Transform _transBombItem;
    public Transform[] _transBombItems;
    private Image _imgBombLine;
    private Text _txtBombFragment;
    private Text _txtBombNum;
    private Text _txtBombTotal;
    #endregion

    #region Mana
    public Transform _transMana;
    private Image _imgManaLine;
    private Image _imgMana;
    private Text _txtManaLabel;
    private Text _txtManaNum;
    private Text _txtManaTotal;
    #endregion

    #region Point
    public Transform _transPoint;
    private Image _imgPointLine;
    private Image _imgPoint;
    private Text _txtPointLabel;
    private Text _txtPointNum;
    #endregion

    #region Graze
    public Transform _transGraze;
    private Image _imgGrazeLine;
    private Text _txtGrazeLabel;
    private Text _txtGrazeNum;
    #endregion

    public Image _imgSacrifice;


    public override void InitChild()
    {
        _txtHighLabel = _transHighScore.GetChild(1).GetComponent<Text>();
        _txtHighNum = _transHighScore.GetChild(2).GetComponent<Text>();
        _imgHighLine = _transHighScore.GetChild(0).GetComponent<Image>();

        _txtCurLabel = _transCurScore.GetChild(1).GetComponent<Text>();
        _txtCurNum = _transCurScore.GetChild(2).GetComponent<Text>();
        _imgCurLine = _transCurScore.GetChild(0).GetComponent<Image>();

        _txtLifeLabel = _transLife.GetChild(1).GetComponent<Text>();
        _transLifeItem = _transLife.GetChild(2);
        _imgLifeLine = _transLife.GetChild(0).GetComponent<Image>();
        _txtLifeFragment = _transLife.GetChild(3).GetComponent<Text>();
        _txtLifeNum = _transLife.GetChild(4).GetComponent<Text>();
        _txtLifeTotal = _transLife.GetChild(5).GetComponent<Text>();

        _txtBombLabel = _transBomb.GetChild(1).GetComponent<Text>();
        _transBombItem = _transBomb.GetChild(2);
        _imgBombLine = _transBomb.GetChild(0).GetComponent<Image>();
        _txtBombFragment = _transBomb.GetChild(3).GetComponent<Text>();
        _txtBombNum = _transBomb.GetChild(4).GetComponent<Text>();
        _txtBombTotal = _transBomb.GetChild(5).GetComponent<Text>();

        _imgManaLine = _transMana.GetChild(0).GetComponent<Image>();
        _imgMana = _transMana.GetChild(1).GetComponent<Image>();
        _txtManaLabel = _transMana.GetChild(2).GetComponent<Text>();
        _txtManaNum = _transMana.GetChild(3).GetComponent<Text>();
        _txtManaTotal = _transMana.GetChild(4).GetComponent<Text>();

        _imgPointLine = _transPoint.GetChild(0).GetComponent<Image>();
        _imgPoint = _transPoint.GetChild(1).GetComponent<Image>();
        _txtPointLabel = _transPoint.GetChild(2).GetComponent<Text>();
        _txtPointNum = _transPoint.GetChild(3).GetComponent<Text>();

        _imgGrazeLine = _transGraze.GetChild(0).GetComponent<Image>();
        _txtGrazeLabel = _transGraze.GetChild(1).GetComponent<Text>();
        _txtGrazeNum = _transGraze.GetChild(2).GetComponent<Text>();
    }

    public override void Show()
    {
        ResetAnim();
        PlayAnim();
    }

    private void PlayAnim()
    {
        #region Rank动画
        int index = (int)GameStateModel.Single.SelectedDegree;
        Text textRank = GameUtil.SetSubActive(_transRank, index).GetComponent<Text>();
        Sequence rank = DOTween.Sequence();
        rank.Append(textRank.DOFade(1, 0.2f).SetLoops(4, LoopType.Restart));
        rank.Insert(2.1f, _transRank.DOLocalRotate(Vector3.right * 90, 0.2f));
        rank.AppendCallback(() => _transRank.localPosition = new Vector3(524, 425, 0));
        rank.Append(_transRank.DOLocalRotate(Vector3.zero, 0.2f));
        #endregion

        #region HighScore动画
        Sequence high = DOTween.Sequence();
        high.Join(_txtHighLabel.DOFade(1, _animSpeed));
        high.Join(_txtHighNum.DOFade(1, _animSpeed));
        high.Join(_imgHighLine.DOFade(0.5f, _animSpeed));
        high.Pause();
        #endregion

        #region CurrentScore动画
        Sequence cur = DOTween.Sequence();
        cur.Join(_txtCurLabel.DOFade(1, _animSpeed));
        cur.Join(_txtCurNum.DOFade(1, _animSpeed));
        cur.Join(_imgCurLine.DOFade(0.5f, _animSpeed));
        cur.Pause();
        #endregion

        #region Life动画
        Sequence life = DOTween.Sequence();
        life.Join(_txtLifeLabel.DOFade(1, _animSpeed));
        life.Join(_imgLifeLine.DOFade(0.5f, _animSpeed));
        life.Join(_txtLifeLabel.DOFade(1, _animSpeed));
        life.Join(_txtLifeFragment.DOFade(1, _animSpeed));
        life.Join(_txtLifeNum.DOFade(1, _animSpeed));
        life.Join(_txtLifeTotal.DOFade(1, _animSpeed));
        life.OnComplete(() => _transLifeItem.gameObject.SetActive(true));
        life.Pause();
        #endregion

        #region Bomb动画
        Sequence bomb = DOTween.Sequence();
        bomb.Join(_txtBombLabel.DOFade(1, _animSpeed));
        bomb.Join(_imgBombLine.DOFade(0.5f, _animSpeed));
        bomb.Join(_txtBombLabel.DOFade(1, _animSpeed));
        bomb.Join(_txtBombFragment.DOFade(1, _animSpeed));
        bomb.Join(_txtBombNum.DOFade(1, _animSpeed));
        bomb.Join(_txtBombTotal.DOFade(1, _animSpeed));
        bomb.OnComplete(() => _transBombItem.gameObject.SetActive(true));
        bomb.Pause();
        #endregion

        #region Mana动画
        Sequence mana = DOTween.Sequence();
        mana.Join(_imgManaLine.DOFade(0.5f, _animSpeed));
        mana.Join(_imgMana.DOFade(1, _animSpeed));
        mana.Join(_txtManaLabel.DOFade(1, _animSpeed));
        mana.Join(_txtManaNum.DOFade(1, _animSpeed));
        mana.Join(_txtManaTotal.DOFade(1, _animSpeed));
        mana.Pause();
        #endregion

        #region Point动画
        Sequence point = DOTween.Sequence();
        point.Join(_imgPointLine.DOFade(0.5f, _animSpeed));
        point.Join(_imgPoint.DOFade(1, _animSpeed));
        point.Join(_txtPointLabel.DOFade(1, _animSpeed));
        point.Join(_txtPointNum.DOFade(1, _animSpeed));
        point.Pause();
        #endregion

        #region Graze动画
        Sequence graze = DOTween.Sequence();
        graze.Join(_imgGrazeLine.DOFade(0.5f, _animSpeed));
        graze.Join(_txtGrazeLabel.DOFade(0.5f, _animSpeed));
        graze.Join(_txtGrazeNum.DOFade(0.5f, _animSpeed));
        graze.Pause();
        #endregion

        #region 动画进行播放
        high.Play();
        high.onComplete += () => cur.Play();
        cur.onComplete += () => life.Play();
        life.onComplete += () => bomb.Play();
        bomb.onComplete += () => mana.Play();
        mana.onComplete += () => point.Play();
        point.onComplete += () => graze.Play();
        #endregion

        _imgSacrifice.transform.DOLocalRotate(Vector3.zero, 0.5f);
        _imgSacrifice.DOFade(1, 0.5f);
    }

    private void ResetAnim()
    {
        int index = (int)GameStateModel.Single.SelectedDegree;
        _transRank.GetChild(index).GetComponent<Text>().color = new Color(1, 1, 1, 0);
        _transRank.localPosition = new Vector3(-60, 416, 0);

        Color t = _txtHighLabel.color;
        _txtHighLabel.color = new Color(t.r, t.g, t.b, 0);
        t = _txtHighNum.color;
        _txtHighNum.color = new Color(t.r, t.g, t.b, 0);
        t = _imgHighLine.color;
        _imgHighLine.color = new Color(t.r, t.g, t.b, 0);

        t = _txtCurLabel.color;
        _txtCurLabel.color = new Color(t.r, t.g, t.b, 0);
        t = _txtCurNum.color;
        _txtCurNum.color = new Color(t.r, t.g, t.b, 0);
        t = _imgCurLine.color;
        _imgCurLine.color = new Color(t.r, t.g, t.b, 0);

        t = _txtLifeLabel.color;
        _txtLifeLabel.color = new Color(t.r, t.g, t.b, 0);
        t = _imgLifeLine.color;
        _imgLifeLine.color = new Color(t.r, t.g, t.b, 0);
        t = _txtLifeLabel.color;
        _txtLifeLabel.color = new Color(t.r, t.g, t.b, 0);
        t = _txtLifeFragment.color;
        _txtLifeFragment.color = new Color(t.r, t.g, t.b, 0);
        t = _txtLifeNum.color;
        _txtLifeNum.color = new Color(t.r, t.g, t.b, 0);
        t = _txtLifeTotal.color;
        _txtLifeTotal.color = new Color(t.r, t.g, t.b, 0);
        _transLifeItem.gameObject.SetActive(false);

        t = _txtBombLabel.color;
        _txtBombLabel.color = new Color(t.r, t.g, t.b, 0);
        t = _imgBombLine.color;
        _imgBombLine.color = new Color(t.r, t.g, t.b, 0);
        t = _txtBombLabel.color;
        _txtBombLabel.color = new Color(t.r, t.g, t.b, 0);
        t = _txtBombFragment.color;
        _txtBombFragment.color = new Color(t.r, t.g, t.b, 0);
        t = _txtBombNum.color;
        _txtBombNum.color = new Color(t.r, t.g, t.b, 0);
        t = _txtBombTotal.color;
        _txtBombTotal.color = new Color(t.r, t.g, t.b, 0);
        _transBombItem.gameObject.SetActive(false);

        t = _imgManaLine.color;
        _imgManaLine.color = new Color(t.r, t.g, t.b, 0);
        t = _imgMana.color;
        _imgMana.color = new Color(t.r, t.g, t.b, 0);
        t = _txtManaLabel.color;
        _txtManaLabel.color = new Color(t.r, t.g, t.b, 0);
        t = _txtManaNum.color;
        _txtManaNum.color = new Color(t.r, t.g, t.b, 0);
        t = _txtManaTotal.color;
        _txtManaTotal.color = new Color(t.r, t.g, t.b, 0);

        t = _imgPointLine.color;
        _imgPointLine.color = new Color(t.r, t.g, t.b, 0);
        t = _imgPoint.color;
        _imgPoint.color = new Color(t.r, t.g, t.b, 0);
        t = _txtPointLabel.color;
        _txtPointLabel.color = new Color(t.r, t.g, t.b, 0);
        t = _txtPointNum.color;
        _txtPointNum.color = new Color(t.r, t.g, t.b, 0);

        t = _imgGrazeLine.color;
        _imgGrazeLine.color = new Color(t.r, t.g, t.b, 0);
        t = _txtGrazeLabel.color;
        _txtGrazeLabel.color = new Color(t.r, t.g, t.b, 0);
        t = _txtGrazeNum.color;
        _txtGrazeNum.color = new Color(t.r, t.g, t.b, 0);
    }
}
