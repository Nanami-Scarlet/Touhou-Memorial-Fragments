using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputKey : IInput
{
    private List<KeyCode> _listKeyCode;
    private Action<KeyCode> _keyEvent;

    public InputKey()
    {
        _listKeyCode = new List<KeyCode>();
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
    }
}
