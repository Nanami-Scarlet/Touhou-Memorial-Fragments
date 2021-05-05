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
                 Count = 15
            },

             new PoolData
            {
                 Path = Paths.PREFAB_ENEMY11,
                 Count = 15
            },

            //todo:添加其他的Data

            new PoolData
            {
                Path = Paths.PREFAB_ITEM_P,
                Count = 200
            },

            new PoolData
            {
                Path = Paths.PREFAB_ITEM_POINT,
                Count = 200
            }
        };
    }
}

public class PoolData
{
    public string Path { get; set; }
    public int Count { get; set; }
}
