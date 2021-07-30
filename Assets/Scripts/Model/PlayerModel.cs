using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerModel : NormalSingleton<PlayerModel>
{
    public int Life { get; set; }

    public int LifeFragment { get; set; }

    public int Bomb { get; set; }

    public int BombFragment { get; set; }

    public int Mana { get; set; }

    public int Graze { get; set; }

    public int MAX_GET_POINT { get; set; }

    public int MemoryFragment { get; set; }

    public int MemoryProcess { get; set; }

    public int GodProcess { get; set; }

    public bool IsGetItem { get; set; }

    public PlayerState State { get; set; }
}
