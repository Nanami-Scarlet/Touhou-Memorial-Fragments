using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputMgr : NormalSingleton<InputMgr>, IInput, IUpdate
{
    [SerializeField]
    private readonly InputKey _input = null;

    public InputMgr()
    {
        _input = new InputKey();
        _input.AddKeyEvent((key) => MessageMgr.Single.DispatchMsg(key.ToString()));
        _input.AddKeyGamingEvent((key, state) => MessageMgr.Single.DispatchMsg(GetKey(key, state)));
    }

    public void AddListener(KeyCode key)
    {
        _input.AddListener(key);
        AddUpdate();
    }

    public void RemoveListener(KeyCode key)
    {
        _input.RemoveListener(key);
    }

    public void AddGameListener(KeyCode key)
    {
        _input.AddGameListener(key);
        AddUpdate();
    }

    public void RemoveGameListener(KeyCode key)
    {
        _input.RemoveGameListener(key);
    }

    public void UpdateFun()
    {
        _input.Execute();
    }

    private void AddUpdate()
    {
        LifeCycleMgr.Single.Add(LifeName.UPDATE, this);
    }

    public string GetKey(KeyCode key, InputState state)
    {
        return key + state.ToString();
    }

    public void UpdateKeyState()
    {
        _input.UpdateKeyState();
    }
}
