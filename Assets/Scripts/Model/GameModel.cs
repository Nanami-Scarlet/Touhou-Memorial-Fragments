using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameModel : NormalSingleton<GameModel>
{
    public int StageNum { get; set; }

    public int EnemyCount { get; set; }     //计时器加了任务之后延时一帧执行，于是用这个同步更新妖精的数量
}
