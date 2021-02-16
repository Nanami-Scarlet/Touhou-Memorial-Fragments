using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MessageMgr : NormalSingleton<MessageMgr>, IMessage
{
    [SerializeField]
    private readonly IMessage _msg = null;

    public MessageMgr()
    {
        _msg = new MessageSystem();
    }

    public void AddListener(int key, Action<object[]> callback)
    {
        _msg.AddListener(key, callback);
    }

    public void AddListener(string key, Action<object[]> callback)
    {
        _msg.AddListener(key, callback);
    }

    public void DispatchMsg(int key, params object[] args)
    {
        _msg.DispatchMsg(key, args);
    }

    public void DispatchMsg(string key, params object[] args)
    {
        _msg.DispatchMsg(key, args);
    }

    public void RemoveListener(int key, Action<object[]> callback)
    {
        _msg.RemoveListener(key, callback);
    }

    public void RemoveListener(string key, Action<object[]> callback)
    {
        _msg.RemoveListener(key, callback);
    }

    public void AddListener(KeyCode code, Action<object[]> callback)
    {
        _msg.AddListener(code.ToString(), callback);
    }

    public void RemoveListener(KeyCode code, Action<object[]> callback)
    {
        _msg.RemoveListener(code.ToString(), callback);
    }
}