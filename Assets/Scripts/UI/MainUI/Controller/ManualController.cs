using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManualController : ControllerBase
{
    public ManualView _view;

    public override void InitAndChild()
    {
        GameStateModel.Single.ManualOPation = 0;
    }

    public override void Show()
    {
        base.Show();

        InputMgr.Single.AddListener(KeyCode.UpArrow);
        InputMgr.Single.AddListener(KeyCode.DownArrow);
        InputMgr.Single.AddListener(KeyCode.X);
        InputMgr.Single.AddListener(KeyCode.Escape);

        MessageMgr.Single.AddListener(KeyCode.UpArrow, DecIndex);
        MessageMgr.Single.AddListener(KeyCode.DownArrow, IncIndex);
        MessageMgr.Single.AddListener(KeyCode.X, OnPressX);
        MessageMgr.Single.AddListener(KeyCode.Escape, OnPressX);
    }

    public override void Hide()
    {
        base.Hide();

        InputMgr.Single.RemoveListener(KeyCode.UpArrow);
        InputMgr.Single.RemoveListener(KeyCode.DownArrow);
        InputMgr.Single.RemoveListener(KeyCode.X);
        InputMgr.Single.RemoveListener(KeyCode.Escape);

        MessageMgr.Single.RemoveListener(KeyCode.UpArrow, DecIndex);
        MessageMgr.Single.RemoveListener(KeyCode.DownArrow, IncIndex);
        MessageMgr.Single.RemoveListener(KeyCode.X, OnPressX);
        MessageMgr.Single.RemoveListener(KeyCode.Escape, OnPressX);
    }

    private void DecIndex(object[] args)
    {
        AudioMgr.Single.PlayUIEff(Paths.AUDIO_SELECT_EFF);

        if (GameStateModel.Single.ManualOPation > 0)
        {
            --GameStateModel.Single.ManualOPation;

            _view.UpdateView(false);
        }
    }

    private void IncIndex(object[] args)
    {
        AudioMgr.Single.PlayUIEff(Paths.AUDIO_SELECT_EFF);

        if (GameStateModel.Single.ManualOPation < _view.MAX_INDEX)
        {
            ++GameStateModel.Single.ManualOPation;

            _view.UpdateView(true);
        }
    }

    private void OnPressX(object[] args)
    {
        AudioMgr.Single.PlayUIEff(Paths.AUDIO_CANCAL_EFF);

        UIManager.Single.Back();
    }
}
