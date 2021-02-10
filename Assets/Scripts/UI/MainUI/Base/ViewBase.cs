using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ViewBase : MonoBehaviour, IView
{
    private UIUtil _util = null;

    private List<IViewInit> _listInit;
    private List<IViewShow> _listShow;
    private List<IViewHide> _listHide;
    private List<IViewUpdate> _listUpdate;

    protected UIUtil Util
    {
        get
        {
            if(_util == null)
            {
                _util = gameObject.AddComponent<UIUtil>();
                _util.Init();
            }

            return _util;
        }
    }

    public void Init()
    {
        InitChild();
        InitAllInterface();
        InitSubView();
    }

    public abstract void InitChild();

    private void InitAllInterface()
    {
        _listInit = new List<IViewInit>();
        _listShow = new List<IViewShow>();
        _listHide = new List<IViewHide>();
        _listUpdate = new List<IViewUpdate>();

        InitInterface(_listInit);
        InitInterface(_listShow);
        InitInterface(_listUpdate);
        InitInterface(_listHide);
    }

    private void InitSubView()
    {
        foreach(IViewInit view in _listInit)
        {
            view.Init();
        }
    }

    private void InitInterface<T>(List<T> list)
    {
        foreach (Transform trans in transform)
        {
            T view = trans.GetComponent<T>();
            if(view != null)
            {
                list.Add(view);
            }
        }
    }

    public virtual void Show()
    {
        gameObject.SetActive(true);

        foreach(IViewShow view in _listShow)
        {
            view.Show();
        }
    }

    public virtual void Hide()
    {
        foreach(IViewHide hide in _listHide)
        {
            hide.Hide();
        }

        gameObject.SetActive(false);
    }

    public virtual void UpdateFun()
    {
        foreach(IViewUpdate update in _listUpdate)
        {
            update.UpdateFun();
        }
    }

    public Transform GetTrans()
    {
        return transform;
    }
}
