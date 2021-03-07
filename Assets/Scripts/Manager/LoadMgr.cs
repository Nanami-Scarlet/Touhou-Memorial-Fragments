using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class LoadMgr : NormalSingleton<LoadMgr>, ILoader
{
    [SerializeField]
    private readonly ILoader _loader;

    public LoadMgr()
    {
        _loader = new ResourceLoader();
    }

    public GameObject LoadPrefabAndInstantiate(string path, Transform parent = null)
    {
        return _loader.LoadPrefabAndInstantiate(path, parent);
    }

    public T Load<T>(string path) where T : Object
    {
        return _loader.Load<T>(path);
    }

    public T[] LoadAll<T>(string path) where T : Object
    {
        return _loader.LoadAll<T>(path);
    }

    public GameObject LoadPrefab(string path)
    {
        return _loader.LoadPrefab(path);
    }

    public TextAsset LoadConfig(string path)
    {
        return _loader.LoadConfig(path);
    }
}
