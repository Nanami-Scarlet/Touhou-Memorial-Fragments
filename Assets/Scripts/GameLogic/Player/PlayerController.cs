using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public PlayerView _view;
    public float _inchSpeed = 1.5f;
    public float _normalSpeed = 6f;

    private MoveComponet _move;
    private Animator _anim;

    public void Init()
    {
        _move = GetComponent<MoveComponet>();
        _anim = GetComponent<Animator>();

        InputMgr.Single.AddGameListener(KeyCode.UpArrow);
        InputMgr.Single.AddGameListener(KeyCode.DownArrow);
        InputMgr.Single.AddGameListener(KeyCode.RightArrow);
        InputMgr.Single.AddGameListener(KeyCode.LeftArrow);
        InputMgr.Single.AddGameListener(KeyCode.LeftShift);

        MessageMgr.Single.AddListener(KeyCode.UpArrow, MoveUp);
        MessageMgr.Single.AddListener(KeyCode.DownArrow, MoveDown);
        MessageMgr.Single.AddListener(KeyCode.RightArrow, MoveRight);
        MessageMgr.Single.AddListener(KeyCode.LeftArrow, MoveLeft);
        MessageMgr.Single.AddListener(KeyCode.LeftShift, Inch);

        InputMgr.Single.AddUpdateListener(OnDirKeyUp);
        InputMgr.Single.AddUpdateListener(OnShiftUp);
    }

    private void OnDestroy()
    {
        InputMgr.Single.RemoveGameListener(KeyCode.UpArrow);
        InputMgr.Single.RemoveGameListener(KeyCode.DownArrow);
        InputMgr.Single.RemoveGameListener(KeyCode.RightArrow);
        InputMgr.Single.RemoveGameListener(KeyCode.LeftArrow);
        InputMgr.Single.RemoveGameListener(KeyCode.LeftShift);

        InputMgr.Single.RemvoeUpdateListener(OnDirKeyUp);
        InputMgr.Single.RemvoeUpdateListener(OnShiftUp);

        MessageMgr.Single.RemoveListener(KeyCode.UpArrow, MoveUp);
        MessageMgr.Single.RemoveListener(KeyCode.DownArrow, MoveDown);
        MessageMgr.Single.RemoveListener(KeyCode.RightArrow, MoveRight);
        MessageMgr.Single.RemoveListener(KeyCode.LeftArrow, MoveLeft);
        MessageMgr.Single.RemoveListener(KeyCode.LeftShift, Inch);

        //InputMgr.Single.RemvoeUpdateListener(OnDirKeyUp);
        //InputMgr.Single.RemvoeUpdateListener(OnShiftUp);
    }

    private void MoveUp(object[] args)
    {
        if (GameUtil.JudgeBorderUp(transform.position))
        {
            _move.Move(Vector3.up);
        }
    }

    private void MoveDown(object[] args)
    {
        if (GameUtil.JudgeBorderDown(transform.position))
        {
            _move.Move(Vector3.down);
        }
    }

    private void MoveRight(object[] args)
    {
        if (GameUtil.JudgeBorderRight(transform.position))
        {
            _anim.SetInteger("Speed", 1);
            _move.Move(Vector3.right);
        }
    }

    private void MoveLeft(object[] args)
    {
        if (GameUtil.JudgeBorderLeft(transform.position))
        {
            _anim.SetInteger("Speed", -1);
            _move.Move(Vector3.left);
        }
    }

    private void Inch(object[] args)
    {
        _view.ISInch = true;
        _move.Speed = _inchSpeed;
        _view.UpdateFun();
    }

    private void OnDirKeyUp()
    {
        _anim.SetInteger("Speed", 0);
    }

    private void OnShiftUp()
    {
        _view.ISInch = false;
        _move.Speed = _normalSpeed;
        _view.UpdateFun();
    }
}
