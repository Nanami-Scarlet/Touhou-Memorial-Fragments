using UnityEngine;

public interface ILoader
{
    GameObject LoadPrefab(string path);
    GameObject LoadPrefabAndInstantiate(string path, Transform parent);
    T Load<T>(string path) where T : Object;
    T[] LoadAll<T>(string path) where T : Object;
}
