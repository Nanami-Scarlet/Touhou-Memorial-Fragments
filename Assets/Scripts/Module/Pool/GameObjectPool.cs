using System.Collections.Generic;
using UnityEngine;

public class GameObjectPool
{
    private List<GameObject> _listActive;
    private List<GameObject> _listInactive;

    private GameObject _selfGo;
    private GameObject _prefab;

    public GameObjectPool(GameObject prefab, GameObject parent, int preCount)
    {
        _listActive = new List<GameObject>(preCount);
        _listInactive = new List<GameObject>(preCount);

        _selfGo = new GameObject(prefab.name + "Pool");
        _selfGo.transform.SetParent(parent.transform);
        _prefab = prefab;

        for(int i = 0; i < preCount; ++i)
        {
            GameObject go = GameObject.Instantiate(_prefab, _selfGo.transform);
            go.SetActive(false);
            _listInactive.Add(go);
        }
    }

    public GameObject Spawn()
    {
        GameObject temp;

        if(_listInactive.Count > 0)
        {
            temp = _listInactive[_listInactive.Count - 1];
            temp.SetActive(true);
            _listInactive.Remove(temp);
        }
        else
        {
            temp = GameObject.Instantiate(_prefab, _selfGo.transform);
        }

        _listActive.Add(temp);
        return temp;
    }

    public void DeSpawn(GameObject go)
    {
        if (_listActive.Contains(go))
        {
            _listActive.Remove(go);
            go.SetActive(false);
            _listInactive.Add(go);
        }
    }

    public int CountActive()
    {
        return _listActive.Count;
    }
}
