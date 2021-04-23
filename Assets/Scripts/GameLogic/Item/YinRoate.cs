using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class YinRoate : MonoBehaviour
{
    private float _rotateSpeed = 180f;

    private void Update()
    {
        transform.Rotate(Vector3.forward * _rotateSpeed * Time.deltaTime);
    }
}
