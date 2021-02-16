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

    public void Show(string path)
    {
        if(_stackView.Count > 0)
        {
            HideAll(_stackView.Peek());
        }

        if (!_dicNameOfView.ContainsKey(path))
        {
            GameObject viewGo = LoadMgr.Single.LoadPrefabAndInstantiate(path, UICanvas.transform);
            _dicNameOfView.Add(path, viewGo);
        }
        GameObject go = _dicNameOfView[path];

        go.SetActive(true);
        _stackView.Push(go);
        InitComponent(go);
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

    private void InitComponent(GameObject go)
    {
        IInit[] inits = go.GetComponents<IInit>();

        foreach(IInit init in inits)
        {
            init.Init();
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
