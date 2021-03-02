using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyData
{
    public string TypeID { get; set; }
    public string BornPos { get; set; }
    public string Path { get; set; }

    public EnemyData(string id, string pos, string path)
    {
        TypeID = id;
        BornPos = pos;
        Path = path;
    }
}