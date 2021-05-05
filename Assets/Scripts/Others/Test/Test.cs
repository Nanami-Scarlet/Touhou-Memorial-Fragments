using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    private void Start()
    {
        LifeCycleAddConfig lifeCycleAddConfig = new LifeCycleAddConfig();
        lifeCycleAddConfig.Init();
        PoolMgr.Single.Init();

        LifeCycleConfig.LifeFun[LifeName.INIT]();


        gameObject.AddComponent<GameProcessMgr>().Init();
    }
}
