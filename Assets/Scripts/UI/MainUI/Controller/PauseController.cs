using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseController : ControllerBase
{
    public PauseView _view;

    private Dictionary<int, Action> _dicIndexAction;

    public override void InitAndChild()
    {
        _dicIndexAction = new Dictionary<int, Action>()
        {
            { 0, () =>
            {
                Time.timeScale = 1;
                GameStateModel.Single.IsPause = false;
                UIManager.Single.Hide(Paths.PREFAB_PAUSE_VIEW);
                AudioMgr.Single.ContinueBGM();
                UIManager.Single.ShowController(Paths.PREFAB_GAME_VIEW);
            } },

            { 1, ()=>
            {
                RemoveKeyCode();
                Time.timeScale = 1;
                GameStateModel.Single.IsPause = false;
                GameStateModel.Single.TargetScene = SceneName.Main;
                GameStateModel.Single.IsChating = false;                            //这几行修改GameScene配置以至于在MainScene正常运行
                MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_CLEAR_ALL_HPBAR);
                LifeCycleMgr.Single.Add(LifeName.UPDATE, this);     //加载场景时，重新注册update，这也是为什么字典在一个方法初始化的原因
                UIManager.Single.Hide(Paths.PREFAB_PAUSE_VIEW);
                SceneMgr.Single.AsyncLoadScene(SceneName.Main);
            } },

            { 2, ()=> 
            {
                UIManager.Single.Hide(Paths.PREFAB_PAUSE_VIEW);
                UIManager.Single.Show(Paths.PREFAB_MANUAL_VIEW);
            } },

            { 3, ()=> Debug.LogWarning("功能开发中") },
        };
    }

    public override void Show()
    {
        UIManager.Single.SimpleHide(Paths.PREFAB_CHAT_VIEW);      //如果Chat显示，则暂时隐藏

        GameStateModel.Single.PauseOption = 0;

        InputMgr.Single.AddListener(KeyCode.UpArrow);
        InputMgr.Single.AddListener(KeyCode.DownArrow);
        InputMgr.Single.AddListener(KeyCode.Escape);
        InputMgr.Single.AddListener(KeyCode.Z);

        MessageMgr.Single.AddListener(KeyCode.UpArrow, DecIndex);
        MessageMgr.Single.AddListener(KeyCode.DownArrow, IncIndex);
        MessageMgr.Single.AddListener(KeyCode.Escape, BackToGame);
        MessageMgr.Single.AddListener(KeyCode.Z, OnSelect);
    }

    public override void UpdateFun()
    {
        if(SceneMgr.Single.IsDone())
        {
            LifeCycleMgr.Single.Remove(LifeName.UPDATE, this);

            //TimeMgr.Single.ClearAllTask();

            UIManager.Single.Hide(Paths.PREFAB_GAME_VIEW);
            UIManager.Single.Hide(Paths.PREFAB_DYNAMIC_VIEW);

            DOTween.CompleteAll();

            UIManager.Single.Show(Paths.PREFAB_START_VIEW);
            AudioMgr.Single.PlayBGM(Paths.AUDIO_TITLE_BGM);

            GameStateModel.Single.IsPause = true;
        }
    }

    public override void Hide()
    {
        RemoveKeyCode();

        MessageMgr.Single.RemoveListener(KeyCode.UpArrow, DecIndex);
        MessageMgr.Single.RemoveListener(KeyCode.DownArrow, IncIndex);
        MessageMgr.Single.RemoveListener(KeyCode.Escape, BackToGame);
        MessageMgr.Single.RemoveListener(KeyCode.Z, OnSelect);

        //LifeCycleMgr.Single.Remove(LifeName.UPDATE, this);

        if (GameStateModel.Single.IsChating)
        {
            UIManager.Single.SimpleShow(Paths.PREFAB_CHAT_VIEW);
        }
    }

    private void IncIndex(object[] args)
    {
        AudioMgr.Single.PlayUIEff(Paths.AUDIO_SELECT_EFF);

        if (GameStateModel.Single.PauseOption < _view.MAX_INDEX)
        {
            ++GameStateModel.Single.PauseOption;
            _view.UpdateFun();
        }
    }

    private void DecIndex(object[] args)
    {
        AudioMgr.Single.PlayUIEff(Paths.AUDIO_SELECT_EFF);

        if (GameStateModel.Single.PauseOption > 0)
        {
            --GameStateModel.Single.PauseOption;
            _view.UpdateFun();
        }
    }

    private void OnSelect(object[] args)
    {
        //Time.timeScale = 1;
        AudioMgr.Single.PlayUIEff(Paths.AUDIO_SURE_EFF);

        int index = GameStateModel.Single.PauseOption;
        //GameStateModel.Single.IsPause = false;
        _dicIndexAction[index]();
    }

    private void BackToGame(object[] args)
    {
        _dicIndexAction[0]();
    }

    private void RemoveKeyCode()
    {
        InputMgr.Single.RemoveListener(KeyCode.UpArrow);
        InputMgr.Single.RemoveListener(KeyCode.DownArrow);
        InputMgr.Single.RemoveListener(KeyCode.Escape);
        InputMgr.Single.RemoveListener(KeyCode.Z);
    }
}
