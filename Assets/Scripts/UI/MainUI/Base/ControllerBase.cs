using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ControllerBase : MonoBehaviour, IController
{
    public virtual void Init()
    {
        InitChild();
    }

    public abstract void InitChild();

    public virtual void Show()
    {
        
    }

    public virtual void Hide()
    {
        
    }

    public virtual void UpdateFun()
    {
        
    }
}
