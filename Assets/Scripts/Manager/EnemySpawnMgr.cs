using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BulletPro;

//普通妖精生成器
public class EnemySpawnMgr : MonoBehaviour
{
    private static List<GameObject> _listEnemy;

    public void Init()
    {
        _listEnemy = new List<GameObject>();

        MessageMgr.Single.AddListener(MsgEvent.EVENT_RELEASE_CARD, ReleaseCard);
    }

    private void OnDestroy()
    {
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_RELEASE_CARD, ReleaseCard);
    }

    public void Spawn(EnemyData data)
    {
        string id = data.TypeID;

        GameObject enemy = PoolMgr.Single.Spawn(id);

        enemy.GetComponent<EnemyController>().Init(data);
        enemy.GetComponent<BehaviourBase>().SetBehaviour(data.HP, data.PCount, data.PointCount);
        enemy.GetComponent<BulletReceiver>().enabled = true;
        enemy.GetComponent<SpriteRenderer>().enabled = true;
        enemy.GetComponent<EnemyBehaviour>().ResetBehaiour();
        YinEnemyView yin = enemy.GetComponent<YinEnemyView>();
        if(yin != null)
        {
            yin._ringSpriteRenderer.enabled = true;
        }

        _listEnemy.Add(enemy);
    }

    public static void DeSpawn(GameObject enemy)
    {
        PoolMgr.Single.Despawn(enemy);
        --GameModel.Single.EnemyCount;
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
