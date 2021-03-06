using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartController : ControllerBase
{
    public StartView _view;

    private Dictionary<int, Action> _dicIndexAction = new Dictionary<int, Action>()
    {
        { 0, () => 
        {
            RemoveListener();

            UIManager.Single.Hide(Paths.PREFAB_START_VIEW);
            UIManager.Single.Show(Paths.PREFAB_DEGREE_VIEW);
        } },

        { 1, () => Debug.LogWarning("功能正在开发中...") },
        { 2, () => Debug.LogWarning("功能正在开发中...") },
        { 3, () => Debug.LogWarning("功能正在开发中...") },

        { 4, () => 
        {
            RemoveListener();

            UIManager.Single.Hide(Paths.PREFAB_START_VIEW);
            UIManager.Single.Show(Paths.PREFAB_MANUAL_VIEW);
        } },

        { 5, () => Application.Quit() },
    };

    public override void InitAndChild()
    {
        
    }

    public override void Show()
    {
        base.Show();

        InputMgr.Single.AddListener(KeyCode.UpArrow);
        InputMgr.Single.AddListener(KeyCode.DownArrow);
        InputMgr.Single.AddListener(KeyCode.X);
        InputMgr.Single.AddListener(KeyCode.Escape);
        InputMgr.Single.AddListener(KeyCode.Z);

        MessageMgr.Single.AddListener(KeyCode.UpArrow, DecIndex);
        MessageMgr.Single.AddListener(KeyCode.DownArrow, IncIndex);
        MessageMgr.Single.AddListener(KeyCode.X, MoveFinalIndex);
        MessageMgr.Single.AddListener(KeyCode.Escape, MoveFinalIndex);
        MessageMgr.Single.AddListener(KeyCode.Z, OnSelect);

        _view.UpdateFun();
    }

    public override void Hide()
    {
        base.Hide();

        MessageMgr.Single.RemoveListener(KeyCode.UpArrow, DecIndex);
        MessageMgr.Single.RemoveListener(KeyCode.DownArrow, IncIndex);
        MessageMgr.Single.RemoveListener(KeyCode.X, MoveFinalIndex);
        MessageMgr.Single.RemoveListener(KeyCode.Escape, MoveFinalIndex);
        MessageMgr.Single.RemoveListener(KeyCode.Z, OnSelect);
    }

    private void IncIndex(object[] args)
    {
        AudioMgr.Single.PlayUIEff(Paths.AUDIO_SELECT_EFF);

        if (GameStateModel.Single.SelectedOption < _view.MAX_INDEX - 1)
        {
            ++GameStateModel.Single.SelectedOption;

            if(GameStateModel.Single.SelectedOption == 1)
            {
                GameStateModel.Single.SelectedOption = 4;
            }
            _view.UpdateFun();
        }
    }

    private void DecIndex(object[] args)
    {
        AudioMgr.Single.PlayUIEff(Paths.AUDIO_SELECT_EFF);

        if (GameStateModel.Single.SelectedOption > 0)
        {
            --GameStateModel.Single.SelectedOption;
            if (GameStateModel.Single.SelectedOption == 3)
            {
                GameStateModel.Single.SelectedOption = 0;
            }

            _view.UpdateFun();
        }
    }

    private void MoveFinalIndex(object[] args)
    {
        AudioMgr.Single.PlayUIEff(Paths.AUDIO_CANCAL_EFF);

        GameStateModel.Single.SelectedOption = _view.MAX_INDEX - 1;
        _view.UpdateFun();
    }

    private void OnSelect(object[] args)
    {
        AudioMgr.Single.PlayUIEff(Paths.AUDIO_SURE_EFF);

        int index = GameStateModel.Single.SelectedOption;

        _view._options[index].DOFade(0, 0.07f).SetLoops(6, LoopType.Yoyo)
            .OnComplete(() => _dicIndexAction[index]());
    }

    private static void RemoveListener()
    {
        InputMgr.Single.RemoveListener(KeyCode.UpArrow);
        InputMgr.Single.RemoveListener(KeyCode.DownArrow);
        InputMgr.Single.RemoveListener(KeyCode.X);
        InputMgr.Single.RemoveListener(KeyCode.Escape);
        InputMgr.Single.RemoveListener(KeyCode.Z);
    }
}
