using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolMgr : NormalSingleton<PoolMgr>, IInit
{
    private Dictionary<string, GameObjectPool> _dicNamePool = new Dictionary<string, GameObjectPool>();

    private GameObject _self;

    public void Init()
    {
        _self = new GameObject("PoolMgr");
        PoolConfig config = new PoolConfig();

        GameObjectPool pool = null;
        for (int i = 0; i < config.ListData.Count; ++i)
        {
            PoolData data = config.ListData[i];

            GameObject prefab = LoadMgr.Single.LoadPrefab(data.Path);
            pool = new GameObjectPool(prefab, data.Count, _self.transform);
            _dicNamePool.Add(prefab.name, pool);
        }
    }

    public GameObject Spawn(string name)
    {
        if (_dicNamePool.ContainsKey(name))
        {
           return _dicNamePool[name].Spawn();
        }

        Debug.LogError("不存在的对象池，名字为：" + name);
        return null;
    }

    public void Despawn(GameObject go)
    {
        string name = go.name.Replace("(Clone)", "");

        if(_dicNamePool.ContainsKey(name))
        {
            _dicNamePool[name].Despawn(go);
        }
        else
        {
            Debug.LogError("欲要删除的物体没有对象池！物体名字为：" + name);
        }
    }

    public void ClearPool()
    {
        _dicNamePool.Clear();
    }
}
