using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ViewBase : MonoBehaviour, IView
{
    private UIUtil _util;

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
        InitAndChild();
    }

    public abstract void InitAndChild();

    public virtual void Show()
    {
        gameObject.SetActive(true);
    }

    public virtual void Hide()
    {
        gameObject.SetActive(false);
    }

    public virtual void UpdateFun()
    {
        
    }
}
