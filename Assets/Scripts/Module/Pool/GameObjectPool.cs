using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using DG.Tweening;

public class GameObjectPool
{
    private GameObject _prefab;
    private GameObject _selfGo;

    private List<GameObject> _listActive;
    private List<GameObject> _listInactive;

    public GameObjectPool(GameObject prefab, int maxCount, Transform parent)
    {
        Init(prefab, maxCount, parent);
    }

    private /*async*/ void Init(GameObject prefab, int maxCount, Transform parent)
    {
        _prefab = prefab;
        _selfGo = new GameObject(prefab.name + "Pool");
        _selfGo.transform.SetParent(parent);
        _listActive = new List<GameObject>(maxCount);
        _listInactive = new List<GameObject>(maxCount);

        for (int i = 0; i < maxCount; ++i)
        {
            GameObject go = Object.Instantiate(_prefab, _selfGo.transform);
            _listInactive.Add(go);
            go.SetActive(false);
            //await Task.Delay(10);
        }
    }

    public GameObject Spawn()
    {
        GameObject go = null;

        if (_listInactive.Count > 0)
        {
            go = _listInactive[0];
            _listInactive.RemoveAt(0);
            go.SetActive(true);
        }
        else
        {
            go = Object.Instantiate(_prefab, _selfGo.transform);
        }

        _listActive.Add(go);

        return go;
    }

    public void Despawn(GameObject go)
    {
        if (_listActive.Contains(go))
        {
            _listActive.Remove(go);
            _listInactive.Add(go);
            go.transform.DOKill();
            go.SetActive(false);
        }
    }

    public void ClearPool()
    {
        for (int i = _listActive.Count - 1; i >= 0; --i)
        {
            GameObject go = _listActive[i];
            _listActive.RemoveAt(i);
            go.transform.DOKill();
            go.SetActive(false);
            _listInactive.Add(go);
        }
    }
}
