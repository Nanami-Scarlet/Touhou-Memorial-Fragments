using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[BindAttribute(Paths.PREFAB_START_VIEW, Const.BIND_PREFAB_PRIORITY_CONTROLLER)]
public class StartController : ControllerBase
{
    public override void InitChild()
    {
        transform.Find("Option").Add<OptionController>();
    }
}
