using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class DynamicView : ViewBase
{
    public Text _textBGMInfo;

    public override void InitChild()
    {
        
    }

    public override void Show()
    {
        base.Show();

        ResetAnim();

        MessageMgr.Single.AddListener(MsgEvent.EVENT_BGM_SETTING, BGMSetting);
    }

    public override void Hide()
    {
        base.Hide();

        MessageMgr.Single.RemoveListener(MsgEvent.EVENT_BGM_SETTING, BGMSetting);
    }

    public void BGMSetting(object[] args)
    {
        string stage = (string)args[0];

        AudioData data = DataMgr.Single.GetBGMData(stage);
        AudioMgr.Single.PlayBGM(stage + "_BGM", data.Volume);
        _textBGMInfo.text = "BGM:" + data.Name;

        _textBGMInfo.transform.DOLocalMoveX(-352, 1).OnComplete(() => 
        {
            TimeMgr.Single.AddTimeTask(() => 
            {
                _textBGMInfo.DOFade(0, 0.5f).OnComplete(() => 
                {
                    ResetAnim();
                });
            }, 3f, TimeUnit.Second);
        });
    }

    private void ResetAnim()
    {
        _textBGMInfo.color = new Color(1, 1, 1, 1);
        _textBGMInfo.transform.localPosition = Vector3.right * 132;
    }
}
