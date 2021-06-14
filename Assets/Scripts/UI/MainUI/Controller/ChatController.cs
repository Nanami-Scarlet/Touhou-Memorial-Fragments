using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        MessageMgr.Single.AddListener(MsgEvent.EVENT_SHOW_DIALOG, ShowDialog);
    }

    public override void Hide()
    {
        base.Hide();

        InputMgr.Single.RemoveListener(KeyCode.Z);

        MessageMgr.Single.RemoveListener(KeyCode.Z, PressZ);
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_SHOW_DIALOG, ShowDialog);
    }

    private void ShowDialog(object[] args)
    {
        string stageName = (string)args[0];
        DataMgr.Single.GetDialogData(out _listDialogs, stageName);
        //GameStateModel.Single.IsChating = true;

        _view.SetDialogData(_listDialogs);
        if (_listDialogs[_view.CurIndex].IsCallBack)
        {
            _view.ShowDialog();
            gameObject.SetActive(true);                     //显示对话框
            //InputMgr.Single.AddListener(KeyCode.Z);

            GameStateModel.Single.IsChating = true;
        }
        else
        {
            MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_CHAT_CALLBACK,
                new ChatCallBack(_listDialogs[_view.CurIndex].CallBack, () =>
                {
                    _view.ShowDialog();
                    gameObject.SetActive(true);             //显示对话框
                    //InputMgr.Single.AddListener(KeyCode.Z);
                    _listDialogs[_view.CurIndex].IsCallBack = true;

                    GameStateModel.Single.IsChating = true;
                }));
        }
    }

    private void PressZ(object[] args)
    {
        if (!_view.IsComplete)
        {
            AudioMgr.Single.PlayUIEff(Paths.AUDIO_SHOOT_EFF);
            if (_listDialogs[_view.CurIndex].IsCallBack)
            {
                _view.PressZ();
            }
            else
            {
                //InputMgr.Single.RemoveListener(KeyCode.Z);

                MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_CHAT_CALLBACK,
                    new ChatCallBack(_listDialogs[_view.CurIndex].CallBack, () =>
                    {
                        _view.PressZ();

                        //InputMgr.Single.AddListener(KeyCode.Z);
                        _listDialogs[_view.CurIndex].IsCallBack = true;
                    }));
            }
        }
        else
        {
            UIManager.Single.Hide(Paths.PREFAB_CHAT_VIEW);
            GameStateModel.Single.IsChating = false;

            _view.CurIndex = 0;
            _view.IsComplete = false;
        }
    }
}

public class ChatCallBack
{
    public int ID { get; set; }
    public Action CallBack { get; set; }

    public ChatCallBack(int id, Action cb)
    {
        ID = id;
        CallBack = cb;
    }
}
