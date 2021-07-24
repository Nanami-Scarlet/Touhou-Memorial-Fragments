using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThanksController : ControllerBase
{
    public ThanksView _view;


    public override void InitAndChild()
    {
        
    }

    public override void Show()
    {
        base.Show();

        //TimeMgr.Single.AddTimeTask(() => 
        //{
            InputMgr.Single.AddListener(KeyCode.X);

            MessageMgr.Single.AddListener(KeyCode.X, BackToStart);
        //}, 27, TimeUnit.Second);
    }

    public override void UpdateFun()
    {
        base.UpdateFun();

        if(SceneMgr.Single.IsDone())
        {
            LifeCycleMgr.Single.Remove(LifeName.UPDATE, this);
            UIManager.Single.Hide(Paths.PREFAB_THANKS_VIEW);
            UIManager.Single.Hide(Paths.PREFAB_CHAT_VIEW);

            UIManager.Single.Show(Paths.PREFAB_START_VIEW);
            AudioMgr.Single.PlayBGM(Paths.AUDIO_TITLE_BGM);

            GameStateModel.Single.IsPause = true;
        }
    }


    public override void Hide()
    {
        base.Hide();

        RemoveKeyCode();

        MessageMgr.Single.RemoveListener(KeyCode.X, BackToStart);
    }

    private void BackToStart(object[] args)
    {
        RemoveKeyCode();
        GameStateModel.Single.TargetScene = SceneName.Main;
        GameStateModel.Single.IsChating = false;
        GameStateModel.Single.IsPause = true;       //防止注册的GameListener起作用
        UIManager.Single.Hide(Paths.PREFAB_GAME_VIEW);

        LifeCycleMgr.Single.Add(LifeName.UPDATE, this);
        SceneMgr.Single.AsyncLoadScene(SceneName.Main);
    }

    private void RemoveKeyCode()
    {
        InputMgr.Single.RemoveListener(KeyCode.X);
    }
}
