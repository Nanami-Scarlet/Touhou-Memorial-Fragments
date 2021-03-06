using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;


public class ResourceLoader : ILoader
{
    public GameObject LoadPrefabAndInstantiate(string path, Transform parent)
    {
        GameObject prefab = LoadPrefab(path);
        return Object.Instantiate(prefab, parent);
    }

    public GameObject LoadPrefab(string path)
    {
        return Resources.Load<GameObject>(path);
    }

    public T Load<T>(string path) where T : Object
    {
        return Resources.Load<T>(path);
    }

    public T[] LoadAll<T>(string path) where T : Object
    {
        return Resources.LoadAll<T>(path);   
    }

    public TextAsset LoadConfig(string path)
    {
        TextAsset config = Resources.Load<TextAsset>(path);
        if (!config)
        {
            Debug.LogError("该文件路径不存在，路径为：" + path);
            return null;
        }

        return config;
    }
}
