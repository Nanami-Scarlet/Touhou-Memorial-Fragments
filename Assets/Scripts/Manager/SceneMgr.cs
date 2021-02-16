using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneMgr : MonoSingleton<SceneMgr>
{
    private AsyncOperation _async;
    private Dictionary<SceneName, int> _dicSceneTask = new Dictionary<SceneName, int>();
    private Dictionary<SceneName, Action<Action>> _dicOnSceneAction = new Dictionary<SceneName, Action<Action>>();

    private int _finishedTask = 0;
    private int _totalTask = 0;

    public SceneMgr()
    {
        SceneManager.sceneLoaded += OnSceneLoad;
        for(SceneName i = SceneName.Main; i < SceneName.COUNT; ++i)
        {
            _dicSceneTask[i] = 1;
        }
    }

    public void AsyncLoadScene(SceneName name)
    {
        _totalTask = _dicSceneTask[name];               //绑定任务数
        StartCoroutine(AsyncLoad(name.ToString()));
    }

    private IEnumerator AsyncLoad(string name)
    {
        _async = SceneManager.LoadSceneAsync(name);
        _async.allowSceneActivation = false;

        yield return _async;
    }

    public void AddSceneLoad(SceneName name, Action<Action> action)
    {
        if (_dicOnSceneAction.ContainsKey(name))
        {
            _dicOnSceneAction[name] += action;
        }
        else
        {
            _dicOnSceneAction[name] = action;
        }
    }

    public float Process()
    {
        float ratio = (float)_finishedTask / _totalTask;
        if(_async != null && _async.progress >= 0.9f)                     //场景本身加载完成，完成任务数+1，且其他东西也加载完成
        {
            FinishTask();
            _async.allowSceneActivation = true;         //允许切换场景
            GameStateModel.Single.CurrentScene = GameStateModel.Single.TargetScene;
            _async = null;
        }

        return ratio;
    }

    private void OnSceneLoad(Scene scene, LoadSceneMode mode)
    {
        SceneName name = (SceneName)Enum.Parse(typeof(SceneName), scene.name);
        if(_dicOnSceneAction.ContainsKey(name) && _dicOnSceneAction[name] != null)
        {
            _dicOnSceneAction[name](FinishTask);        //加载场景时，除加载场景还会加载其他东西，加载完成任务完成
        }
    }

    private void FinishTask()
    {
        ++_finishedTask;
    }
}
