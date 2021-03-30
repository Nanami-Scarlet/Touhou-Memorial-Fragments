using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneMgr : NormalSingleton<SceneMgr>
{
    private AsyncOperation _async;
    private Dictionary<SceneName, Action> _dicOnSceneAction = new Dictionary<SceneName, Action>();

    public SceneMgr()
    {
        SceneManager.sceneLoaded += OnSceneLoad;
    }

    public void AsyncLoadScene(SceneName name)
    {
        CoroutineMgr.Single.Execute(AsyncLoad(name.ToString()));
    }

    private IEnumerator AsyncLoad(string name)
    {
        _async = SceneManager.LoadSceneAsync(name);
        _async.allowSceneActivation = false;

        yield return _async;
    }

    public void AddSceneLoad(SceneName name, Action action)
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

    public bool IsDone()
    {
        if(_async != null && _async.progress >= 0.9f)
        {
            _async.allowSceneActivation = true;         //允许切换场景
            GameStateModel.Single.CurrentScene = GameStateModel.Single.TargetScene;
            _async = null;
        }

        return _async == null;
    }

    private void OnSceneLoad(Scene scene, LoadSceneMode mode)
    {
        SceneName name = (SceneName)Enum.Parse(typeof(SceneName), scene.name);
        if(_dicOnSceneAction.ContainsKey(name) && _dicOnSceneAction[name] != null)
        {
            _dicOnSceneAction[name]();        //加载场景时，除加载场景还会加载其他东西，加载完成任务完成
        }
    }
}
