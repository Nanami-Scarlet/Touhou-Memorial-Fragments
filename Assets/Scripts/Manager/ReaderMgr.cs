using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReaderMgr : NormalSingleton<ReaderMgr>
{
    private Dictionary<string, IReader> _dicPathReader;

    public ReaderMgr()
    {
        _dicPathReader = new Dictionary<string, IReader>();
    }

    public IReader GetReader(string path)
    {
        if (_dicPathReader.ContainsKey(path))
        {
            return _dicPathReader[path];
        }

        IReader reader = ReaderConfig.GetReader(path);
        LoadMgr.Single.LoadConfig(path, data => 
        {
            reader.SetData(data);
        });

        if (reader != null)
        {
            _dicPathReader.Add(path, reader);
        }
        else
        {
            Debug.LogError("没有获取对应路径的读取器，路径为：" + path);
        }

        return reader;
    }
}
