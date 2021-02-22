using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : ControllerBase
{
    public override void InitChild()
    {
        
    }

    public override void Show()
    {
        InputMgr.Single.AddListener(KeyCode.Escape);

        MessageMgr.Single.AddListener(KeyCode.Escape, OnPause);
    }

    public override void Hide()
    {
        InputMgr.Single.RemoveListener(KeyCode.Escape);

        MessageMgr.Single.RemoveListener(KeyCode.Escape, OnPause);
    }

    private void OnPause(object[] args)
    {
        Time.timeScale = 0;
        AudioMgr.Single.PlayEff(Const.PAUSE_EFF, 0.2f);
        UIManager.Single.Show(Paths.PREFAB_PAUSE_VIEW, false);
        UIManager.Single.HideController(Paths.PREFAB_GAME_VIEW);
        AudioMgr.Single.StopBGM();
        GameStateModel.Single.Status = GameStatus.Pause;
    }
}
