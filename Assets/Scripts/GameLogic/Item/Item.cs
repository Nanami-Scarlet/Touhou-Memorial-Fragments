using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    private GameObject _player;

    private float _shootHeight;
    private bool _isFall = false;

    private float _shootSpeed = 2f;
    private float _speed = 3.5f;

    private bool _isMoveToPlayer = false;           //以免出现掉落物移动到玩家然后不移动了

    public void InitFall(Transform bornTrans)
    {
        _player = GameObject.FindGameObjectWithTag("Player");

        transform.localPosition = bornTrans.localPosition;
        Vector3 pos = transform.localPosition;
        _shootHeight = Random.Range(pos.y, pos.y + 3f);

        transform.DOLocalMoveY(_shootHeight, 1 / _shootSpeed).OnComplete(() =>
        {
            transform.localRotation = new Quaternion(0, 0, 0, 1);
            _isFall = true;
        });
    }

    public void Summon(Vector3 bornPos)
    {
        _player = GameObject.FindGameObjectWithTag("Player");
        transform.localPosition = bornPos;
        _isFall = true;
    }

    private void Update()
    {
        if(!_isMoveToPlayer && PlayerModel.Single.IsGetItem)
        {
            _isMoveToPlayer = true;
        }

        if (_isFall)
        {
            transform.Translate(Vector2.down * Time.deltaTime * _speed);
        }
        else
        {
            transform.Rotate(Vector3.forward * Time.deltaTime * 1800);
        }

        if(_isMoveToPlayer)
        {
            transform.position = Vector3.MoveTowards(transform.position, _player.transform.position, Time.deltaTime * 12f);
        }
        else if(GameUtil.GetDistance(transform, _player.transform) < 0.75f)
        {
            transform.position = Vector3.MoveTowards(transform.position, _player.transform.position, Time.deltaTime * 3f);
        }

        if(transform.position.y < -5f)
        {
            PoolMgr.Single.Despawn(gameObject);
        }
    }

    public void ResetItem()
    {
        _isFall = false;
        _isMoveToPlayer = false;
    }
}
