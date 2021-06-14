using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerUController : ControllerBase
{
    public PlayerUView _view;

    private Dictionary<int, Action> _dicIndexAction = null;

    public override void InitAndChild()
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
                AudioMgr.Single.StopBGM();
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

        //等待动画加载完成才可以操作
        TimeMgr.Single.AddTimeTask(() => 
        {
            MessageMgr.Single.AddListener(KeyCode.RightArrow, IncIndex);
            MessageMgr.Single.AddListener(KeyCode.LeftArrow, DecIndex);
            MessageMgr.Single.AddListener(KeyCode.Z, OnSelect);
            MessageMgr.Single.AddListener(KeyCode.X, Back);
        }, 0.9f, TimeUnit.Second);
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
        GameStateModel.Single.PlayerOption = (GameStateModel.Single.PlayerOption + 1) % 2;
        _view.UpdateFun();
    }

    private void DecIndex(object[] args)
    {
        AudioMgr.Single.PlayUIEff(Paths.AUDIO_SELECT_EFF);
        --GameStateModel.Single.PlayerOption;
        if(GameStateModel.Single.PlayerOption == -1)
        {
            GameStateModel.Single.PlayerOption = 1;
        }
        _view.UpdateFun();
    }

    private void Back(object[] args)
    {
        AudioMgr.Single.PlayUIEff(Paths.AUDIO_CANCAL_EFF);

        UIManager.Single.Hide(Paths.PREFAB_PLAYER_VIEW);
        UIManager.Single.Show(Paths.PREFAB_DEGREE_VIEW);
    }

    private void OnSelect(object[] args)
    {
        AudioMgr.Single.PlayUIEff(Paths.AUDIO_SURE_EFF);

        int index = GameStateModel.Single.PlayerOption;

        //RemoveKeyCode();

        if (_dicIndexAction[index] != null)
        {
            _dicIndexAction[index]();
        }
    }

    private void RemoveKeyCode()
    {
        InputMgr.Single.RemoveListener(KeyCode.RightArrow);
        InputMgr.Single.RemoveListener(KeyCode.LeftArrow);
        InputMgr.Single.RemoveListener(KeyCode.Z);
        InputMgr.Single.RemoveListener(KeyCode.X);
    }
}
