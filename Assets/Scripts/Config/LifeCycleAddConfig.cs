using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LifeCycleAddConfig : IInit
{
    public void Init()
    {
        LifeInitAdd(TimeMgr.Single);
        LifeInitAdd(AudioMgr.Single);
        LifeInitAdd(SceneConfig.Single);
    }

    private void LifeInitAdd(object o)
    {
        LifeCycleMgr.Single.Add(LifeName.INIT, o);
    }
}
