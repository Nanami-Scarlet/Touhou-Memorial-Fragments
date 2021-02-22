using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUView : ViewBase
{
    public Transform _transRank;
    public Transform _transTitle;
    public Transform _transPic;
    public Transform _transDes;
    public Transform _transLoad;

    private int _curIndex;

    private int MAX_INDEX { get; set; }

    public override void InitChild()
    {
       
    }

    public override void Show()
    {
        ResetAnim();
        InitConfig();
        PlayAnim();
    }

    public override void UpdateFun()
    {
        int offset = _curIndex - GameStateModel.Single.PlayerOption;
        int index = Mathf.Abs(GameStateModel.Single.PlayerOption % MAX_INDEX);

        _transDes.DOBlendableLocalRotateBy(new Vector3(0, 180 * offset, 0), 0.4f);

        int nextIndex = _curIndex % MAX_INDEX;
        if(offset < 0)
        {
            nextIndex = nextIndex - 1 < 0 ? MAX_INDEX - 1 : nextIndex - 1;
        }
        else
        {
            nextIndex = nextIndex + 1 >= MAX_INDEX ? 0 : nextIndex + 1;
        }
        _transDes.GetChild(nextIndex).gameObject.SetActive(true);
        GameUtil.SetSubActive(_transDes, index);
        GameUtil.SetSubActive(_transPic, index);

        _curIndex = GameStateModel.Single.PlayerOption;
    }

    private void InitConfig()
    {
        MAX_INDEX = 2;
        int rankIndex = GameStateModel.Single.RankOption;
        GameUtil.SetSubActive(_transRank, rankIndex);
    }

    private void PlayAnim()
    {
        _transDes.localRotation = new Quaternion(0, -1, 0, 0);

        _transRank.DOScale(1, 0.5f);
        _transRank.DOLocalMove(new Vector3(0, -430, 0), 0.5f);
        _transTitle.DOLocalMoveY(370, 0.7f);
        _transPic.DOLocalMoveX(-380, 0.9f);
        _transDes.DOLocalMoveX(233, 0.9f);
        _transDes.DOLocalRotate(Vector3.zero, 0.3f).SetLoops(3, LoopType.Incremental);
    }

    private void ResetAnim()
    {
        _transPic.localPosition = new Vector3(-1000, 0, 0);
        _transDes.localPosition = new Vector3(1060, 0, 0);
        _transDes.localRotation = new Quaternion(0, -1, 0, 0);
        _transRank.localScale = Vector3.one * 1.5f;
        _transRank.localPosition = Vector3.zero;

        GameUtil.SetSubActive(_transDes, 0);
        GameUtil.SetSubActive(_transPic, 0);

        _transLoad.gameObject.SetActive(false);

        _curIndex = 0;
    }
}
