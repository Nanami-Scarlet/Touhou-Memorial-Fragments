using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[BindAttribute(Paths.PREFAB_START_VIEW, Const.BIND_PREFAB_PRIORITY_VIEW)]
public class StartView : ViewBase
{
    public override void InitChild()
    {
        Util.Get("Option").Add<OptionView>();
    }
}
