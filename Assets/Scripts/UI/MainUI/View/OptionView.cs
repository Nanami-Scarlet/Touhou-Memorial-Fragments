using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OptionView : ViewBase
{
    public override void InitChild()
    {
        foreach(Transform trans in transform)
        {
            trans.Add<OptionItemView>();
        }
    }
}
