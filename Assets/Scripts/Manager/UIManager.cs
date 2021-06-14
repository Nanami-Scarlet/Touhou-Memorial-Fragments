using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class UIManager : NormalSingleton<UIManager>
{
    private Canvas _canvas;

    private Dictionary<string, GameObject> _dicNameOfView;
    private Dictionary<string, int> _dicViewData;
    private Stack<GameObject> _stackView;

    private Transform[] _layers = new Transform[3];

    public UIManager()
    {
        _canvas = Object.FindObjectOfType<Canvas>();
        for (int i = 0; i < 3; ++i)
        {
            _layers[i] = _canvas.transform.GetChild(i);
        }

        _dicNameOfView = new Dictionary<string, GameObject>();
        _dicViewData = DataMgr.Single.GetViewData();
        _stackView = new Stack<GameObject>();
    }

    public void Show(string path, bool isShow = true)
    {
        if (!_dicNameOfView.ContainsKey(path))
        {
            string[] temp = path.Split('/');
            string name = temp[temp.Length - 1];
            if (!name.Contains("View"))
            {
                Debug.LogError("传入了错误的UI地址，地址为：" + path);
                return;
            }

            int layer = _dicViewData[name];
            GameObject viewGo = LoadMgr.Single.LoadPrefabAndInstantiate(path, _layers[layer]);
            _dicNameOfView.Add(path, viewGo);
            InitComponent(viewGo);
        }
        GameObject go = _dicNameOfView[path];

        //go.SetActive(isShow);
        _stackView.Push(go);
        ShowAll(go);
        go.SetActive(isShow);
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

    public void SimpleShow(string path)         //为了解决Chat面板和Pause面板的冲突
    {
        if(_dicNameOfView.ContainsKey(path))
        {
            _dicNameOfView[path].SetActive(true);
            ShowController(path);
        }
    }

    public void SimpleHide(string path)
    {
        if (_dicNameOfView.ContainsKey(path))
        {
            _dicNameOfView[path].SetActive(false);
            HideController(path);
        }
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
