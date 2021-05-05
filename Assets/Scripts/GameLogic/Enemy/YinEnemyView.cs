using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class YinEnemyView : EnemyView
{
    public SpriteRenderer _ringSpriteRenderer;

    public override void DieView()
    {
        base.DieView();

        _ringSpriteRenderer.enabled = false;
    }
}
