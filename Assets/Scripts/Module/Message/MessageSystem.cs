using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MessageSystem : IMessage
{
    private Dictionary<int, ActionMgr<object[]>> _dicIntAndAction = new Dictionary<int, ActionMgr<object[]>>();
    private Dictionary<string, ActionMgr<object[]>> _dicStrAndAction = new Dictionary<string, ActionMgr<object[]>>();

    public void AddListener(int key, Action<object[]> callback)
    {
        if (!_dicIntAndAction.ContainsKey(key))
        {
            _dicIntAndAction[key] = new ActionMgr<object[]>();
        }
        _dicIntAndAction[key].Add(callback);
    }

    public void RemoveListener(int key, Action<object[]> callback)
    {
        if (_dicIntAndAction.ContainsKey(key))
        {
            _dicIntAndAction[key].Remove(callback);
        }
    }

    public void DispatchMsg(int key, params object[] args)
    {
        if (_dicIntAndAction.ContainsKey(key))
        {
            _dicIntAndAction[key].Execute(args);
        }
    }

    public void AddListener(string key, Action<object[]> callback)
    {
        if (!_dicStrAndAction.ContainsKey(key))
        {
            _dicStrAndAction[key] = new ActionMgr<object[]>();
        }
        _dicStrAndAction[key].Add(callback);
    }

    public void RemoveListener(string key, Action<object[]> callback)
    {
        if (_dicStrAndAction.ContainsKey(key))
        {
            _dicStrAndAction[key].Remove(callback);
        }
    }

    public void DispatchMsg(string key, params object[] args)
    {
        if (_dicStrAndAction.ContainsKey(key))
        {
            _dicStrAndAction[key].Execute(args);
        }
    }
}
