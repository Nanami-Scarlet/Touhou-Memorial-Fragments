using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicController : ControllerBase
{
    public DynamicView _view;

    private bool _isComplete = true;
    private float _totTime;
    private int _sec;
    private int _tempSec;
    private int _mili;
    private bool _isRed = false;

    public override void InitAndChild()
    {
        
    }

    public override void Show()
    {
        base.Show();

        MessageMgr.Single.AddListener(MsgEvent.EVENT_BGM_SETTING, BGMSetting);
        MessageMgr.Single.AddListener(MsgEvent.EVENT_ADD_HPBAR, AddHPBar);
        MessageMgr.Single.AddListener(MsgEvent.EVENT_SET_HPBAR, SetHPBar);
        MessageMgr.Single.AddListener(MsgEvent.EVENT_SET_TIMER, SetTimer);
        MessageMgr.Single.AddListener(MsgEvent.EVENT_HIDE_TIMER, HideTimer);
        MessageMgr.Single.AddListener(MsgEvent.EVENT_SET_HPBAR_VIEW, SetHPView);
        MessageMgr.Single.AddListener(MsgEvent.EVENT_SHOW_BOSS_NAME_CARD, ShowBossNameCard);
        MessageMgr.Single.AddListener(MsgEvent.EVENT_CLEAR_ALL_HPBAR, ClearAllHPBar);
        MessageMgr.Single.AddListener(MsgEvent.EVENT_MOVE_TIMER, MoveTimer);
        MessageMgr.Single.AddListener(MsgEvent.EVENT_PLAY_CARD_INFO_ANIM, PlayCardInfoAnim);
        MessageMgr.Single.AddListener(MsgEvent.EVENT_SET_CARD_PIC, SetCardPic);
        MessageMgr.Single.AddListener(MsgEvent.EVENT_PLAY_CARD_PIC_ANIM, PlayCardPicAnim);
        MessageMgr.Single.AddListener(MsgEvent.EVENT_MOVE_CARD_INFO_RIGHT, MoveCardInfoRight);
        MessageMgr.Single.AddListener(MsgEvent.EVENT_HIDE_HP_VIEW, HideHPView);
        MessageMgr.Single.AddListener(MsgEvent.EVENT_HIDE_BOSS_NAME_CARD, HideBossNameCard);
        MessageMgr.Single.AddListener(MsgEvent.EVENT_COMPLETE_TIMER, CompleteTimer);
    }

    private void Update()
    {
        if (!_isComplete)
        {
            _totTime -= Time.deltaTime;

            if (_totTime <= 0)      //如果时间超时
            {
                _isComplete = true;
                _view.SetTimerActive(false);
                MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_SET_TIMEUP, true);
            }
            else
            {
                _sec = (int)_totTime;
                _mili = (int)(_totTime * 100 % 100);

                if(_sec <= 7 && _sec != _tempSec)
                {
                    _tempSec = _sec;
                    AudioMgr.Single.PlayGameEff(AudioType.TimeOut);
                    _isRed = true;
                }

                _view.SetTimeText(_sec, _mili, _isRed);
            }
        }
    }

    public override void Hide()
    {
        base.Hide();

        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_BGM_SETTING, BGMSetting);
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_ADD_HPBAR, AddHPBar);
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_SET_HPBAR, SetHPBar);
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_SET_TIMER, SetTimer);
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_HIDE_TIMER, HideTimer);
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_SET_HPBAR_VIEW, SetHPView);
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_SHOW_BOSS_NAME_CARD, ShowBossNameCard);
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_CLEAR_ALL_HPBAR, ClearAllHPBar);
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_MOVE_TIMER, MoveTimer);
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_PLAY_CARD_INFO_ANIM, PlayCardInfoAnim);
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_SET_CARD_PIC, SetCardPic);
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_PLAY_CARD_PIC_ANIM, PlayCardPicAnim);
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_MOVE_CARD_INFO_RIGHT, MoveCardInfoRight);
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_HIDE_HP_VIEW, HideHPView);
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_HIDE_BOSS_NAME_CARD, HideBossNameCard);
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_COMPLETE_TIMER, CompleteTimer);

        _isComplete = true;
        _view.HideBossNameCard();
    }

    private void BGMSetting(object[] args)
    {
        string stage = (string)args[0];

        _view.BGMSetting(stage);
    }

    private void AddHPBar(object[] args)
    {
        GameObject go = (GameObject)args[0];

        _view.AddHPBar(go.name);
    }

    private void SetHPBar(object[] args)
    {
        BaseCardHPData data = (BaseCardHPData)args[0];
        bool isFinalCard = data.NormalHP == 0;

        _view.SetHPBar(data, isFinalCard);
    }

    private void ClearAllHPBar(object[] args)
    {
        _view.ClearAllHPBar();
    }

    private void SetHPView(object[] args)
    {
        HPData hpData = (HPData)args[0];

        _view.SetHPView(hpData);
    }

    private void HideHPView(object[] args)
    {
        GameObject go = (GameObject)args[0];

        _view.HideHPView(go);
    }

    private void ShowBossNameCard(object[] args)
    {
        BossNameCard data = (BossNameCard)args[0];

        _view.ShowBossNameCard(data);
    }

    private void HideBossNameCard(object[] args)
    {
        _view.HideBossNameCard();
    }

    private void SetTimer(object[] args)
    {
        float sec = (float)args[0];

        _totTime = sec;
        _isComplete = false;
        _isRed = false;
        _view.SetTimerActive(true);
    }

    private void HideTimer(object[] args)
    {
        _view.SetTimerActive(false);
    }

    private void MoveTimer(object[] args)
    {
        int poxY = (int)args[0];

        _view.MoveTimer(poxY);
    }

    private void PlayCardInfoAnim(object[] args)
    {
        string cardName = (string)args[0];

        _view.PlayCardInfoAnim(cardName);
    }

    private void SetCardPic(object[] args)
    {
        string bossType = (string)args[0];

        _view.SetCardPic(bossType);
    }

    private void PlayCardPicAnim(object[] args)
    {
        _view.PlayCardPicAnim();
    }

    private void MoveCardInfoRight(object[] args)
    {
        _view.MoveCardInfoRight();
    }

    private void CompleteTimer(object[] args)
    {
        _isComplete = true;
    }
}
