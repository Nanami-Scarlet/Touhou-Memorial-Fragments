using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameStateModel : NormalSingleton<GameStateModel>
{
    public int SelectedOption { get; set; }
    public int RankOption { get; set; }
    public int PlayerOption { get; set; }
    public int PauseOption { get; set; }
    public Degree SelectedDegree { get; set; }
    public SceneName CurrentScene { get; set; }
    public SceneName TargetScene { get; set; }
    public GameStatus Status { get; set; }
}
