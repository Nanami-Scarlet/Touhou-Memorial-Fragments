using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityControllerBase : MonoBehaviour
{
    protected Animator _anim;
    protected Vector3 _lastPos;

    public virtual void Init(EntityData data)
    {
        _anim = GetComponent<Animator>();
    }

    public virtual void Update()
    {
        if (!GameStateModel.Single.IsPause)
        {
            _anim.SetInteger("Speed", GetOffsetX());
            _lastPos = transform.position;
        }
    }

    private int GetOffsetX()
    {
        if (transform.position.x == _lastPos.x)
        {
            return 0;
        }

        return transform.position.x - _lastPos.x < 0 ? -1 : 1;
    }
}
