using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LifeCycleConfig
{
    public static Dictionary<LifeName, ILifeCycle> DicLifeConfig = new Dictionary<LifeName, ILifeCycle>
    {
        { LifeName.INIT, new LifeCycle<IInit>() },
        { LifeName.UPDATE, new LifeCycle<IUpdate>() }
    };

    public static Dictionary<LifeName, Action> LifeFun = new Dictionary<LifeName, Action>
    {
        { LifeName.INIT, () => DicLifeConfig[LifeName.INIT].Execute((IInit o) => o.Init()) },
        { LifeName.UPDATE, () => DicLifeConfig[LifeName.UPDATE].Execute((IUpdate o) => o.UpdateFun()) }
    };
}

public enum LifeName
{
    INIT,
    UPDATE
}
