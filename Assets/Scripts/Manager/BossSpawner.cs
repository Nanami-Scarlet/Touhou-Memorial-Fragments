using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossSpawner : MonoBehaviour
{
    public void Init()
    {

    }

    public GameObject Spawn(SingleBossInitData data)
    {
        string id = data.BossType;

        GameObject boss = PoolMgr.Single.Spawn(id);
        boss.GetComponent<BossController>().Init(data);

        return boss;
    }

    public static void DeSpawn(GameObject boss)
    {
        PoolMgr.Single.Despawn(boss);
    }
}
