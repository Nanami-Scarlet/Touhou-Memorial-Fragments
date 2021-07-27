using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class ChatController : ControllerBase
{
    public ChatView _view;

    private List<DialogData> _listDialogs = new List<DialogData>();

    public override void InitAndChild()
    {
        
    }

    public override void Show()
    {
        base.Show();

        InputMgr.Single.AddListener(KeyCode.Z);

        MessageMgr.Single.AddListener(KeyCode.Z, PressZ);
        MessageMgr.Single.AddListener(MsgEvent.EVENT_PRESSZ, PressZ);
        MessageMgr.Single.AddListener(MsgEvent.EVENT_SHOW_DIALOG, ShowDialog);
        MessageMgr.Single.AddListener(MsgEvent.EVENT_SET_ENDING_PIC, SetEndingPic);
    }

    public override void Hide()
    {
        base.Hide();

        InputMgr.Single.RemoveListener(KeyCode.Z);

        MessageMgr.Single.RemoveListener(KeyCode.Z, PressZ);
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_PRESSZ, PressZ);
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_SHOW_DIALOG, ShowDialog);
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_SET_ENDING_PIC, SetEndingPic);

        _view.CurIndex = 0;
    }

    private void ShowDialog(object[] args)
    {
        string stageName = (string)args[0];
        DataMgr.Single.GetDialogData(out _listDialogs, stageName);

        _view.CurIndex = 0;
        _view.IsComplete = false;
        _view._tween.Kill();
        _view._txtdialog.text = "";
        _view.SetDialogData(_listDialogs);
        if (_listDialogs[_view.CurIndex].IsCallBack)
        {
            _view.ShowDialog();
            gameObject.SetActive(true);                     //显示对话框

            GameStateModel.Single.IsChating = true;
        }
        else
        {
            MessageMgr.Single.RemoveListener(KeyCode.Z, PressZ);
            DialogData data = _listDialogs[_view.CurIndex];
            MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_CHAT_CALLBACK,
                new ChatCallBack(data.CallBack, data.StrArg, () =>
                {
                    _view.ShowDialog();
                    gameObject.SetActive(true);             //显示对话框
                    data.IsCallBack = true;
                    MessageMgr.Single.AddListener(KeyCode.Z, PressZ);

                    GameStateModel.Single.IsChating = true;
                }));
        }
    }

    private void PressZ(object[] args)
    {
        if (_view.IsComplete)
        {
            UIManager.Single.Hide(Paths.PREFAB_CHAT_VIEW);
            GameStateModel.Single.IsChating = false;

            _view.IsComplete = false;

            return;
        }

        AudioMgr.Single.PlayUIEff(Paths.AUDIO_SHOOT_EFF);

        if (_view._tween.IsPlaying())
        {
            _view._tween.Complete();
        }
        else
        {
            if (_listDialogs[_view.CurIndex].IsCallBack)
            {
                _view.PressZ();
            }
            else
            {
                MessageMgr.Single.RemoveListener(KeyCode.Z, PressZ);
                DialogData data = _listDialogs[_view.CurIndex];
                MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_CHAT_CALLBACK,
                    new ChatCallBack(data.CallBack, data.StrArg, () =>
                    {
                        _view.PressZ();
                        MessageMgr.Single.AddListener(KeyCode.Z, PressZ);

                        data.IsCallBack = true;
                    }));
            }
        }
    }

    private void SetEndingPic(object[] args)
    {
        string name = (string)args[0];

        _view.SetEndingPic(name);
    }
}

public class ChatCallBack
{
    public int ID { get; set; }
    public string StrArg { get; set; }
    public Action CallBack { get; set; }

    public ChatCallBack(int id, string strArg, Action cb)
    {
        ID = id;
        StrArg = strArg;
        CallBack = cb;
    }
}
