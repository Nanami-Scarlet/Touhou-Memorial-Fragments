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
        MessageMgr.Single.AddListener(MsgEvent.EVENT_GET_LIFT, GetLife);
        MessageMgr.Single.AddListener(MsgEvent.EVENT_GET_BOMB, GetBomb);
        MessageMgr.Single.AddListener(MsgEvent.EVENT_UPDATE_GRAZE, UpdateGraze);
        MessageMgr.Single.AddListener(MsgEvent.EVENT_UPDATE_POINT, UpdatePoint);
        MessageMgr.Single.AddListener(MsgEvent.EVENT_UPDATE_MEMORY, UpdateMemory);
        MessageMgr.Single.AddListener(MsgEvent.EVENT_UPDATE_GOD, UpdateGod);
        MessageMgr.Single.AddListener(MsgEvent.EVENT_PLAYER_USE_LIFE, UseLife);
        MessageMgr.Single.AddListener(MsgEvent.EVENT_PLAYER_USE_BOMB, UseBomb);
        MessageMgr.Single.AddListener(MsgEvent.EVENT_PLAY_GET_CARD_INFO_ANIM, PlayGetCardInfoAnim);
        MessageMgr.Single.AddListener(MsgEvent.EVENT_PLAY_STAGE_CLEAR_ANIM, PlayStageClearAnim);

        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_UPDATE_MEMORY);
    }

    public override void Hide()
    {
        InputMgr.Single.RemoveListener(KeyCode.Escape);

        MessageMgr.Single.RemoveListener(KeyCode.Escape, OnPause);

        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_STAGE_ANIM, StageAnim);
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_UPDATE_SCORE, UpdateScore);
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_UPDATE_MANA, UpdateMana);
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_GET_LIFT, GetLife);
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_GET_BOMB, GetBomb);
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_UPDATE_GRAZE, UpdateGraze);
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_UPDATE_POINT, UpdatePoint);
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_UPDATE_MEMORY, UpdateMemory);
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_UPDATE_GOD, UpdateGod);
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_PLAYER_USE_LIFE, UseLife);
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_PLAYER_USE_BOMB, UseBomb);
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_PLAY_GET_CARD_INFO_ANIM, PlayGetCardInfoAnim);
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_PLAY_STAGE_CLEAR_ANIM, PlayStageClearAnim);
    }

    private void OnPause(object[] args)
    {
        Time.timeScale = 0;
        AudioMgr.Single.PlayUIEff(Paths.AUDIO_PAUSE_EFF, 0.2f);
        UIManager.Single.Show(Paths.PREFAB_PAUSE_VIEW);
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
        _view.UpdateScore(GameModel.Single.Score);
    }

    private void UpdateMana(object[] args)
    {
        _view.UpdateMana(PlayerModel.Single.Mana);
    }

    private void GetLife(object[] args)
    {
        if(PlayerModel.Single.Life < 8)
        {
            ++PlayerModel.Single.LifeFragment;

            if(PlayerModel.Single.LifeFragment == Const.FULL_LIFE_FRAGMENT)
            {
                PlayerModel.Single.LifeFragment = 0;
                ++PlayerModel.Single.Life;

                _view.ResetItem(_view._transLifeItems, PlayerModel.Single.Life);
                AudioMgr.Single.PlayGameEff(AudioType.Extend);

                return;
            }

            _view.UpdateLife();
        }
    }

    private void UseLife(object[] args)
    {
        if (PlayerModel.Single.Life > 0)
        {
            --PlayerModel.Single.Life;
            _view.ResetItem(_view._transLifeItems, PlayerModel.Single.Life);
        }
    }

    private void GetBomb(object[] args)
    {
        if(PlayerModel.Single.Bomb < 8)
        {
            ++PlayerModel.Single.BombFragment;

            if (PlayerModel.Single.BombFragment == Const.FULL_BOMB_FRAGMENT)
            {
                PlayerModel.Single.BombFragment = 0;
                _view.UpdateBomb();
                ++PlayerModel.Single.Bomb;

                _view.ResetItem(_view._transBombItems, PlayerModel.Single.Bomb);
                AudioMgr.Single.PlayGameEff(AudioType.GetBomb);

                return;
            }

            _view.UpdateBomb();
        }
    }

    private void UseBomb(object[] args)
    {
        if (PlayerModel.Single.Bomb > 0)
        {
            --PlayerModel.Single.Bomb;
            _view.ResetItem(_view._transBombItems, PlayerModel.Single.Bomb);
        }
    }

    private void UpdateGraze(object[] args)
    {
        ++PlayerModel.Single.MAX_GET_POINT;

        _view.UpdateGraze(PlayerModel.Single.Graze);
    }

    private void UpdatePoint(object[] args)
    {
        _view.UpdatePoint();
    }

    private void UpdateMemory(object[] args)
    {
        if (PlayerModel.Single.MemoryFragment < 3)
        {
            if (PlayerModel.Single.MemoryProcess >= 100)
            {
                PlayerModel.Single.MemoryProcess = 0;
                ++PlayerModel.Single.MemoryFragment;
            }

            _view.UpdateMemory();
        }
    }

    private void UpdateGod(object[] args)
    {
        if (PlayerModel.Single.GodProcess >= 100)
        {
            PlayerModel.Single.GodProcess = 100;
        }

        if (PlayerModel.Single.GodProcess <= 0)
        {
            PlayerModel.Single.GodProcess = 0;
        }

        _view.UpdateGod();
    }

    private void PlayGetCardInfoAnim(object[] args)
    {
        GetCardInfo info = (GetCardInfo)args[0];

        _view.PlayGetCardInfoAnim(info);
    }

    private void PlayStageClearAnim(object[] args)
    {
        _view.PlayStageClearAnim();
    }
}
