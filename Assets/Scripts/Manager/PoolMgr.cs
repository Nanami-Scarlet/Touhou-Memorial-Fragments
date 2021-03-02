using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class PoolMgr : MonoSingleton<PoolMgr>
{
    private Dictionary<string, GameObjectPool> _dicPool = new Dictionary<string, GameObjectPool>();
    private bool _isInit = false;

    public async void Init(Action sceneAction)
    {
        if (!_isInit)
        {
            GameObjectPool pool = null;
            GameObject prefab = null;
            PoolData data = null;

            PoolConfig config = new PoolConfig();
            for (int i = 0; i < config.ListData.Count; ++i)
            {
                data = config.ListData[i];
                prefab = LoadMgr.Single.LoadPrefab(data.Path);
                pool = new GameObjectPool(prefab, gameObject, data.Count);
                _dicPool.Add(data.Path, pool);

                await Task.Delay(100);
            }
        }
        _isInit = true;

        sceneAction();
    }

    public GameObject Spawn(string path)
    {
        if (!_dicPool.ContainsKey(path))
        {
            Debug.LogError("该路径下的prefab没有对象池，路径为：" + path);
            return null;
        }

        return _dicPool[path].Spawn();
    }

    public bool DeSpawn(GameObject go)
    {
        string name = go.name.Replace("Clone", "");
        foreach(var pair in _dicPool)
        {
            if (pair.Key.Contains(name))
            {
                pair.Value.DeSpawn(go);
                return true;
            }
        }

        return false;
    }

    public int CountActive()           //所有敌机的数量
    {
        int num = 0;

        foreach(var pair in _dicPool)
        {
            num += pair.Value.CountActive();
        }

        return num;
    }
}
