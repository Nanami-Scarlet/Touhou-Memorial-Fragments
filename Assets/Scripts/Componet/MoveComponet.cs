using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveComponet : MonoBehaviour
{
    private float _speed;

    public float Speed 
    {
        get
        {
            return _speed;
        }

        set
        {
            _speed = value;
        }
    }

    public void Move(Vector3 direction)
    {
        if (_speed != 0)
        {
            transform.Translate(direction.normalized * _speed * Time.deltaTime, Space.World);
        }
    }
}
