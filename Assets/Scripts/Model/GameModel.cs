using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameModel : NormalSingleton<GameModel>
{
    public int WaveCount { get; set; }

    public int EnemyCount { get; set; }
}
