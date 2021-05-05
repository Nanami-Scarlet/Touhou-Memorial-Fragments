using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BulletPro;

//普通妖精生成器
public class EnemySpawnMgr : MonoBehaviour
{
    private static List<GameObject> _listEnemy;
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
            { "10", Paths.PREFAB_ENEMY10 },
            { "11", Paths.PREFAB_ENEMY11 },
            //todo:Boss的编号记得加上
        };

        MessageMgr.Single.AddListener(MsgEvent.EVENT_RELEASE_CARD, ReleaseCard);
    }

    private void OnDestroy()
    {
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_RELEASE_CARD, ReleaseCard);
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
        //GameObject enemy = LoadMgr.Single.LoadPrefabAndInstantiate(enemyName);
        GameObject enemy = PoolMgr.Single.Spawn(id);

        //enemy.GetComponent<EnemyView>().Init();
        enemy.GetComponent<EnemyController>().Init(data);
        enemy.GetComponent<BehaviourBase>().Init(data.HP, data.PCount, data.PointCount);
        enemy.GetComponent<BulletReceiver>().enabled = true;
        enemy.GetComponent<SpriteRenderer>().enabled = true;
        enemy.GetComponent<EnemyBehaviour>().IsDead = false;

        _listEnemy.Add(enemy);
    }

    public static void DeSpawn(GameObject enemy)
    {
        //Destroy(enemy);
        PoolMgr.Single.Despawn(enemy);
        --GameModel.Single.EnemyCount;
        //Debug.Log(GameModel.Single.EnemyCount);
        _listEnemy.Remove(enemy);
    }

    public int GetEnemyCount()
    {
        return _listEnemy.Count;
    }

    private void ReleaseCard(object[] args)
    {
        foreach(var enemy in _listEnemy)
        {
            enemy.GetComponent<EnemyBehaviour>().Dead();
        }

        _listEnemy.Clear();
    }
}
