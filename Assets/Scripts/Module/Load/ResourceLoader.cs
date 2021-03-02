using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
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

    public void LoadConfig(string path, Action<string> callback)
    {
        CoroutineMgr.Single.Execute(Config(path, callback));
    }

    private IEnumerator Config(string path, Action<string> callback)
    {
        if(Application.platform != RuntimePlatform.Android)
        {
            path = "file://" + path;
        }

        UnityWebRequest request = UnityWebRequest.Get(path);
        yield return request.SendWebRequest();

        if (request.error != null)
        {
            Debug.LogError("资源加载错误，资源路径为：" + path);
        }

        callback(request.downloadHandler.text);
    }
}
