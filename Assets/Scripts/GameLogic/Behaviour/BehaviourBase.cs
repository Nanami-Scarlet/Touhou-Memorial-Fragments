﻿using BulletPro;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BehaviourBase : MonoBehaviour
{
    private int _hp;
    private int _pCount;
    private int _pointCount;

    public int HP 
    {
        get
        {
            return _hp;
        }

        set
        {
            _hp = value;
        }
    }

    public int PCount
    {
        get
        {
            return _pCount;
        }

        set
        {
            _pCount = value;
        }
    }

    public int PointCount
    {
        get
        {
            return _pointCount;
        }

        set
        {
            _pointCount = value;
        }
    }

    public void Init(int hp = 1, int pCount = 0, int pointCount = 0)
    {
        HP = hp;
        PCount = pCount;
        PointCount = pointCount;
    }

    public virtual void Hurt(Bullet bullet, Vector3 hitPoint)
    {
        HP -= bullet.moduleParameters.GetInt("_PowerLevel");
    }

    public abstract void Dead();

    protected virtual void SpawnItems()
    {
        GetItems("P", PCount);
        GetItems("Point", PointCount);
    }

    private void GetItems(string name, int count)
    {
        for (int i = 0; i < count; ++i)
        {
            GameObject item = PoolMgr.Single.Spawn(name);
            item.GetComponent<Item>().Init(transform);
            item.transform.localPosition = GetRandom();
        }
    }

    private Vector2 GetRandom()
    {
        float x = transform.position.x;
        float y = transform.position.y;

        float newX = Random.Range(x - 0.2f, x + 0.2f);
        float newY = Random.Range(y - 0.1f, y + 0.1f);

        return new Vector2(newX, newY);
    }
}
