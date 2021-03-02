using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeMgr : MonoSingleton<TimeMgr>, IInit, IUpdate
{
    private int _tid;
    private double _nowTime;
    private DateTime _startTime = new DateTime(2020, 1, 1, 0, 0, 0);

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
            _nowTime = GetNowTime();
            if(_nowTime - task.DestTime > 0)
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

    public int AddTimeTask(Action callback, double delay, TimeUnit unit = TimeUnit.MilliSecond)
    {
        if(unit != TimeUnit.MilliSecond)
        {
            switch (unit)       //这里用switch以后方便拓展
            {
                case TimeUnit.Second:
                    delay *= 1000;
                    break;

                default:
                    Debug.LogError("错误的时间格式...");
                    break;
            }        
        }

        int tid = GetTid();
        _nowTime = GetNowTime();
        _listTask.Add(new TimeTask(tid, _nowTime + delay, delay, callback));
        _listID.Add(tid);

        if(_listTask.Count > 0)
        {
            LifeCycleMgr.Single.Add(LifeName.UPDATE, this);
        }

        return tid;
    }

    public bool RemoveTimeTask(int tid)
    {
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
        _tid = 0;
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

    private double GetNowTime()
    {
        return (DateTime.Now - _startTime).TotalMilliseconds;
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
