using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ChatView : ViewBase
{
    public Transform _layerFG;
    public Transform _layerBG;
    public Image[] _imgPic;         //一般地 0--灵梦 2,3均为右边的角色
    public Image _imgBg;

    public Text _txtdialog;
    public Text _txtChaterName;

    private Color _clrChating = new Color(1, 1, 1);
    private Color _clrUnChating = new Color(0.3f, 0.3f, 0.3f);

    private Dictionary<string, Sprite> _dicNameSprite;
    private Dictionary<int, ChatPicData> _dicPicData;
    private List<DialogData> _listDialogs;
    public int CurIndex { get; set; }
    public bool IsComplete { get; set; }
    public Tween _tween;
    private int _tid;

    public float _dialogSpeed = 0.2f;

    public override void InitAndChild()
    {
        _dicPicData = DataMgr.Single.GetChatPicData();

        _dicNameSprite = DataMgr.Single.GetNameSpriteData();
    }

    public override void Hide()
    {
        base.Hide();

        IsComplete = false;
        _tween.Kill();

        _imgBg.enabled = false;
    }

    public void ShowDialog()
    {
        HideAllPic();

        SetDialog();
    }

    private void SetDialog()
    {
        DialogData dialogData = _listDialogs[CurIndex];

        string picName = dialogData.PicName;
        if (!picName.Equals(""))                //是否为旁白，如果是就清空对话栏图片
        {
            int picType = int.Parse(picName.Split('_')[0]);
            ChatPicData picData = GetPicData(picType);
            int index = picData.Index;

            _imgPic[index].enabled = true;
            for (int i = 0; i < _imgPic.Length; ++i)
            {
                _imgPic[i].color = i == index ? _clrChating : _clrUnChating;
                _imgPic[i].rectTransform.SetParent(i == index ? _layerFG : _layerBG);
            }
            _imgPic[index].rectTransform.sizeDelta = picData.PicSize;
            _imgPic[index].sprite = _dicNameSprite[picName];
        }
        else
        {
            HideAllPic();
        }
        _txtChaterName.text = dialogData.ChaterName;

        TimeMgr.Single.RemoveTimeTask(_tid);
        _txtdialog.text = "";
        _tween = _txtdialog.DOText(dialogData.DialogTxt, dialogData.Count * _dialogSpeed).SetEase(Ease.Linear).SetAutoKill(false).OnComplete(() =>
        {
            _tid = TimeMgr.Single.AddTimeTask(() =>
            {
                ShowNextDialog();
            }, 8, TimeUnit.Second);
        });
    }

    private ChatPicData GetPicData(int character)
    {
        if (!_dicPicData.ContainsKey(character))
        {
            Debug.LogError("不存在该类型的人物，人物类型为：" + character);
            return null;
        }

        return _dicPicData[character];
    }

    public void PressZ()
    {
        ShowNextDialog();
    }

    public void SetDialogData(List<DialogData> listDialogs)
    {
        _listDialogs = listDialogs;
    }

    private void ShowNextDialog()
    {
        ++CurIndex;

        if (CurIndex < _listDialogs.Count)
        {
            SetDialog();
        }
        else
        {
            IsComplete = true;
        }
    }

    public void SetEndingPic(string spriteName)
    {
        if (!_dicNameSprite.ContainsKey(spriteName))
        {
            Debug.LogError("不存在该图片，图片名为：" + spriteName);
            return;
        }

        _imgBg.enabled = true;
        _imgBg.sprite = _dicNameSprite[spriteName];
    }

    private void HideAllPic()
    {
        for (int i = 0; i < _imgPic.Length; ++i)
        {
            _imgPic[i].enabled = false;
        }
    }
}
