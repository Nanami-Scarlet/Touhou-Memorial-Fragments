﻿using System.Collections;
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
                 Count = 10
            },

            //todo:添加其他的Data
        };
    }
}

public class PoolData
{
    public string Path { get; set; }
    public int Count { get; set; }
}
