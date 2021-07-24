using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyView : EntityViewBase
{
    public ParticleSystem _deadParticle;
    public SpriteRenderer _selfSpriteRenderer;

    public virtual void DieView()
    {
        _selfSpriteRenderer.enabled = false;
        _deadParticle.Play();
    }
}
