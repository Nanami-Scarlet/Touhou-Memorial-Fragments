using BulletPro;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EntityBehaviourBase : MonoBehaviour
{
    private int _hp;
    private int _pCount;
    private int _pointCount;
    private int _lifeFragmentCount;
    private int _bombFragmentCount;

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

    public int LifeFragmentCount
    {
        get
        {
            return _lifeFragmentCount;
        }

        set
        {
            _lifeFragmentCount = value;
        }
    }

    public int BombFragmentCount
    {
        get
        {
            return _bombFragmentCount;
        }

        set
        {
            _bombFragmentCount = value;
        }
    }

    public void SetBehaviour(int hp = 1, int pCount = 0, int pointCount = 0, 
        int lifeFragmentCount = 0, int bombFragmentCount = 0)
    {
        HP = hp;
        PCount = pCount;
        PointCount = pointCount;
        LifeFragmentCount = lifeFragmentCount;
        BombFragmentCount = bombFragmentCount;
    }

    public virtual void Hurt(Bullet bullet, Vector3 hitPoint)
    {
        HP -= bullet.moduleParameters.GetInt("_PowerLevel");

        GameModel.Single.Score += Const.BULLET_SCORE;
        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_UPDATE_SCORE, GameModel.Single.Score);
    }

    public abstract void Dead();

    public virtual void SpawnItems()
    {
        GetItems("P", PCount);
        GetItems("Point", PointCount);
        GetItems("LifeFragment", LifeFragmentCount);
        GetItems("BombFragment", BombFragmentCount);
    }

    private void GetItems(string name, int count)
    {
        for (int i = 0; i < count; ++i)
        {
            GameObject item = PoolMgr.Single.Spawn(name);
            item.GetComponent<Item>().InitFall(transform);
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
