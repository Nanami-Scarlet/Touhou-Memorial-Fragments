using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class YinRoate : MonoBehaviour, IUpdate
{
    private float _rotateSpeed = 180f;

    private void OnEnable()
    {
        LifeCycleMgr.Single.Add(LifeName.UPDATE, this);
    }

    public void UpdateFun()
    {
        transform.Rotate(Vector3.forward * _rotateSpeed * Time.deltaTime);
    }

    private void OnDisable()
    {
        RemoveUpdate();
    }

    public void RemoveUpdate()
    {
        LifeCycleMgr.Single.Remove(LifeName.UPDATE, this);
    }
}
