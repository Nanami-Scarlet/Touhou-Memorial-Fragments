using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OptionController : ControllerBase
{
    public override void InitChild()
    {
        foreach (Transform trans in transform)
        {
            trans.Add<OptionItemController>();
        }
    }
}
