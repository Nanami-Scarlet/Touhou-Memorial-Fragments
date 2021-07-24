using BulletPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wall : MonoBehaviour
{
    public BounceChannel _channel = BounceChannel.Horizontal;
    private float _bounceCoolDown = 0.2f;
    public bool _isBottom = false;

    public void BounceBullet(Bullet bullet, Vector3 hitPoint)
    {
        if (!bullet.IsBounce && !_isBottom)
        {
            bullet.moduleMovement.Bounce(transform.up, _bounceCoolDown, _channel);
            bullet.IsBounce = true;
        }
    }
}
