using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerUComtroller : ControllerBase
{
    public PlayerUView _view;

    private Dictionary<int, Action> _dicIndexAction = null;

    public override void InitChild()
    {
        _dicIndexAction = new Dictionary<int, Action>()
        {
            { 0, () =>
            {
                _view._transLoad.gameObject.SetActive(true);
                LifeCycleMgr.Single.Add(LifeName.UPDATE, this);
                GameStateModel.Single.TargetScene = SceneName.Game;
                RemoveKeyCode();
                SceneMgr.Single.AsyncLoadScene(SceneName.Game);
                GameStateModel.Single.Status = GameStatus.Gameing;
            } },

            { 1, null }
        };
    }

    public override void Show()
    {
        GameStateModel.Single.PlayerOption = 0;

        InputMgr.Single.AddListener(KeyCode.RightArrow);
        InputMgr.Single.AddListener(KeyCode.LeftArrow);
        InputMgr.Single.AddListener(KeyCode.Z);
        InputMgr.Single.AddListener(KeyCode.X);

        MessageMgr.Single.AddListener(KeyCode.RightArrow, IncIndex);
        MessageMgr.Single.AddListener(KeyCode.LeftArrow, DecIndex);
        MessageMgr.Single.AddListener(KeyCode.Z, OnSelect);
        MessageMgr.Single.AddListener(KeyCode.X, Back);

        //LifeCycleMgr.Single.Add(LifeName.UPDATE, this);
    }

    public override void UpdateFun()
    {
        if (SceneMgr.Single.IsDone())
        {

        }
    }

    public override void Hide()
    {
        RemoveKeyCode();

        MessageMgr.Single.RemoveListener(KeyCode.RightArrow, IncIndex);
        MessageMgr.Single.RemoveListener(KeyCode.LeftArrow, DecIndex);
        MessageMgr.Single.RemoveListener(KeyCode.Z, OnSelect);
        MessageMgr.Single.RemoveListener(KeyCode.X, Back);

        LifeCycleMgr.Single.Remove(LifeName.UPDATE, this);
    }

    private void IncIndex(object[] args)
    {
        AudioMgr.Single.PlayUIEff(Paths.AUDIO_SELECT_EFF);
        ++GameStateModel.Single.PlayerOption;
        _view.UpdateFun();
    }

    private void DecIndex(object[] args)
    {
        AudioMgr.Single.PlayUIEff(Paths.AUDIO_SELECT_EFF);
        --GameStateModel.Single.PlayerOption;
        _view.UpdateFun();
    }

    private void Back(object[] args)
    {
        AudioMgr.Single.PlayUIEff(Paths.AUDIO_CANCAL_EFF);

        UIManager.Single.Show(Paths.PREFAB_DEGREE_VIEW);
    }

    private void OnSelect(object[] args)
    {
        AudioMgr.Single.PlayUIEff(Paths.AUDIO_SURE_EFF);

        int index = GameStateModel.Single.PlayerOption;
        if (_dicIndexAction[index] != null)
        {
            _dicIndexAction[index]();
        }
        AudioMgr.Single.StopBGM();
    }

    private void RemoveKeyCode()
    {
        InputMgr.Single.RemoveListener(KeyCode.RightArrow);
        InputMgr.Single.RemoveListener(KeyCode.LeftArrow);
        InputMgr.Single.RemoveListener(KeyCode.Z);
        InputMgr.Single.RemoveListener(KeyCode.X);
    }
}
