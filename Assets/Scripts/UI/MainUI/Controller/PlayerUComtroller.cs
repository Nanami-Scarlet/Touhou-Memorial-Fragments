using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerUComtroller : ControllerBase
{
    public PlayerView _view;

    public override void InitChild()
    {
        GameStateModel.Single.PlayerOption = 0;

        InputMgr.Single.AddListener(KeyCode.RightArrow);
        InputMgr.Single.AddListener(KeyCode.LeftArrow);
        InputMgr.Single.AddListener(KeyCode.Z);
        InputMgr.Single.AddListener(KeyCode.X);

        MessageMgr.Single.AddListener(KeyCode.RightArrow, IncIndex);
        MessageMgr.Single.AddListener(KeyCode.LeftArrow, DecIndex);
        MessageMgr.Single.AddListener(KeyCode.Z, LoadScene);
        MessageMgr.Single.AddListener(KeyCode.X, Back);

        LifeCycleMgr.Single.Add(LifeName.UPDATE, this);
    }

    public override void UpdateFun()
    {
        if(SceneMgr.Single.Process() == 1)
        {
            //todo:加载主游戏UI
            UIManager.Single.Hide(Paths.PREFAB_PLAYER_VIEW);
        }
    }

    public override void Hide()
    {
        InputMgr.Single.RemoveListener(KeyCode.RightArrow);
        InputMgr.Single.RemoveListener(KeyCode.LeftArrow);
        InputMgr.Single.RemoveListener(KeyCode.Z);
        InputMgr.Single.RemoveListener(KeyCode.X);

        MessageMgr.Single.RemoveListener(KeyCode.RightArrow, IncIndex);
        MessageMgr.Single.RemoveListener(KeyCode.LeftArrow, DecIndex);
        MessageMgr.Single.RemoveListener(KeyCode.Z, LoadScene);
        MessageMgr.Single.RemoveListener(KeyCode.X, Back);

        LifeCycleMgr.Single.Remove(LifeName.UPDATE, this);
    }

    private void IncIndex(object[] args)
    {
        //todo:这里应该有个音效
        ++GameStateModel.Single.PlayerOption;
        _view.UpdateFun();
    }

    private void DecIndex(object[] args)
    {
        //todo:这里应该有个音效
        --GameStateModel.Single.PlayerOption;
        _view.UpdateFun();
    }

    private void Back(object[] args)
    {
        //todo:这里应该有个音效

        UIManager.Single.Show(Paths.PREFAB_DEGREE_VIEW);
    }

    private void LoadScene(object[] args)
    {
        //todo:这里应该有个音效
        _view._transLoad.gameObject.SetActive(true);
        SceneMgr.Single.AsyncLoadScene(SceneName.Game);
    }
}
