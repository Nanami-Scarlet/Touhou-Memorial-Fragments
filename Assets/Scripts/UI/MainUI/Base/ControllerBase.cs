using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ControllerBase : MonoBehaviour, IController
{
    private List<IControllerInit> _listInit;
    private List<IControllerShow> _listShow;
    private List<IControllerHide> _listHide;
    private List<IControllerUpdate> _listUpdate;

    public virtual void Init()
    {
        InitChild();
        InitAllInterface();
        InitSubController();
    }

    public abstract void InitChild();

    private void InitAllInterface()
    {
        _listInit = new List<IControllerInit>();
        _listShow = new List<IControllerShow>();
        _listHide = new List<IControllerHide>();
        _listUpdate = new List<IControllerUpdate>();

        InitInterface(_listInit);
        InitInterface(_listShow);
        InitInterface(_listUpdate);
        InitInterface(_listHide);
    }

    private void InitSubController()
    {
        foreach(IControllerInit init in _listInit)
        {
            init.Init();
        }
    }

    public void InitInterface<T>(List<T> list)
    {
        foreach (Transform trans in transform)
        {
            T controller = trans.GetComponent<T>();
            if(controller != null)
            {
                list.Add(controller);
            }
        }
    }

    public virtual void Show()
    {
        foreach(IControllerShow show in _listShow)
        {
            show.Show();
        }
    }

    public virtual void Hide()
    {
        foreach(IControllerHide hide in _listHide)
        {
            hide.Hide();
        }
    }

    public virtual void UpdateFun()
    {
        foreach(IControllerUpdate update in _listUpdate)
        {
            update.UpdateFun();
        }
    }
}
