using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class UIManager : NormalSingleton<UIManager>
{
    private Canvas _canvas;

    private Dictionary<string, IView> _dicNameOfView;
    private Stack<IView> _stackView;

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
        _dicNameOfView = new Dictionary<string, IView>();
        _stackView = new Stack<IView>();
    }

    public void Show(string path)
    {
        if(_stackView.Count > 0)
        {
            _stackView.Peek().Hide();
        }

        IView view = GetView(path);
        view.Show();
        
        _stackView.Push(view);
    }

    public void Hide()
    {
        IView view = _stackView.Peek();

        view.Hide();
    }

    public void Back()
    {
        IView viewTop = _stackView.Pop();
        IView viewBottom = _stackView.Pop();

        _stackView.Push(viewTop);
        _stackView.Push(viewBottom);
        viewBottom.Show();
    }

    private IView GetView(string path)
    {
        if (_dicNameOfView.ContainsKey(path))
        {
            return _dicNameOfView[path]; 
        }

        return InitView(path);
    }

    private IView InitView(string path)
    {
        GameObject viewGo = LoadMgr.Single.LoadPrefabAndInstantiate(path, UICanvas.transform);

        AddTypeComponet(viewGo, path);

        InitComponent(viewGo);

        IView view = viewGo.GetComponent<IView>();

        _dicNameOfView.Add(path, view);

        return view;
    }

    private void AddTypeComponet(GameObject viewGo, string path)
    {
        foreach(Type type in BindUtil.GetType(path))
        {
            viewGo.AddComponent(type);
        }
    }

    private void InitComponent(GameObject go)
    {
        IInit[] inits = go.GetComponents<IInit>();
        foreach (IInit init in inits)
        {
            init.Init();
        }
    }
}
