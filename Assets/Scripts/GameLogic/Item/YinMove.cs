using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class YinMove : MonoBehaviour
{
    public Transform _transShift;

    private Vector3 _vecNormal;

    public void Init()
    {
        _vecNormal = transform.localPosition;
    }

    public void OnShift()
    {
        transform.DOLocalMove(_transShift.localPosition, 0.3f);
    }

    private void OnDestroy()
    {
        transform.DOKill();
    }

    public void OnNormal()
    {
        transform.DOLocalMove(_vecNormal, 0.3f);
    }
}
