using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossSpawnMgr : MonoBehaviour, ISpawn
{
    public void Init()
    {

    }

    public GameObject Spawn(EntityData data)
    {
        string id = data.EntityType;
        SingleBossInitData singleBossInitData = (SingleBossInitData)data;

        GameObject boss = PoolMgr.Single.Spawn(id);
        boss.GetComponent<BossView>().Init();
        boss.GetComponent<BossController>().Init(singleBossInitData);

        return boss;
    }

    public static void DeSpawn(GameObject boss)
    {
        PoolMgr.Single.Despawn(boss);
    }
}
