using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    private GameObject _player;

    private float _shootHeight;
    public bool _isFall = false;

    private float _shootSpeed = 2f;
    private float _speed = 3.5f;

    public bool _isMoveToPlayer = false;           //以免出现掉落物移动到玩家然后不移动了

    public void InitFall(Transform bornTrans)
    {
        _player = GameObject.FindGameObjectWithTag("Player");

        transform.localPosition = bornTrans.localPosition;
        Vector3 pos = transform.localPosition;
        _shootHeight = Random.Range(pos.y, pos.y + 3f);

        transform.DOLocalMoveY(_shootHeight, 1 / _shootSpeed).OnComplete(() =>
        {
            _isFall = true;

            transform.localEulerAngles = Vector3.zero;
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
        if (PlayerModel.Single.IsGetItem)
        {
            _isMoveToPlayer = true;
            _isFall = false;
        }

        if (_isMoveToPlayer)
        {
            transform.position = Vector3.MoveTowards(transform.position, _player.transform.position, Time.deltaTime * 10f);
        }
        else if (GameUtil.GetDistance(transform, _player.transform) < 0.75f)
        {
            transform.position = Vector3.MoveTowards(transform.position, _player.transform.position, Time.deltaTime * 3f);
        }

        if (_isFall)
        {
            transform.Translate(Vector2.down * Time.deltaTime * _speed);
        }
        else
        {
            if (!PlayerModel.Single.IsGetItem)
            {
                transform.Rotate(Vector3.forward * Time.deltaTime * 1800);
            }
        }

        if (transform.position.y < -5f)
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
