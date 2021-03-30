using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyView : MonoBehaviour
{
    public ParticleSystem _deadParticle;
    public SpriteRenderer _selfSpriteRenderer;

    public void DieView()
    {
        _selfSpriteRenderer.enabled = false;
        _deadParticle.Play();

        AudioMgr.Single.PlayGameEff(AudioType.EnemyDead);
    }
}
