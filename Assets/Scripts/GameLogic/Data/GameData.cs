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

public class DialogData
{
    public string ChaterName { get; set; }
    public string DialogTxt { get; set; }
    public string PicName { get; set; }
    public int Count { get; set; }
    public int CallBack { get; set; }
    public bool IsCallBack { get; set; }

    public DialogData(string chaterName, string dialogTxt, string picName, int count, int callBack)
    {
        ChaterName = chaterName;
        DialogTxt = dialogTxt;
        PicName = picName;
        Count = count;
        CallBack = callBack;

        if(callBack == -1)
        {
            IsCallBack = true;
        }
    }

    public DialogData(DialogData data)
    {
        ChaterName = data.ChaterName;
        DialogTxt = data.DialogTxt;
        PicName = data.PicName;
        Count = data.Count;
        CallBack = data.CallBack;
        IsCallBack = data.IsCallBack;
    }
}

public class ChatPicData
{
    public Vector2 PicSize { get; set; }
    public int Index { get; set; }

    public ChatPicData(Vector2 picSize, int index)
    {
        PicSize = picSize;
        Index = index;
    }
}

public class CardPicData
{
    public string PicName { get; set; }
    public Vector2 PicSize { get; set; }
    public int Index { get; set; }

    public CardPicData(string picName, Vector2 picSize, int index)
    {
        PicName = picName;
        PicSize = picSize;
        Index = index;
    }
}

public class BossData
{
    public string Name { get; set; }
    public List<string> BossType { get; set; }
    public List<Vector3> BornPos { get; set; }
    public List<List<Vector3>> AppearPath { get; set; }
    public List<CardData> Cards { get; set; }           //有多少张符卡

    public BossData(string name, List<string> bossType, List<Vector3> bornPos,
        List<List<Vector3>> appearPath, List<CardData> cards)
    {
        Name = name;
        BossType = bossType;
        BornPos = bornPos;
        AppearPath = appearPath;
        Cards = cards;
    }
}

public class SingleBossInitData {
    public string BossType { get; set; }
    public Vector3 BornPos { get; set; }
    public List<Vector3> AppearPath { get; set; }

    public SingleBossInitData(string bossType, Vector3 bornBos, List<Vector3> appearPath)
    {
        BossType = bossType;
        BornPos = bornBos;
        AppearPath = appearPath;
    }
}

public class SingleBossCardData
{
    public List<List<Vector3>> Path { get; set; }
    public List<float> Duration { get; set; }
    public List<float> Delay { get; set; }
    public List<EmitterProfile> Emitters { get; set; }

    public SingleBossCardData(List<List<Vector3>> path, List<float> duration, List<float> delay, List<EmitterProfile> emitters)
    {
        Path = path;
        Duration = duration;
        Delay = delay;
        Emitters = emitters;
    }
}

public class BaseCardHPData
{
    public int NormalHP { get; set; }
    public int CardHP { get; set; }
    public int BarIndex { get; set; }
    public Transform TransBoss { get; set; }

    public BaseCardHPData(int normalHP, int cardHP, int barIndex, Transform transBoss)
    {
        NormalHP = normalHP;
        CardHP = cardHP;
        BarIndex = barIndex;
        TransBoss = transBoss;
    }
}

public class CardData
{
    public List<string> CurBoss { get; set; }
    public int NormalHP { get; set; }
    public int CardHP { get; set; }
    public int BarIndex { get; set; }
    public string CardName { get; set; }
    public List<List<List<Vector3>>> NormalPath { get; set; }   //[哪个boss][路径编号][具体路径]
    public List<List<float>> NormalDuration { get; set; }
    public List<List<float>> NormalDelay { get; set; }
    public List<List<EmitterProfile>> NormalEmitter { get; set; }
    public float NormalTime { get; set; }

    public Vector3 CardInitPos { get; set; }
    public List<List<List<Vector3>>> CardPath { get; set; }   //[哪个boss][路径编号][具体路径]
    public List<List<float>> CardDuration { get; set; }
    public List<List<float>> CardDelay { get; set; }
    public List<List<EmitterProfile>> CardEmitter { get; set; }
    public float CardTime { get; set; }

    public CardData(List<string> curBoss, int normalHP, int cardHP, int barIndex, string cardName,
        List<List<List<Vector3>>> normalPath, List<List<float>> normalDuration, List<List<float>> normalDelay, List<List<EmitterProfile>> normalEmitter, float normalTime,
        Vector3 cardInitPos, List<List<List<Vector3>>> cardPath, List<List<float>> cardDuration, List<List<float>> cardDelay, List<List<EmitterProfile>> cardEmitter, float cardTime)
    {
        CurBoss = curBoss;
        NormalHP = normalHP;
        CardHP = cardHP;
        BarIndex = barIndex;
        CardName = cardName;
        NormalPath = normalPath;
        NormalDuration = normalDuration;
        NormalDelay = normalDelay;
        NormalEmitter = normalEmitter;
        NormalTime = normalTime;

        CardInitPos = cardInitPos;
        CardPath = cardPath;
        CardDuration = cardDuration;
        CardDelay = cardDelay;
        CardEmitter = cardEmitter;
        CardTime = cardTime;
    }
}

public class SingleCardData
{
    public int NormalHP { get; set; }
    public int CardHP { get; set; }
    public int BarIndex { get; set; }
    public List<List<Vector3>> NormalPath { get; set; }
    public List<float> NormalDuration { get; set; }
    public List<float> NormalDelay { get; set; }

    public SingleCardData(int normalHP, int cardHP, int barIndex,
        List<List<Vector3>> normalPath, List<float> normalDuration, List<float> normalDelay) 
    {
        NormalHP = normalHP;
        CardHP = cardHP;
        BarIndex = barIndex;
        NormalPath = normalPath;
        NormalDuration = normalDuration;
        NormalDelay = normalDelay;
    }
}

public class BossNameCard
{
    public string Name { get; set; }
    public int CardCount { get; set; }

    public BossNameCard(string name, int cardCount)
    {
        Name = name;
        CardCount = cardCount;
    }
}

public class HPData
{
    public GameObject BossGO { get; set; }
    public int CurHP { get; set; }

    public HPData(GameObject bossGO, int curHP)
    {
        BossGO = bossGO;
        CurHP = curHP;
    }
}