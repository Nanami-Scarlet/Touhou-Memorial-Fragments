using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    private Animator _anim;
    private Vector3 _lastPos;
    private int _lastDir = 0;

    public void Init()
    {
        _anim = GetComponent<Animator>();
        _lastPos = transform.position;
    }

    private void Update()
    {
        _anim.SetInteger("Speed", GetOffsetX());
        _lastPos = transform.position;
    }

    private int GetOffsetX()
    {
        if(transform.position.x == _lastPos.x)
        {
            return _lastDir;
        }

        return _lastDir = transform.position.x - _lastPos.x < 0 ? -1 : 1;
    }
}
