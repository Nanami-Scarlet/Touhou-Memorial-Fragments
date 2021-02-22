using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class UIManager : NormalSingleton<UIManager>
{
    private Canvas _canvas;

    private Dictionary<string, GameObject> _dicNameOfView;
    private Stack<GameObject> _stackView;

    public Canvas UICanvas
    {
        get
        {
            if(_canvas == null)
            {
                _canvas = Object.FindObjectOfType<Canvas>();
            }

            if(_canvas == null)
            {
                Debug.LogError("场景中没有Canvas");
            }

            return _canvas;
        }
    }

    public UIManager()
    {
        _dicNameOfView = new Dictionary<string, GameObject>();
        _stackView = new Stack<GameObject>();
    }

    public void Show(string path, bool hideTop = true)
    {
        if(_stackView.Count > 0 && hideTop)             //一个界面同时会存在多个UI
        {
            HideAll(_stackView.Peek());
        }

        if (!_dicNameOfView.ContainsKey(path))
        {
            GameObject viewGo = LoadMgr.Single.LoadPrefabAndInstantiate(path, UICanvas.transform);
            _dicNameOfView.Add(path, viewGo);
            InitComponent(viewGo);
        }
        GameObject go = _dicNameOfView[path];

        go.SetActive(true);
        _stackView.Push(go);
        ShowAll(go);
    }

    public void Hide(string path)
    {
        GameObject go = _dicNameOfView[path];
        HideAll(go);
    }

    public void Back()
    {
        GameObject goTop = _stackView.Pop();
        GameObject goBottom = _stackView.Pop();

        _stackView.Push(goTop);
        _stackView.Push(goBottom);

        goTop.SetActive(false);
        goBottom.SetActive(true);

        HideAll(goTop);
        InitComponent(goBottom);
    }

    public GameObject PreLoad(string path)
    {
        if (_dicNameOfView.ContainsKey(path))
        {
            //Debug.LogError("该路径的预制体已经存在，路径为：" + path);
            //return null;

            return _dicNameOfView[path];
        }

        GameObject preGo = LoadMgr.Single.LoadPrefabAndInstantiate(path, UICanvas.transform);
        InitComponent(preGo);
        preGo.SetActive(false);
        _dicNameOfView.Add(path, preGo);

        return preGo;
    }

    public void ShowController(string path)         //该方法在多个UI同时存在下使用
    {
        if (!_dicNameOfView.ContainsKey(path))
        {
            Debug.LogError("该路径的预制件未被加载，路径为："  + path);
            return;
        }

        GameObject go = _dicNameOfView[path];
        IControllerShow show = go.GetComponent<IControllerShow>();
        show.Show();
    }

    public void HideController(string path)         //上面的配套方法
    {
        if (!_dicNameOfView.ContainsKey(path))
        {
            Debug.LogError("该路径的预制件未被加载，路径为：" + path);
            return;
        }

        GameObject go = _dicNameOfView[path];
        IControllerHide hide = go.GetComponent<IControllerHide>();
        hide.Hide();
    }

    private void InitComponent(GameObject go)
    {
        IInit[] inits = go.GetComponents<IInit>();

        foreach(IInit init in inits)
        {
            init.Init();
        }
    }

    private void ShowAll(GameObject go)
    {
        IShow[] shows = go.GetComponents<IShow>();

        foreach(IShow show in shows)
        {
            show.Show();
        }
    }

    private void HideAll(GameObject go)
    {
        IHide[] hides = go.GetComponents<IHide>();

        foreach(IHide hide in hides)
        {
            hide.Hide();
        }
    }
}
