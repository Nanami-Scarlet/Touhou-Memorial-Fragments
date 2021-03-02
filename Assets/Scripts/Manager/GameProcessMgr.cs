using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameProcessMgr : MonoBehaviour, IUpdate
{
    private EnemySpawnMgr _spawnMgr;
    private IReader _reader = null;

    private float _delay;

    public void Init()
    {
        _spawnMgr = gameObject.AddComponent<EnemySpawnMgr>();
        _spawnMgr.Init();

        _reader = ReaderMgr.Single.GetReader(Paths.CONFIG_ENEMY);

        //CoroutineMgr.Single.Execute(OnState("state1_1"));
    }

    public void UpdateFun()
    {
        
    }

    //private IEnumerator OnState(string stateName)
    //{

    //}
        
}
