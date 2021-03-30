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
            UIManager.Single.Show(Paths.PREFAB_PLAYER_VIEW);
            GameStateModel.Single.SelectedDegree = Degree.NORMAL;
        } },

        { 1, () => 
        {
            UIManager.Single.Show(Paths.PREFAB_PLAYER_VIEW);
            GameStateModel.Single.SelectedDegree = Degree.LUNATIC;
        } }
    };

    public override void InitChild()
    {
        
    }

    public override void Show()
    {
        InputMgr.Single.AddListener(KeyCode.UpArrow);
        InputMgr.Single.AddListener(KeyCode.DownArrow);
        InputMgr.Single.AddListener(KeyCode.X);
        InputMgr.Single.AddListener(KeyCode.Z);

        MessageMgr.Single.AddListener(KeyCode.UpArrow, DecIndex);
        MessageMgr.Single.AddListener(KeyCode.DownArrow, IncIndex);
        MessageMgr.Single.AddListener(KeyCode.X, Back);
        MessageMgr.Single.AddListener(KeyCode.Z, OnSelect);
    }

    public override void Hide()
    {
        InputMgr.Single.RemoveListener(KeyCode.UpArrow);
        InputMgr.Single.RemoveListener(KeyCode.DownArrow);
        InputMgr.Single.RemoveListener(KeyCode.X);
        InputMgr.Single.RemoveListener(KeyCode.Z);

        MessageMgr.Single.RemoveListener(KeyCode.UpArrow, DecIndex);
        MessageMgr.Single.RemoveListener(KeyCode.DownArrow, IncIndex);
        MessageMgr.Single.RemoveListener(KeyCode.X, Back);
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

        UIManager.Single.Show(Paths.PREFAB_START_VIEW);
    }

    private void OnSelect(object[] args)
    {
        AudioMgr.Single.PlayUIEff(Paths.AUDIO_SURE_EFF);
        int index = GameStateModel.Single.RankOption;

        _view._options[index].DOFade(0, 0.07f).SetLoops(6, LoopType.Yoyo)
            .OnComplete(() => _dicIndexAction[index]());
    }
}
