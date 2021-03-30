using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeMgr : NormalSingleton<TimeMgr>, IInit, IUpdate
{
    private int _tid;

    private List<TimeTask> _listTask;
    private List<int> _listID;

    public void Init()
    {
        _listTask = new List<TimeTask>();
        _listID = new List<int>();
    }

    public void UpdateFun()
    {
        for(int i = 0; i < _listTask.Count; ++i)
        {
            TimeTask task = _listTask[i];
            if(Time.time - task.DestTime > 0)
            {
                Action cb = task.CallBack;
                if(cb != null)
                {
                    cb();
                }
                RemoveTimeTask(task.Tid);
            }
        }
    }

    public int AddTimeTask(Action callback, double delay, TimeUnit unit = TimeUnit.Second)
    {
        if(unit != TimeUnit.Second)
        {
            switch (unit)       //这里用switch以后方便拓展
            {
                case TimeUnit.MilliSecond:
                    delay /= 1000;
                    break;

                default:
                    Debug.LogError("错误的时间格式...");
                    break;
            }        
        }

        int tid = GetTid();
        _listTask.Add(new TimeTask(tid, Time.time + delay, delay, callback));
        _listID.Add(tid);

        if(_listTask.Count > 0)
        {
            LifeCycleMgr.Single.Add(LifeName.UPDATE, this);
        }

        return tid;
    }

    public bool RemoveTimeTask(int tid)
    {
        if (!_listID.Contains(tid))
        {
            return false;
        }

        for(int i = 0; i < _listTask.Count; ++i)
        {
            TimeTask task = _listTask[i];
            if(task.Tid == tid)
            {
                _listTask.Remove(task);
                _listID.Remove(tid);

                if(_listTask.Count == 0)
                {
                    LifeCycleMgr.Single.Remove(LifeName.UPDATE, this);
                }

                return true;
            }
        }
        return false;
    }

    public void ClearAllTask()
    {
        _listTask.Clear();
        _listID.Clear();
        _tid = 0;

        //LifeCycleMgr.Single.Remove(LifeName.UPDATE, this);
    }

    private int GetTid()
    {
        ++_tid;

        if(_tid == int.MaxValue)
        {
            _tid = 0;
            while (_listID.Contains(_tid))
            {
                ++_tid;
            }
        }

        return _tid;
    }
}

class TimeTask
{
    public int Tid { get; set; }
    public double DestTime { get; set; }
    public double Delay { get; set; }
    public Action CallBack { get; set; }

    public TimeTask(int tid, double destTime, double delay, Action callback)
    {
        Tid = tid;
        DestTime = destTime;
        Delay = delay;
        CallBack = callback;
    }
}
