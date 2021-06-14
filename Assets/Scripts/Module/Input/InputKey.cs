using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputKey : IInput
{
    private List<KeyCode> _listKeyCode;
    private List<KeyCode> _listGameKeyCode;
    private Action<KeyCode> _keyEvent;
    private Action<KeyCode, InputState> _keyGamingEvent;

    public InputKey()
    {
        _listKeyCode = new List<KeyCode>();
        _listGameKeyCode = new List<KeyCode>();
        _keyEvent = null;
        _keyGamingEvent = null;
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

    public void AddKeyEvent(Action<KeyCode> keyEvent)
    {
        _keyEvent = keyEvent;
    }

    public void AddKeyGamingEvent(Action<KeyCode, InputState> keyGamingEvent)
    {
        _keyGamingEvent = keyGamingEvent;
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

        if (!GameStateModel.Single.IsChating && !GameStateModel.Single.IsPause)
        {
            for (int i = 0; i < _listGameKeyCode.Count; ++i)
            {
                if (Input.GetKey(_listGameKeyCode[i]))
                {
                    _keyGamingEvent(_listGameKeyCode[i], InputState.PRESS);
                }

                if (Input.GetKeyUp(_listGameKeyCode[i]))
                {
                    _keyGamingEvent(_listGameKeyCode[i], InputState.UP);
                }

                if (Input.GetKeyDown(_listGameKeyCode[i]))
                {
                    _keyGamingEvent(_listGameKeyCode[i], InputState.DOWN);
                }
            }
        }
    }

    public void UpdateKeyState()
    {
        for (int i = 0; i < _listGameKeyCode.Count; ++i)
        {
            _keyGamingEvent(_listGameKeyCode[i], InputState.UP);
        }
    }
}
