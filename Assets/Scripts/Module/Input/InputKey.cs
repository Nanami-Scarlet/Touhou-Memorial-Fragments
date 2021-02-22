using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputKey : IInput
{
    private List<KeyCode> _listKeyCode;
    private List<KeyCode> _listGameKeyCode;
    private Action<KeyCode> _keyEvent;
    private Action _update;

    public InputKey()
    {
        _listKeyCode = new List<KeyCode>();
        _listGameKeyCode = new List<KeyCode>();
        _keyEvent = null;
    }

    public void AddListener(KeyCode key)
    {
        if (!_listKeyCode.Contains(key))
        {
            _listKeyCode.Add(key);
        }
    }

    public void RemoveListener(KeyCode key)
    {
        if (_listKeyCode.Contains(key))
        {
            _listKeyCode.Remove(key);
        }
    }

    public void AddGameListener(KeyCode key)
    {
        if (!_listGameKeyCode.Contains(key))
        {
            _listGameKeyCode.Add(key);
        }
    }

    public void RemoveGameListener(KeyCode key)
    {
        if (_listGameKeyCode.Contains(key))
        {
            _listGameKeyCode.Remove(key);
        }
    }

    public void AddUpdateListener(Action action)
    {
        _update += action;
    }

    public void RemoveUpdateListener(Action action)
    {
        _update -= action;
    }

    public void AddKeyEvent(Action<KeyCode> keyEvent)
    {
        _keyEvent = keyEvent;
    }

    public void Execute()
    {
        for(int i = 0; i < _listKeyCode.Count; ++i)
        {
            if (Input.GetKeyDown(_listKeyCode[i]))
            {
                _keyEvent(_listKeyCode[i]);
            }
        }

        if (GameStateModel.Single.Status == GameStatus.Gameing)
        {
            for (int i = 0; i < _listGameKeyCode.Count; ++i)
            {
                if (Input.GetKey(_listGameKeyCode[i]))
                {
                    _keyEvent(_listGameKeyCode[i]);
                }

                if (Input.GetKeyUp(_listGameKeyCode[i]))
                {
                    _update();
                }
            }
        }
    }
}
