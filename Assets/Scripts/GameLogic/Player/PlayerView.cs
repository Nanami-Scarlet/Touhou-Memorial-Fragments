using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerView : MonoBehaviour, IUpdate
{
    public Transform _transCentre;
    public float _roateSpeed = 60f;

    private Transform _transPoint;
    private Transform _transSnowLeft;
    private Transform _transSnowRight;

    public bool ISInch { get; set; }

    public void Init()
    {
        _transPoint = _transCentre.GetChild(0);
        _transSnowLeft = _transCentre.GetChild(1);
        _transSnowRight = _transCentre.GetChild(2);

        transform.DOMoveY(-3.5f, 1);
        //transform.position = new Vector3(transform.position.x, -3.5f, transform.position.z);
    }

    public void UpdateFun()
    {
        if (ISInch)
        {
            //_transPoint.DOScale(Vector3.one, 0.5f);
            _transPoint.localScale = Vector3.one;
            _transSnowLeft.gameObject.SetActive(true);
            _transSnowRight.gameObject.SetActive(true);
            _transSnowLeft.Rotate(new Vector3(0, 0, _roateSpeed * Time.deltaTime));
            _transSnowRight.Rotate(new Vector3(0, 0, -_roateSpeed * Time.deltaTime));
        }
        else
        {
            //_transPoint.DOScale(Vector3.zero, 0.5f);
            _transPoint.localScale = Vector3.zero;
            _transSnowLeft.gameObject.SetActive(false);
            _transSnowRight.gameObject.SetActive(false);
        }
    }
}
