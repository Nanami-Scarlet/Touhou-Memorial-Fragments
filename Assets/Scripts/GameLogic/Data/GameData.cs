using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageData
{
    public string Name { get; set; }
    public int WaveCount { get; set; }
    public List<WaveData> ListWaveEnemy { get; set; }
}

public class WaveData
{
    public List<EnemyData> ListEnemy { get; set; }
}

public class EnemyData
{
    public string TypeID { get; set; }
    public float Delay { get; set; }
    public Vector3 BornPos { get; set; }
    public List<List<Vector3>> Path { get; set; }
    public float PauseTime { get; set; }
    public float PathDurUP { get; set; }
    public float PathDurDown { get; set; }

    public EnemyData(string id, float delay, Vector3 pos,
        List<List<Vector3>> path, float pauseTime, float pathDurUP,
        float pathDurDown)
    {
        TypeID = id;
        Delay = delay;
        BornPos = pos;
        Path = path;
        PauseTime = pauseTime;
        PathDurUP = pathDurUP;
        PathDurDown = pathDurDown;
    }
}