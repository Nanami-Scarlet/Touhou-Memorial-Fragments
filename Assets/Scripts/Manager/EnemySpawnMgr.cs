using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnMgr : MonoBehaviour
{
    private List<GameObject> _listEnemy;
    private Dictionary<string, string> _dicIDEnemy = null;

    public void Init()
    {
        _listEnemy = new List<GameObject>();

        _dicIDEnemy = new Dictionary<string, string>()
        {
            { "1", Paths.PREFAB_ENEMY1 },
            { "2", Paths.PREFAB_ENEMY2 },
            { "3", Paths.PREFAB_ENEMY3 },
            { "4", Paths.PREFAB_ENEMY4 },
            { "5", Paths.PREFAB_ENEMY5 },
            { "6", Paths.PREFAB_ENEMY6 },
            { "7", Paths.PREFAB_ENEMY7 },
            { "8", Paths.PREFAB_ENEMY8 },
            { "9", Paths.PREFAB_ENEMY9 },
            //todo:Boss的编号记得加上
        };
    }

    public void Spawn(EnemyData data)
    {
        string id = data.TypeID;

        if (!_dicIDEnemy.ContainsKey(id))
        {
            Debug.LogError("不存在该敌机编号，编号：" + id);
            return;
        }

        string enemyName = _dicIDEnemy[id];
        GameObject enemy = PoolMgr.Single.Spawn(enemyName);

        enemy.GetComponent<EnemyController>().Init(data);

        _listEnemy.Add(enemy);
    }

    public void DeSpawn(GameObject enemy)
    {
        PoolMgr.Single.DeSpawn(enemy);
        _listEnemy.Remove(enemy);
    }

    public int GetEnemyCount()
    {
        return _listEnemy.Count;
    }
}
