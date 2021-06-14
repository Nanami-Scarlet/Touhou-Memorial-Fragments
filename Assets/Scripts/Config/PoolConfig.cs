using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolConfig
{
    public List<PoolData> ListData;

    public PoolConfig()
    {
        ListData = new List<PoolData>()
        {
            new PoolData
            {
                 Path = Paths.PREFAB_ENEMY1,
                 Count = 30
            },

            new PoolData
            {
                 Path = Paths.PREFAB_ENEMY2,
                 Count = 30
            },

            new PoolData
            {
                 Path = Paths.PREFAB_ENEMY3,
                 Count = 30
            },

            new PoolData
            {
                 Path = Paths.PREFAB_ENEMY4,
                 Count = 30
            },

            new PoolData
            {
                 Path = Paths.PREFAB_ENEMY5,
                 Count = 30
            },

            new PoolData
            {
                 Path = Paths.PREFAB_ENEMY6,
                 Count = 30
            },

            new PoolData
            {
                 Path = Paths.PREFAB_ENEMY7,
                 Count = 30
            },

            new PoolData
            {
                 Path = Paths.PREFAB_ENEMY8,
                 Count = 30
            },

            new PoolData
            {
                 Path = Paths.PREFAB_ENEMY9,
                 Count = 15
            },

            new PoolData
            {
                 Path = Paths.PREFAB_ENEMY10,
                 Count = 30
            },

            new PoolData
            {
                 Path = Paths.PREFAB_ENEMY11,
                 Count = 30
            },

            new PoolData
            {
                 Path = Paths.PREFAB_BOSS1,
                 Count = 1
            },

            new PoolData
            {
                 Path = Paths.PREFAB_BOSS2,
                 Count = 1
            },

            new PoolData
            {
                 Path = Paths.PREFAB_BOSS3,
                 Count = 1
            },

            new PoolData
            {
                 Path = Paths.PREFAB_BOSS4,
                 Count = 1
            },

            new PoolData
            {
                 Path = Paths.PREFAB_BOSS5,
                 Count = 4
            },

            new PoolData
            {
                Path = Paths.PREFAB_ITEM_P,
                Count = 200
            },

            new PoolData
            {
                Path = Paths.PREFAB_ITEM_POINT,
                Count = 200
            },

            new PoolData
            {
                Path = Paths.PREFAB_ITEM_LIFE_FRAGMENT,
                Count = 3
            },

            new PoolData
            {
                Path = Paths.PREFAB_ITEM_BOMB_FRAGMENT,
                Count = 3
            }
        };
    }
}

public class PoolData
{
    public string Path { get; set; }
    public int Count { get; set; }
}
