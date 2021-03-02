﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IReader
{
    IReader this[string key] { get; }
    IReader this[int key] { get; }
    void Get<T>(Action<T> callback);
    void SetData(string data);
    void Count(Action<int> callback);
}
