using BulletPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageData
{
    public string Name { get; set; }
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
    public EmitterProfile Emitter { get; set; }
    public int HP { get; set; }
    public int PCount { get; set; }
    public int PointCount { get; set; }

    public EnemyData(string id, float delay, Vector3 pos,
        List<List<Vector3>> path, float pauseTime, float pathDurUP,
        float pathDurDown, EmitterProfile emitter, int hp, 
        int pCount, int pointCount)
    {
        TypeID = id;
        Delay = delay;
        BornPos = pos;
        Path = path;
        PauseTime = pauseTime;
        PathDurUP = pathDurUP;
        PathDurDown = pathDurDown;
        Emitter = emitter;
        HP = hp;
        PCount = pCount;
        PointCount = pointCount;
    }
}

public class YinEmitter
{
    public List<BulletEmitter> NormalEmitters { get; set; }
    public List<BulletEmitter> FoucusEmitters { get; set; }
}

public class AudioData
{
    public string Name { get; set; }
    public float Volume { get; set; }
    
}