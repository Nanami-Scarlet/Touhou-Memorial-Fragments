using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BulletPro;

public class EliteSpawnMgr : MonoBehaviour, ISpawn
{
    public void Init()
    {
        
    }

    public GameObject Spawn(EntityData data)
    {
        string type = data.EntityType;
        ElitleData e = (ElitleData)data;

        GameObject elite = PoolMgr.Single.Spawn(type);

        elite.GetComponent<EliteController>().Init(data);
        elite.GetComponent<EntityBehaviourBase>().SetBehaviour(e.HP, e.PCount, e.PointCount, e.LifeCount, e.BombCount);
        elite.GetComponent<EliteBehaviour>().ResetBehaiour();
        elite.GetComponent<SpriteRenderer>().enabled = true;
        elite.GetComponent<BulletReceiver>().enabled = true;

        ++GameModel.Single.EnemyCount;

        return elite;
    }

    public static void DeSpawn(GameObject elite)
    {
        PoolMgr.Single.Despawn(elite);
        --GameModel.Single.EnemyCount;
    }
}
