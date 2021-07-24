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

public class EntityData
{
    public string EntityType { get; set; }
}

public class EnemyData : EntityData
{
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
        EntityType = id;
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
    public string StrArg { get; set; }
    public bool IsCallBack { get; set; }

    public DialogData(string chaterName, string dialogTxt, string picName, int count, int callBack, string strArg)
    {
        ChaterName = chaterName;
        DialogTxt = dialogTxt;
        PicName = picName;
        Count = count;
        StrArg = strArg;
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
        StrArg = data.StrArg;
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
    public List<Vector3> FinalMovePath { get; set; }
    public List<CardData> Cards { get; set; }           //有多少张符卡

    public BossData(string name, List<string> bossType, List<Vector3> bornPos,
        List<List<Vector3>> appearPath, List<Vector3> finalMovePath, List<CardData> cards)
    {
        Name = name;
        BossType = bossType;
        BornPos = bornPos;
        AppearPath = appearPath;
        FinalMovePath = finalMovePath;
        Cards = cards;
    }
}

public class SingleBossInitData : EntityData
{
    public Vector3 BornPos { get; set; }
    public List<Vector3> AppearPath { get; set; }
    public Vector3 FinalMovePos { get; set; }

    public SingleBossInitData(string bossType, Vector3 bornBos, List<Vector3> appearPath, Vector3 finalMovePos)
    {
        EntityType = bossType;
        BornPos = bornBos;
        AppearPath = appearPath;
        FinalMovePos = finalMovePos;
    }
}

public class SingleBossCardData
{
    public List<List<Vector3>> Path { get; set; }
    public float MoveTime { get; set; }
    public List<float> Duration { get; set; }
    public List<float> Delay { get; set; }
    public List<EmitterProfile> Emitters { get; set; }
    public List<Vector3> EmittersPos { get; set; }

    public SingleBossCardData(List<List<Vector3>> path, float moveTime, List<float> duration, List<float> delay, 
        List<EmitterProfile> emitters, List<Vector3> emittersPos)
    {
        Path = path;
        MoveTime = moveTime;
        Duration = duration;
        Delay = delay;
        Emitters = emitters;
        EmittersPos = emittersPos;
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
    public List<string> NormalBoss { get; set; }
    public int NormalHP { get; set; }
    public int CardHP { get; set; }
    public int BarIndex { get; set; }
    public string CardName { get; set; }
    public List<Vector3> NormalInitPos { get; set; }
    public List<List<List<Vector3>>> NormalPath { get; set; }   //[哪个boss][路径编号][具体路径]
    public List<float> NormalMoveTime { get; set; }
    public List<List<float>> NormalDuration { get; set; }
    public List<List<float>> NormalDelay { get; set; }
    public List<List<EmitterProfile>> NormalEmitter { get; set; }
    public List<List<Vector3>> NormalEmitterPos { get; set; }
    public float NormalTime { get; set; }
    public int NormalP { get; set; }
    public int NormalPoint { get; set; }
    public int NormalLife { get; set; }
    public int NormalBomb { get; set; }

    public List<string> CardBoss { get; set; }
    public List<Vector3> CardInitPos { get; set; }
    public List<List<List<Vector3>>> CardPath { get; set; }   //[哪个boss][路径编号][具体路径]
    public List<float> CardMoveTime { get; set; }
    public List<List<float>> CardDuration { get; set; }
    public List<List<float>> CardDelay { get; set; }
    public List<List<EmitterProfile>> CardEmitter { get; set; }
    public List<List<Vector3>> CardEmitterPos { get; set; }
    public float CardTime { get; set; }
    public int CardP { get; set; }
    public int CardPoint { get; set; }
    public int CardLife { get; set; }
    public int CardBomb { get; set; }
    public int CardBonus { get; set; }
    public int MaxPoint { get; set; }

    public CardData(List<string> normalBoss, int normalHP, int cardHP, int barIndex, string cardName,
        List<Vector3> normalInitPos, List<List<List<Vector3>>> normalPath, List<float> normalMoveTime, 
        List<List<float>> normalDuration, List<List<float>> normalDelay, 
        List<List<EmitterProfile>> normalEmitter, List<List<Vector3>> normalEmitterPos, float normalTime,
        int normalP, int normalPoint, int normalLife, int normalBomb,
        List<string> cardBoss, List<Vector3> cardInitPos, List<List<List<Vector3>>> cardPath, List<float> cardMoveTime,
        List<List<float>> cardDuration, List<List<float>> cardDelay, 
        List<List<EmitterProfile>> cardEmitter, List<List<Vector3>> cardEmitterPos, float cardTime,
        int cardP, int cardPoint, int cardLife, int cardBomb, int cardBonus, 
        int maxPoint)
    {
        NormalBoss = normalBoss;
        NormalHP = normalHP;
        CardHP = cardHP;
        BarIndex = barIndex;
        CardName = cardName;
        NormalInitPos = normalInitPos;
        NormalPath = normalPath;
        NormalMoveTime = normalMoveTime;
        NormalDuration = normalDuration;
        NormalDelay = normalDelay;
        NormalEmitter = normalEmitter;
        NormalEmitterPos = normalEmitterPos;
        NormalTime = normalTime;
        NormalP = normalP;
        NormalPoint = normalPoint;
        NormalLife = normalLife;
        NormalBomb = normalBomb;

        CardBoss = cardBoss;
        CardInitPos = cardInitPos;
        CardPath = cardPath;
        CardMoveTime = cardMoveTime;
        CardDuration = cardDuration;
        CardDelay = cardDelay;
        CardEmitter = cardEmitter;
        CardEmitterPos = cardEmitterPos;
        CardTime = cardTime;
        CardTime = cardTime;
        CardP = cardP;
        CardPoint = cardPoint;
        CardLife = cardLife;
        CardBomb = cardBomb;
        CardBonus = cardBonus;
        MaxPoint = maxPoint;
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

public class GetCardInfo
{
    public int LabelIndex { get; set; }
    public int Score { get; set; }
    public float TimeValue { get; set; }

    public GetCardInfo(int labelIndex, int score, float timeValue)
    {
        LabelIndex = labelIndex;
        Score = score;
        TimeValue = timeValue;
    }
}

public class ElitleData : EntityData
{
    public Vector3 BornPos { get; set; }
    public Vector3 AppearPath { get; set; }
    public List<List<Vector3>> MovePath { get; set; }
    public float MoveTime { get; set; }
    public List<float> Delay { get; set; }
    public List<float> Duration { get; set; }
    public Vector3 ExitPath { get; set; }
    public float TimeLimit { get; set; }
    public List<EmitterProfile> Emitters { get; set; }
    public List<Vector3> EmitterPos { get; set; }
    public int HP { get; set; }
    public int PCount { get; set; }
    public int PointCount { get; set; }
    public int LifeCount { get; set; }
    public int BombCount { get; set; }

    public ElitleData(string eliteType, Vector3 bornPos, Vector3 appearPath, 
        List<List<Vector3>> movePath, float moveTime, List<float> delay, List<float> duration, Vector3 exitPath,float timeLimit,
        List<EmitterProfile> emitters, List<Vector3> emitterPos, int hp, int pCount, int pointCount, int lifeCount, int bombCount)
    {
        EntityType = eliteType;
        BornPos = bornPos;
        AppearPath = appearPath;
        MovePath = movePath;
        MoveTime = moveTime;
        Delay = delay;
        Duration = duration;
        ExitPath = exitPath;
        TimeLimit = timeLimit;
        Emitters = emitters;
        EmitterPos = emitterPos;
        HP = hp;
        PCount = pCount;
        PointCount = pointCount;
        LifeCount = lifeCount;
        BombCount = bombCount;
    }
}