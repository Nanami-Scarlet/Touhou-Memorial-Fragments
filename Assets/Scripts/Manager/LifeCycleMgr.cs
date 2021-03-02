using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LifeCycleMgr : MonoSingleton<LifeCycleMgr>, IInit
{
    public void Init()
    {
        LifeCycleAddConfig lifeCycleAddConfig = new LifeCycleAddConfig();
        lifeCycleAddConfig.Init();

        LifeCycleConfig.LifeFun[LifeName.INIT]();
    }

    public void Add(LifeName name, object o)
    {
        LifeCycleConfig.DicLifeConfig[name].Add(o);
    }

    public void Remove(LifeName name, object o)
    {
        LifeCycleConfig.DicLifeConfig[name].Remove(o);
    }

    private void Update()
    {
        LifeCycleConfig.LifeFun[LifeName.UPDATE]();
    }
}

public interface ILifeCycle
{
    void Add(object o);
    void Remove(object o);
    void Execute<T>(Action<T> execute);
}

public class LifeCycle<T> : ILifeCycle
{
    private List<object> _listClass = new List<object>();

    public void Add(object o)
    {
        if (o is T && !_listClass.Contains(o))
        {
            _listClass.Add(o);
        }
    }

    public void Remove(object o)
    {
        if (_listClass.Contains(o))
        {
            _listClass.Remove(o);
        }
    }

    public void Execute<T1>(Action<T1> execute)
    {
        for(int i = 0; i < _listClass.Count; ++i)
        {
            execute((T1)_listClass[i]);
        }
    }
}
