using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionMgr<T>
{
    private HashSet<Action<T>> _hashAction;     //这个集合用于判重
    private Action<T> action;

    public ActionMgr()
    {
        _hashAction = new HashSet<Action<T>>();
        action = null;
    }

    public void Add(Action<T> callback)
    {
        if (_hashAction.Add(callback))
        {
            action += callback;
        }
    }

    public void Remove(Action<T> callback)
    {
        if (_hashAction.Remove(callback))
        {
            action -= callback;
        }
    }

    public void Execute(T t)
    {
        if(action != null)
        {
            action(t);
        }
    }
}
