using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameStateModel : NormalSingleton<GameStateModel>
{
    private bool _isChating = false;
    private bool _isPause = false;

    public int SelectedOption { get; set; }
    public int RankOption { get; set; }
    public int PlayerOption { get; set; }
    public int PauseOption { get; set; }
    public int ManualOPation { get; set; }
    public bool IsChating 
    {
        get
        {
            return _isChating;
        }

        set
        {
            InputMgr.Single.UpdateKeyState();
            _isChating = value;
        }
    }
    public bool IsPause 
    {
        get
        {
            return _isPause;
        }

        set
        {
            InputMgr.Single.UpdateKeyState();
            _isPause = value;
        }
    }
    public Degree GameDegree { get; set; }      //游戏难度
    public SceneName CurrentScene { get; set; }
    public SceneName TargetScene { get; set; }
    public Mode GameMode { get; set; }
    public bool IsCard { get; set; }            //当前是否是符卡状态
}
