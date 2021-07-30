using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerView : MonoBehaviour
{
    public Transform _transCenter;
    public float _roateSpeed = 60f;
    public Transform _transCenterShot;

    private Transform _transPoint;
    private Transform _transSnowLeft;
    private Transform _transSnowRight;

    public List<Transform> _listTransYin;
    private Dictionary<int, List<YinMove>> _dicIndexYinMove;

    public ParticleSystem _grazeEffect;

    public void Init()
    {
        _transPoint = _transCenter.GetChild(0);
        _transSnowLeft = _transCenter.GetChild(1);
        _transSnowRight = _transCenter.GetChild(2);

        _dicIndexYinMove = new Dictionary<int, List<YinMove>>();

        for (int i = 0; i < 4; ++i)
        {
            _dicIndexYinMove[i] = new List<YinMove>();

            for(int j = 0; j < i + 1; ++j)
            {
                YinMove yin = _listTransYin[i].GetChild(j).GetComponent<YinMove>();
                yin.Init();
                _dicIndexYinMove[i].Add(yin);
            }
        }

        transform.DOMoveY(Const.BORN_POS.y, 1);

        InputMgr.Single.AddGameListener(KeyCode.LeftShift);

        MessageMgr.Single.AddListener(KeyCode.LeftShift, OnShift, InputState.PRESS);
        MessageMgr.Single.AddListener(KeyCode.LeftShift, OnNormal, InputState.UP);
        MessageMgr.Single.AddListener(MsgEvent.EVENT_CHECK_MANA, CheckMana);

    }

    private void OnDestroy()
    {
        transform.DOKill();

        InputMgr.Single.RemoveGameListener(KeyCode.LeftShift);

        MessageMgr.Single.RemoveListener(KeyCode.LeftShift, OnShift, InputState.PRESS);
        MessageMgr.Single.RemoveListener(KeyCode.LeftShift, OnNormal, InputState.UP);
        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_CHECK_MANA, CheckMana);
    }

    private void OnShift(object[] args)
    {
        _transPoint.DOScale(Vector3.one, 0.5f);
        _transSnowLeft.gameObject.SetActive(true);
        _transSnowRight.gameObject.SetActive(true);
        _transSnowLeft.Rotate(new Vector3(0, 0, _roateSpeed * Time.deltaTime));
        _transSnowRight.Rotate(new Vector3(0, 0, -_roateSpeed * Time.deltaTime));

        int index = GameUtil.GetManaLevel();
        List<YinMove> yins = _dicIndexYinMove[index];
        foreach(var yin in yins)
        {
            yin.OnShift();
        }
    }

    private void OnNormal(object[] args)
    {
        _transPoint.DOScale(Vector3.zero, 0.5f);
        _transSnowLeft.gameObject.SetActive(false);
        _transSnowRight.gameObject.SetActive(false);

        int index = GameUtil.GetManaLevel();
        List<YinMove> yins = _dicIndexYinMove[index];
        foreach (var yin in yins)
        {
            yin.OnNormal();
        }
    }

    private void CheckMana(object[] args)
    {
        int index = GameUtil.GetManaLevel();

        GameUtil.SetSubActive(_listTransYin, index);
    }

    public void PlayGrazeEffect()
    {
        _grazeEffect.Play();
    }
}
