using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DegreeController : ControllerBase
{
    public DegreeView _view;

    private Dictionary<int, Action> _dicIndexAction = new Dictionary<int, Action>()
    {
        { 0, () => 
        {
            GameStateModel.Single.GameDegree = Degree.NORMAL;
        } },

        { 1, () => 
        {
            GameStateModel.Single.GameDegree = Degree.LUNATIC;
        } }
    };

    public override void InitAndChild()
    {
        
    }

    public override void Show()
    {
        InputMgr.Single.AddListener(KeyCode.UpArrow);
        InputMgr.Single.AddListener(KeyCode.DownArrow);
        InputMgr.Single.AddListener(KeyCode.X);
        InputMgr.Single.AddListener(KeyCode.Escape);
        InputMgr.Single.AddListener(KeyCode.Z);

        TimeMgr.Single.AddTimeTask(() =>
        {
            MessageMgr.Single.AddListener(KeyCode.UpArrow, DecIndex);
            MessageMgr.Single.AddListener(KeyCode.DownArrow, IncIndex);
            MessageMgr.Single.AddListener(KeyCode.X, Back);
            MessageMgr.Single.AddListener(KeyCode.Escape, Back);
            MessageMgr.Single.AddListener(KeyCode.Z, OnSelect);
        }, 0.5f, TimeUnit.Second);
        
    }

    public override void Hide()
    {
        MessageMgr.Single.RemoveListener(KeyCode.UpArrow, DecIndex);
        MessageMgr.Single.RemoveListener(KeyCode.DownArrow, IncIndex);
        MessageMgr.Single.RemoveListener(KeyCode.X, Back);
        MessageMgr.Single.RemoveListener(KeyCode.Escape, Back);
        MessageMgr.Single.RemoveListener(KeyCode.Z, OnSelect);
    }

    private void IncIndex(object[] args)
    {
        AudioMgr.Single.PlayUIEff(Paths.AUDIO_SELECT_EFF);

        if (GameStateModel.Single.RankOption < _view.MAX_INDEX - 1)
        {
            ++GameStateModel.Single.RankOption;
            _view.UpdateFun();
        }
    }

    private void DecIndex(object[] args)
    {
        AudioMgr.Single.PlayUIEff(Paths.AUDIO_SELECT_EFF);

        if (GameStateModel.Single.RankOption > 0)
        {
            --GameStateModel.Single.RankOption;
            _view.UpdateFun();
        }
    }

    private void Back(object[] args)
    {
        AudioMgr.Single.PlayUIEff(Paths.AUDIO_CANCAL_EFF);

        UIManager.Single.Hide(Paths.PREFAB_DEGREE_VIEW);
        UIManager.Single.Show(Paths.PREFAB_START_VIEW);
    }

    private void OnSelect(object[] args)
    {
        AudioMgr.Single.PlayUIEff(Paths.AUDIO_SURE_EFF);
        int index = GameStateModel.Single.RankOption;

        InputMgr.Single.RemoveListener(KeyCode.UpArrow);
        InputMgr.Single.RemoveListener(KeyCode.DownArrow);
        InputMgr.Single.RemoveListener(KeyCode.X);
        InputMgr.Single.RemoveListener(KeyCode.Escape);
        InputMgr.Single.RemoveListener(KeyCode.Z);

        UIManager.Single.Hide(Paths.PREFAB_DEGREE_VIEW);
        UIManager.Single.Show(Paths.PREFAB_PLAYER_VIEW);

        _view._txtOptions[index].DOFade(0, 0.07f).SetLoops(6, LoopType.Yoyo)
            .OnComplete(() => _dicIndexAction[index]());
    }
}
