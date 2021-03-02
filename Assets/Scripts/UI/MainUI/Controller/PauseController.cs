using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseController : ControllerBase
{
    public PauseView _view;

    private Dictionary<int, Action> _dicIndexAction;

    public override void InitChild()
    {
        _dicIndexAction = new Dictionary<int, Action>()
        {
            { 0, () =>
            {
                AudioMgr.Single.ContinueBGM();
                UIManager.Single.ShowController(Paths.PREFAB_GAME_VIEW);
            } },

            { 1, ()=>
            {
                GameStateModel.Single.TargetScene = SceneName.Main;
                GameStateModel.Single.Status = GameStatus.Pause;
                LifeCycleMgr.Single.Add(LifeName.UPDATE, this);     //加载场景时，重新注册update，这也是为什么字典在一个方法初始化的原因
                SceneMgr.Single.AsyncLoadScene(SceneName.Main);
            } },

            { 2, ()=> Debug.LogWarning("功能开发中") },
        };
    }

    public override void Show()
    {
        GameStateModel.Single.PauseOption = 0;

        InputMgr.Single.AddListener(KeyCode.UpArrow);
        InputMgr.Single.AddListener(KeyCode.DownArrow);
        InputMgr.Single.AddListener(KeyCode.Z);

        MessageMgr.Single.AddListener(KeyCode.UpArrow, DecIndex);
        MessageMgr.Single.AddListener(KeyCode.DownArrow, IncIndex);
        MessageMgr.Single.AddListener(KeyCode.Z, OnSelect);

        //LifeCycleMgr.Single.Add(LifeName.UPDATE, this);
    }

    public override void UpdateFun()
    {
        if(SceneMgr.Single.Process() == 1)
        {
            UIManager.Single.Hide(Paths.PREFAB_GAME_VIEW);
            UIManager.Single.Show(Paths.PREFAB_START_VIEW);
            AudioMgr.Single.PlayBGM(Paths.AUDIO_TITLE_BGM);

            SceneMgr.Single.ResetData();
        }
    }

    public override void Hide()
    {
        InputMgr.Single.RemoveListener(KeyCode.UpArrow);
        InputMgr.Single.RemoveListener(KeyCode.DownArrow);
        InputMgr.Single.RemoveListener(KeyCode.Z);

        MessageMgr.Single.RemoveListener(KeyCode.UpArrow, DecIndex);
        MessageMgr.Single.RemoveListener(KeyCode.DownArrow, IncIndex);
        MessageMgr.Single.RemoveListener(KeyCode.Z, OnSelect);

        LifeCycleMgr.Single.Remove(LifeName.UPDATE, this);
    }

    private void IncIndex(object[] args)
    {
        AudioMgr.Single.PlayEff(Paths.AUDIO_SELECT_EFF);

        if (GameStateModel.Single.PauseOption < _view.MAX_INDEX - 1)
        {
            ++GameStateModel.Single.PauseOption;
            _view.UpdateFun();
        }
    }

    private void DecIndex(object[] args)
    {
        AudioMgr.Single.PlayEff(Paths.AUDIO_SELECT_EFF);

        if (GameStateModel.Single.PauseOption > 0)
        {
            --GameStateModel.Single.PauseOption;
            _view.UpdateFun();
        }
    }

    private void OnSelect(object[] args)
    {
        Time.timeScale = 1;
        AudioMgr.Single.PlayEff(Paths.AUDIO_SURE_EFF);
        UIManager.Single.Hide(Paths.PREFAB_PAUSE_VIEW);

        int index = GameStateModel.Single.PauseOption;
        GameStateModel.Single.Status = GameStatus.Gameing;
        _dicIndexAction[index]();
    }
}
