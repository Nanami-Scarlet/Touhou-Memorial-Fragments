using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : ControllerBase
{
    public GameView _view;

    public override void InitAndChild()
    {
        
    }

    public override void Show()
    {
        InputMgr.Single.AddListener(KeyCode.Escape);

        MessageMgr.Single.AddListener(KeyCode.Escape, OnPause);

        MessageMgr.Single.AddListener(MsgEvent.EVENT_STAGE_ANIM, StageAnim);
        MessageMgr.Single.AddListener(MsgEvent.EVENT_UPDATE_SCORE, UpdateScore);
        MessageMgr.Single.AddListener(MsgEvent.EVENT_UPDATE_MANA, UpdateMana);
        MessageMgr.Single.AddListener(MsgEvent.EVENT_UPDATE_LIFT, UpdateLife);
        MessageMgr.Single.AddListener(MsgEvent.EVENT_UPDATE_BOMB, UpdateBomb);
        MessageMgr.Single.AddListener(MsgEvent.EVENT_UPDATE_GRAZE, UpdateGraze);
        MessageMgr.Single.AddListener(MsgEvent.EVENT_UPDATE_POINT, UpdatePoint);
        MessageMgr.Single.AddListener(MsgEvent.EVENT_UPDATE_MEMORY, UpdateMemory);

        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_UPDATE_MEMORY);
    }

    public override void Hide()
    {
        InputMgr.Single.RemoveListener(KeyCode.Escape);

        MessageMgr.Single.RemoveListener(KeyCode.Escape, OnPause);

        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_STAGE_ANIM, StageAnim);
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_UPDATE_SCORE, UpdateScore);
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_UPDATE_MANA, UpdateMana);
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_UPDATE_LIFT, UpdateLife);
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_UPDATE_BOMB, UpdateBomb);
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_UPDATE_GRAZE, UpdateGraze);
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_UPDATE_POINT, UpdatePoint);
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_UPDATE_MEMORY, UpdateMemory);
    }

    private void OnPause(object[] args)
    {
        Time.timeScale = 0;
        AudioMgr.Single.PlayUIEff(Paths.AUDIO_PAUSE_EFF, 0.2f);
        UIManager.Single.Show(Paths.PREFAB_PAUSE_VIEW/*, false*/);
        UIManager.Single.HideController(Paths.PREFAB_GAME_VIEW);
        AudioMgr.Single.StopBGM();
        GameStateModel.Single.IsPause = true;
    }

    private void StageAnim(object[] args)
    {
        _view.StageAnim();
    }

    private void UpdateScore(object[] args)
    {
        int score = (int)args[0];

        _view.UpdateScore(score);
    }

    private void UpdateMana(object[] args)
    {
        int mana = (int)args[0];

        _view.UpdateMana(mana);
    }

    private void UpdateLife(object[] args)
    {
        _view.UpdateLife();
    }

    private void UpdateBomb(object[] args)
    {
        _view.UpdateBomb();
    }

    private void UpdateGraze(object[] args)
    {
        int graze = (int)args[0];

        _view.UpdateGraze(graze);
    }

    private void UpdatePoint(object[] args)
    {
        _view.UpdatePoint();
    }

    private void UpdateMemory(object[] args)
    {
        _view.UpdateMemory();
    }
}
