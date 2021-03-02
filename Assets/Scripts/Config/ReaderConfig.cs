using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReaderConfig
{
    private static readonly Dictionary<string, Func<IReader>> _dicTypeReader = new Dictionary<string, Func<IReader>>()
    {
        { ".json", () => new JsonReader() }
    };

    public static IReader GetReader(string path)
    {
        foreach(var pair in _dicTypeReader)
        {
            if (path.Contains(pair.Key))
            {
                return pair.Value();
            }
        }

        Debug.LogError("没有改路径后缀的读取器，路径为" + path);
        return null;
    }
}
