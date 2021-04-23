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
    private Text _txtHighScore;
    private Image _imgHighLine;
    #endregion

    #region CurrentScore
    public Transform _transCurScore;
    private Text _txtCurLabel;
    private Text _txtCurScore;
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
    private Transform _transLifeDisable;
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

    #region BorderLine
    public Transform _transBorderLine;
    private Text _txtBorderTitle;
    private Image _imgBorderLine;
    #endregion

    #region Memory
    public Transform _transMemory;
    public Transform[] _memoryYins;
    private Image _imgBarBg;
    private Image _imgBarFg;
    private Image _imgBarPoint;
    private Text _txtBarProcess;
    private float _imgBarFgWidth;
    #endregion

    public Image _imgSacrifice;

    private GameObject _objStageLabel;


    public override void InitChild()
    {
        _txtHighLabel = _transHighScore.GetChild(1).GetComponent<Text>();
        _txtHighScore = _transHighScore.GetChild(2).GetComponent<Text>();
        _imgHighLine = _transHighScore.GetChild(0).GetComponent<Image>();

        _txtCurLabel = _transCurScore.GetChild(1).GetComponent<Text>();
        _txtCurScore = _transCurScore.GetChild(2).GetComponent<Text>();
        _imgCurLine = _transCurScore.GetChild(0).GetComponent<Image>();

        _txtLifeLabel = _transLife.GetChild(1).GetComponent<Text>();
        _transLifeItem = _transLife.GetChild(2);
        _imgLifeLine = _transLife.GetChild(0).GetComponent<Image>();
        _txtLifeFragment = _transLife.GetChild(3).GetComponent<Text>();
        _txtLifeNum = _transLife.GetChild(4).GetComponent<Text>();
        _txtLifeTotal = _transLife.GetChild(5).GetComponent<Text>();
        _transLifeDisable = _transLife.GetChild(6);

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

        _imgBarBg = _transMemory.GetChild(1).GetComponent<Image>();
        _imgBarFg = _transMemory.GetChild(2).GetComponent<Image>();
        _imgBarPoint = _transMemory.GetChild(2).GetChild(0).GetComponent<Image>();
        _txtBarProcess = _transMemory.GetChild(3).GetComponent<Text>();
        _imgBarFgWidth = _imgBarFg.GetComponent<RectTransform>().sizeDelta.x;

        _imgBorderLine = _transBorderLine.GetChild(0).GetComponent<Image>();
        _txtBorderTitle = _transBorderLine.GetChild(1).GetComponent<Text>();
    }

    public override void Show()
    {
        _objStageLabel = LoadMgr.Single.LoadPrefabAndInstantiate(Paths.PREFAB_STAGE_LABEL, transform);
        ResetData();
        ResetAnim();
        PlayAnim();

        MessageMgr.Single.AddListener(MsgEvent.EVENT_STAGE_ANIM, StageAnim);
        MessageMgr.Single.AddListener(MsgEvent.EVENT_UPDATE_SCORE, UpdateScore);
        MessageMgr.Single.AddListener(MsgEvent.EVENT_UPDATE_MANA, UpdateMana);
        MessageMgr.Single.AddListener(MsgEvent.EVENT_UPDATE_LIFT, UpdateLife);
        MessageMgr.Single.AddListener(MsgEvent.EVENT_UPDATE_BOMB, UpdateBomb);
        MessageMgr.Single.AddListener(MsgEvent.EVENT_UPDATE_GRAZE, UpdateGraze);
        MessageMgr.Single.AddListener(MsgEvent.EVENT_UPDATE_POINT, UpdatePoint);
        MessageMgr.Single.AddListener(MsgEvent.EVENT_UPDATE_MEMORY, UpdateMemory);
    }

    public override void Hide()
    {
        base.Hide();

        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_STAGE_ANIM, StageAnim);
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_UPDATE_SCORE, UpdateScore);
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_UPDATE_MANA, UpdateMana);
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_UPDATE_LIFT, UpdateLife);
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_UPDATE_BOMB, UpdateBomb);
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_UPDATE_GRAZE, UpdateGraze);
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_UPDATE_POINT, UpdatePoint);
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_UPDATE_MEMORY, UpdateMemory);


        DOTween.KillAll();

        Destroy(_objStageLabel);
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
        high.Join(_txtHighScore.DOFade(1, _animSpeed));
        high.Join(_imgHighLine.DOFade(0.5f, _animSpeed));
        high.Pause();
        #endregion

        #region CurrentScore动画
        Sequence cur = DOTween.Sequence();
        cur.Join(_txtCurLabel.DOFade(1, _animSpeed));
        cur.Join(_txtCurScore.DOFade(1, _animSpeed));
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

        #region Memory动画
        Sequence memory = DOTween.Sequence();
        memory.Join(_imgBarBg.DOFade(1, 0.5f));
        memory.Join(_imgBarFg.DOFade(1, 0.5f));
        memory.Join(_imgBarPoint.DOFade(1, 0.5f));
        memory.Join(_txtBarProcess.DOFade(1, 0.5f));
        memory.Pause();
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
        graze.onComplete += () => memory.Play();
        if(GameStateModel.Single.SelectedDegree == Degree.NORMAL)
        {
            memory.onComplete += () => _transLifeDisable.gameObject.SetActive(true);
        }
        #endregion

        _imgSacrifice.transform.DOLocalRotate(Vector3.zero, 0.5f);
        _imgSacrifice.DOFade(1, 0.5f);

        _transBorderLine.DOLocalRotate(Vector3.zero, 0.5f);
        _txtBorderTitle.DOColor(new Color(0, 0.9f, 1), 0.2f).SetLoops(15, LoopType.Restart);
        TimeMgr.Single.AddTimeTask(() => 
        {
            _transBorderLine.gameObject.SetActive(false);
        }, 2.1f, TimeUnit.Second);
    }

    private void ResetAnim()
    {
        int index = (int)GameStateModel.Single.SelectedDegree;
        _transRank.GetChild(index).GetComponent<Text>().color = new Color(1, 1, 1, 0);
        _transRank.localPosition = new Vector3(-60, 416, 0);

        Color t = _txtHighLabel.color;
        _txtHighLabel.color = new Color(t.r, t.g, t.b, 0);
        t = _txtHighScore.color;
        _txtHighScore.color = new Color(t.r, t.g, t.b, 0);
        t = _imgHighLine.color;
        _imgHighLine.color = new Color(t.r, t.g, t.b, 0);

        t = _txtCurLabel.color;
        _txtCurLabel.color = new Color(t.r, t.g, t.b, 0);
        t = _txtCurScore.color;
        _txtCurScore.color = new Color(t.r, t.g, t.b, 0);
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

        for(int i = 0; i < _memoryYins.Length; ++i)
        {
            _memoryYins[i].GetComponent<Image>().enabled = false;
        }
        t = _imgBarBg.color;
        _imgBarBg.color = new Color(t.r, t.g, t.b, 0);
        t = _imgBarFg.color;
        _imgBarFg.color = new Color(t.r, t.g, t.b, 0);
        t = _imgBarPoint.color;
        _imgBarPoint.color = new Color(t.r, t.g, t.b, 0);
        t = _txtBarProcess.color;
        _txtBarProcess.color = new Color(t.r, t.g, t.b, 0);

        _transBorderLine.gameObject.SetActive(true);
        _transBorderLine.rotation = new Quaternion(0.7071068f, 0, 0, 0.7071068f);

        _transLifeDisable.gameObject.SetActive(false);
    }

    private void ResetData()
    {
        _txtManaNum.text = "1.00";
        _txtGrazeNum.text = "0";

        _txtCurScore.text = "0";
        _txtPointNum.text = PlayerModel.Single.Init_Point.ToString();
        _txtBarProcess.text = "0%";
        _imgBarFg.fillAmount = 0;
        _imgBarPoint.transform.localPosition = new Vector3(-170, 0, 0);

        ResetItem(_transLifeItems, PlayerModel.Single.Life);
        ResetItem(_transBombItems, PlayerModel.Single.Bomb);
    }

    private void ResetItem(Transform[] transItems, int num)
    {
        for(int i = 0; i < 8; ++i)
        {
            if (i < num)
            {
                transItems[i].GetChild(1).GetComponent<Image>().fillAmount = 1;
            }
            else
            {
                transItems[i].GetChild(1).GetComponent<Image>().fillAmount = 0;
            }
        }
    }

    private void StageAnim(object[] args)
    {
        _objStageLabel.SetActive(true);

        Transform transStage = GameUtil.SetSubActive(_objStageLabel.transform, GameModel.Single.StageNum);
        Image bg = transStage.GetChild(0).GetComponent<Image>();
        Text label = transStage.GetChild(1).GetComponent<Text>();
        Text title = transStage.GetChild(2).GetComponent<Text>();

        Sequence sequence = DOTween.Sequence();
        sequence.Join(bg.transform.DOLocalMoveX(120, 3));
        sequence.Join(bg.DOFade(0.5f, 3));
        sequence.Join(bg.transform.DOLocalRotate(Vector3.back * 1080, 3, RotateMode.FastBeyond360).SetEase(Ease.OutCirc));
        sequence.Insert(1f, label.DOFade(1, 2));
        sequence.Insert(2f, title.DOFade(1, 1));

        sequence.OnComplete(() => 
        {
            bg.DOFade(0, 1);
            label.DOFade(0, 1);
            title.DOFade(0, 1);

            TimeMgr.Single.AddTimeTask(() =>
            {
                _objStageLabel.SetActive(false);
            }, 1.1f, TimeUnit.Second);
        });
    }

    private void UpdateScore(object[] args)          //这里的参数应该是int
    {
        int score = (int)args[0];

        _txtCurScore.text = string.Format("{0:N0}", score);
        if(score > GameModel.Single.HighScore)
        {
            GameModel.Single.HighScore = score;
            _txtHighScore.text = string.Format("{0:N0}", score);
        }
    }

    private void UpdateMana(object[] args)
    {
        int mana = (int)args[0];

        int level = mana / 100;
        int process = mana % 100;

        _txtManaNum.text = level + "." + string.Format("{0:D2}", process);
    }

    private void UpdateLife(object[] args)
    {
        if(GameStateModel.Single.SelectedDegree == Degree.LUNATIC)
        {
            int life = PlayerModel.Single.Life;

            ResetItem(_transLifeItems, life);
            _transLifeItems[life].GetChild(1).GetComponent<Image>().fillAmount
                = (float)PlayerModel.Single.LifeFragment / Const.FULL_FRAGMENT;

            if (PlayerModel.Single.LifeFragment == Const.FULL_FRAGMENT)
            {
                PlayerModel.Single.LifeFragment = 0;
                ++PlayerModel.Single.Life;

                AudioMgr.Single.PlayGameEff(AudioType.Extend);
            }
            _txtLifeNum.text = PlayerModel.Single.LifeFragment.ToString();
        }
    }

    private void UpdateBomb(object[] args)
    {
        int bomb = PlayerModel.Single.Bomb;

        ResetItem(_transBombItems, bomb);
        _transBombItems[bomb].GetChild(1).GetComponent<Image>().fillAmount
            = (float)PlayerModel.Single.BombFragment / Const.FULL_FRAGMENT;

        if (PlayerModel.Single.BombFragment == Const.FULL_FRAGMENT)
        {
            PlayerModel.Single.BombFragment = 0;
            ++PlayerModel.Single.Bomb;

            AudioMgr.Single.PlayGameEff(AudioType.GetBomb);
        }
        _txtBombNum.text = PlayerModel.Single.BombFragment.ToString();
    }

    private void UpdateGraze(object[] args)
    {
        int graze = (int)args[0];

        _txtGrazeNum.text = graze.ToString();
    }

    private void UpdatePoint(object[] args)
    {
        //todo:最大得点要更新，不仅仅这么简单
        int num = PlayerModel.Single.Init_Point + (PlayerModel.Single.Graze / 10) * 10;

        _txtPointNum.text = num.ToString();
    }

    private void UpdateMemory(object[] args)
    {
        for(int i = 0; i < PlayerModel.Single.MemoryFragment; ++i)
        {
            _memoryYins[i].GetComponent<Image>().enabled = true;
        }

        int process = PlayerModel.Single.MemoryProcess;
        _txtBarProcess.text = process + "%";
        _imgBarFg.fillAmount = (float)process / 100;

        float posX = (float)process / 100 * _imgBarFgWidth - 170;
        _imgBarPoint.transform.localPosition = new Vector3(posX, 0, 0);
    }
}
