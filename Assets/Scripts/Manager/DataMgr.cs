using BulletPro;
using LitJson;
using System.Collections;
using System.ComponentModel;
using System.Collections.Generic;
using UnityEngine;
using System;

public class DataMgr : NormalSingleton<DataMgr>, IInit
{
    private Dictionary<string, int> _dicViewData = new Dictionary<string, int>();
    private Dictionary<string, StageData> _dicStageData = new Dictionary<string, StageData>();
    private Dictionary<string, BossData> _dicBossData = new Dictionary<string, BossData>();
    private Dictionary<string, List<ElitleData>> _dicEliteData = new Dictionary<string, List<ElitleData>>();
    private Dictionary<string, Dictionary<string, EmitterProfile>> _dicStageEmitter = new Dictionary<string, Dictionary<string, EmitterProfile>>();
    private Dictionary<AudioType, AudioData> _dicTypeAudio = new Dictionary<AudioType, AudioData>();
    private Dictionary<string, AudioData> _dicBGMName = new Dictionary<string, AudioData>();
    private Dictionary<string, List<DialogData>> _dicDialogData = new Dictionary<string, List<DialogData>>();
    private Dictionary<int, ChatPicData> _dicChatPicData = new Dictionary<int, ChatPicData>();
    private Dictionary<string, CardPicData> _dicCardPicData = new Dictionary<string, CardPicData>();
    private Dictionary<string, Sprite> _dicNameSprite = new Dictionary<string, Sprite>();

    public void Init()
    {
        InitViewConfig();
        InitEmitData();
        InitAudioData();
        InitBGMData();
        InitChatData();
        InitChatPicData();
        InitCardPicData();
        InitNameSpriteData();

        InitEnemyData();
        InitBossData();
        InitEliteData();
    }

    #region ViewData
    private void InitViewConfig()
    {
        TextAsset config = LoadMgr.Single.LoadConfig(Paths.CONFIG_VIEW);

        JsonData json = JsonMapper.ToObject(config.text);
        JsonData views = json["views"];

        for(int i = 0; i < views.Count; ++i)
        {
            JsonData view = views[i];

            string name = GetValue<string>(view["name"]).Trim('"');
            int layer = GetValue<int>(view["layer"]);

            _dicViewData.Add(name, layer);
        }
    }

    public Dictionary<string, int> GetViewData()
    {
        return _dicViewData;
    }
    #endregion

    #region EnemyConfig
    private void InitEnemyData()
    {
        TextAsset config = LoadMgr.Single.LoadConfig(Paths.CONFIG_ENEMY);

        JsonData json = JsonMapper.ToObject(config.text);

        for(NormalStage i = NormalStage.stage1_1; i < NormalStage.COUNT; ++i)
        {
            JsonData stage = json[i.ToString()];
            StageData stageData = new StageData()
            {
                Name = i.ToString(),
                ListWaveEnemy = new List<WaveData>()
            };

            for(int j = 0; j < stage.Count; ++j)
            {
                JsonData wave = stage[j];           //波
                WaveData waveData = new WaveData()
                {
                    ListEnemy = new List<EnemyData>()
                };

                for(int k = 0; k < wave.Count; ++k)
                {
                    JsonData enemy = wave[k];       //每个具体的敌人

                    string enemyType = GetValue<string>(enemy["enemyType"]).Trim('"');
                    float delay = GetValue<float>(enemy["delay"]);
                    string strPos = GetValue<string>(enemy["bornPos"]).Trim('"');
                    string strPath = GetValue<string>(enemy["path"]).Trim('"');
                    float pauseTime = GetValue<float>(enemy["pauseTime"]);
                    float pathDurUP = GetValue<float>(enemy["pathDurUP"]);
                    float pathDurDown = GetValue<float>(enemy["pathDurDown"]);
                    int hp = GetValue<int>(enemy["hp"]);
                    int pCount = GetValue<int>(enemy["pCount"]);
                    int pointCount = GetValue<int>(enemy["pointCount"]);

                    string emitterName = GetValue<string>(enemy["emitterName"]).Trim('"');
                    EmitterProfile emitter = GetEmit(i.ToString(), j + "_" + emitterName);

                    EnemyData enemyData = new EnemyData(
                        enemyType, delay, GetVector3(strPos), GetMulPath(strPath), pauseTime,
                        pathDurUP, pathDurDown, emitter, hp, pCount, pointCount);

                    waveData.ListEnemy.Add(enemyData);
                }

                stageData.ListWaveEnemy.Add(waveData);
            }
            _dicStageData.Add(i.ToString(), stageData);
        }
    }

    public StageData GetStageData(string stageName)
    {
        if (!_dicStageData.ContainsKey(stageName))
        {
            Debug.LogError("没有加载该场景中的数据，场景名为：" + stageName);
            return null;
        }

        return _dicStageData[stageName];
    }
    #endregion

    #region ChatData

    private void ReadChatData(JsonData json, string stage)
    {
        JsonData stageChat = json[stage];
        List<DialogData> dialogs = new List<DialogData>();

        for (int j = 0; j < stageChat.Count; ++j)
        {
            JsonData chat = stageChat[j];

            string chaterName = chat["chaterName"].ToString();
            string DialogTxt = chat["DialogTxt"].ToString();
            string picName = GetValue<string>(chat["picName"]).Trim('"');
            int count = GetValue<int>(chat["count"]);
            int callBack = GetValue<int>(chat["callback"]);
            string strArg = GetValue<string>(chat["strArg"]).Trim('"');

            DialogData dialogData = new DialogData(chaterName, DialogTxt, picName, count, callBack, strArg);
            dialogs.Add(dialogData);
        }

        _dicDialogData.Add(stage, dialogs);
    }

    private void InitChatData()
    {
        TextAsset config = LoadMgr.Single.LoadConfig(Paths.CONFIG_CHAT);

        JsonData json = JsonMapper.ToObject(config.text);
        for(BossStage i = BossStage.stage_B1; i < BossStage.COUNT; ++i)
        {
            ReadChatData(json, i.ToString());
        }

        for(ChatStage i = ChatStage.Ending; i < ChatStage.COUNT; ++i)
        {
            ReadChatData(json, i.ToString());
        }
    }

    public void GetDialogData(out List<DialogData> dialogs, string stageName)
    {
        if (!_dicDialogData.ContainsKey(stageName))
        {
            Debug.LogError("在该关卡下没有对话，关卡名为：" + stageName);
            dialogs = null;
            return;
        }

        dialogs = new List<DialogData>();               //取得副本
        for (int i = 0; i < _dicDialogData[stageName].Count; ++i)
        {
            DialogData data = new DialogData(_dicDialogData[stageName][i]);

            dialogs.Add(data);
        }
    }
    #endregion

    #region BossData
    private void InitBossData()
    {
        TextAsset config = LoadMgr.Single.LoadConfig(Paths.CONFIG_BOSS);

        JsonData json = JsonMapper.ToObject(config.text);
        JsonData bosses = json["boss"];

        for(int i = 0; i < bosses.Count; ++i)
        {
            JsonData boss = bosses[i];

            string stage = GetValue<string>(boss["stage"]).Trim('"');
            string name = boss["name"].ToString();

            List<string> bossType = GetMulString(GetValue<string>(boss["bossType"]).Trim('"'));

            List<Vector3> bornPos = new List<Vector3>(2);
            JsonData bornPosJson = boss["bornPos"];
            for(int j = 0; j < bornPosJson.Count; ++j)
            {
                JsonData path = bornPosJson[j];

                Vector3 pos = GetVector3(GetValue<string>(path["path"]).Trim('"'));

                bornPos.Add(pos);
            }

            List<List<Vector3>> appearPath = new List<List<Vector3>>(2);
            JsonData appearPathJson = boss["appearPath"];
            for (int j = 0; j < appearPathJson.Count; ++j)
            {
                JsonData path = appearPathJson[j];

                List<Vector3> temp = GetPath(GetValue<string>(path["path"]).Trim('"'));
                appearPath.Add(temp);
            }

            List<Vector3> finalInitPos = new List<Vector3>(2);
            JsonData finalInitPosJson = boss["finalInitPos"];
            for(int j = 0; j < finalInitPosJson.Count; ++j)
            {
                JsonData tempJson = finalInitPosJson[j];

                Vector3 pos = GetVector3(GetValue<string>(tempJson["pos"]).Trim('"'));
                finalInitPos.Add(pos);
            }

            List<CardData> cards = new List<CardData>();
            JsonData cardJson = boss["cards"];
            for(int j = 0; j < cardJson.Count; ++j)
            {
                JsonData card = cardJson[j];

                List<string> normalBoss = GetMulString(GetValue<string>(card["normalBoss"]).Trim('"'));
                int normalHP = GetValue<int>(card["normalHP"]);
                int cardHP = GetValue<int>(card["cardHP"]);
                int barIndex = GetValue<int>(card["barIndex"]);
                string cardName = card["cardName"].ToString();
                float normalTime = GetValue<float>(card["normalTime"]);
                int normalP = GetValue<int>(card["normalP"]);
                int normalPoint = GetValue<int>(card["normalPoint"]);
                int normalLife = GetValue<int>(card["normalLife"]);
                int normalBomb = GetValue<int>(card["normalBomb"]);

                List<Vector3> normalInitPos = new List<Vector3>(2);
                JsonData normalInitPosJson = card["normalInitPos"];
                for (int k = 0; k < normalInitPosJson.Count; ++k)
                {
                    JsonData tempJson = normalInitPosJson[k];

                    Vector3 pos = GetVector3(GetValue<string>(tempJson["pos"]).Trim('"'));
                    normalInitPos.Add(pos);
                }

                List<string> cardBoss = GetMulString(GetValue<string>(card["normalBoss"]).Trim('"'));
                List<Vector3> cardInitPos = new List<Vector3>(2);
                JsonData cardInitPosJson = card["cardInitPos"];
                for (int k = 0; k < cardInitPosJson.Count; ++k)
                {
                    JsonData tempJson = cardInitPosJson[k];

                    Vector3 pos = GetVector3(GetValue<string>(tempJson["pos"]).Trim('"'));
                    cardInitPos.Add(pos);
                }
                float cardTime = GetValue<float>(card["cardTime"]);
                int cardP = GetValue<int>(card["cardP"]);
                int cardPoint = GetValue<int>(card["cardPoint"]);
                int cardLife = GetValue<int>(card["cardLife"]);
                int cardBomb = GetValue<int>(card["cardBomb"]);
                int cardBonus = GetValue<int>(card["cardBonus"]);
                int maxPoint = GetValue<int>(card["maxPoint"]);

                List<List<List<Vector3>>> normalPath = new List<List<List<Vector3>>>(2);
                JsonData normalPathJson = card["normalPath"];
                for(int k = 0; k < normalPathJson.Count; ++k)
                {
                    JsonData tempJson = normalPathJson[k];

                    List<List<Vector3>> tempPath = GetMulPath(GetValue<string>(tempJson["path"]).Trim('"'));
                    normalPath.Add(tempPath);
                }

                List<float> normalMoveTime = new List<float>(2);
                JsonData normalMoveTimeJson = card["normalMoveTime"];
                for(int k = 0; k < normalMoveTimeJson.Count; ++k)
                {
                    JsonData tempJson = normalMoveTimeJson[k];

                    float t = GetValue<float>(tempJson["time"]);
                    normalMoveTime.Add(t);
                }

                List<List<float>> normalDuration = new List<List<float>>(2);
                JsonData normalDurationJson = card["normalDuration"];
                for (int k = 0; k < normalDurationJson.Count; ++k)
                {
                    JsonData tempJson = normalDurationJson[k];

                    List<float> tempPath = GetMulFloat(GetValue<string>(tempJson["duration"]).Trim('"'));
                    normalDuration.Add(tempPath);
                }

                List<List<float>> normalDelay = new List<List<float>>(2);
                JsonData normalDelayJson = card["normalDelay"];
                for(int k = 0; k < normalDelayJson.Count; ++k)
                {
                    JsonData tempJson = normalDelayJson[k];

                    List<float> tempDelay = GetMulFloat(GetValue<string>(tempJson["delay"]).Trim('"'));
                    normalDelay.Add(tempDelay);
                }

                List<List<EmitterProfile>> normalEmitter = new List<List<EmitterProfile>>(2);
                JsonData normalEmitterJson = card["normalEmitter"];
                for (int k = 0; k < normalEmitterJson.Count; ++k)
                {
                    JsonData tempJson = normalEmitterJson[k];
                    List<EmitterProfile> emitters = new List<EmitterProfile>();

                    string emitterName = GetValue<string>(tempJson["emitter"]).Trim('"');
                    string[] temp = emitterName.Split('|');
                    for(int m = 0; m < temp.Length; ++m)
                    {
                        EmitterProfile emitter = GetEmit(stage, temp[m]);
                        emitters.Add(emitter);
                    }
                    normalEmitter.Add(emitters);
                }

                List<List<Vector3>> normalEmitterPos = new List<List<Vector3>>(2);
                JsonData normalEmitterPosJson = card["normalEmitterPos"];
                for(int k = 0; k < normalEmitterPosJson.Count; ++k)
                {
                    JsonData tempJson = normalEmitterPosJson[k];
                    string strPos = GetValue<string>(tempJson["pos"]).Trim('"');
                    List<Vector3> poss = GetPath(strPos, '|');

                    normalEmitterPos.Add(poss);
                }

                List<List<List<Vector3>>> cardPath = new List<List<List<Vector3>>>(2);
                JsonData cardPathJson = card["cardPath"];
                for (int k = 0; k < cardPathJson.Count; ++k)
                {
                    JsonData tempJson = cardPathJson[k];

                    List<List<Vector3>> tempPath = GetMulPath(GetValue<string>(tempJson["path"]).Trim('"'));
                    cardPath.Add(tempPath);
                }

                List<float> cardMoveTime = new List<float>(2);
                JsonData cardMoveTimeJson = card["cardMoveTime"];
                for (int k = 0; k < cardMoveTimeJson.Count; ++k)
                {
                    JsonData tempJson = cardMoveTimeJson[k];

                    float t = GetValue<float>(tempJson["time"]);
                    cardMoveTime.Add(t);
                }

                List<List<float>> cardDuration = new List<List<float>>(2);
                JsonData cardDurationJson = card["cardDuration"];
                for (int k = 0; k < cardDurationJson.Count; ++k)
                {
                    JsonData tempJson = cardDurationJson[k];

                    List<float> tempPath = GetMulFloat(GetValue<string>(tempJson["duration"]).Trim('"'));
                    cardDuration.Add(tempPath);
                }

                List<List<float>> cardDelay = new List<List<float>>(2);
                JsonData cardDelayJson = card["cardDelay"];
                for (int k = 0; k < cardDelayJson.Count; ++k)
                {
                    JsonData tempJson = cardDelayJson[k];

                    List<float> tempDelay = GetMulFloat(GetValue<string>(tempJson["delay"]).Trim('"'));
                    cardDelay.Add(tempDelay);
                }

                List<List<EmitterProfile>> cardEmitter = new List<List<EmitterProfile>>(2);
                JsonData cardEmitterJson = card["cardEmitter"];
                for (int k = 0; k < cardEmitterJson.Count; ++k)
                {
                    JsonData tempJson = cardEmitterJson[k];
                    List<EmitterProfile> emitters = new List<EmitterProfile>();

                    string emitterName = GetValue<string>(tempJson["emitter"]).Trim('"');
                    string[] temp = emitterName.Split('|');
                    for (int m = 0; m < temp.Length; ++m)
                    {
                        EmitterProfile emitter = GetEmit(stage, temp[m]);
                        emitters.Add(emitter);
                    }
                    cardEmitter.Add(emitters);
                }

                List<List<Vector3>> cardEmitterPos = new List<List<Vector3>>(2);
                JsonData cardEmitterPosJson = card["cardEmitterPos"];
                for (int k = 0; k < cardEmitterPosJson.Count; ++k)
                {
                    JsonData tempJson = cardEmitterPosJson[k];
                    string strPos = GetValue<string>(tempJson["pos"]).Trim('"');
                    List<Vector3> poss = GetPath(strPos, '|');

                    cardEmitterPos.Add(poss);
                }

                CardData cardData = new CardData(normalBoss, normalHP, cardHP, barIndex, cardName,
                    normalInitPos, normalPath, normalMoveTime, normalDuration, normalDelay, 
                    normalEmitter, normalEmitterPos, normalTime,
                    normalP, normalPoint, normalLife, normalBomb, 
                    cardBoss, cardInitPos, cardPath, cardMoveTime, cardDuration, cardDelay, 
                    cardEmitter, cardEmitterPos, cardTime,
                    cardP, cardPoint, cardLife, cardBomb, cardBonus, 
                    maxPoint);

                cards.Add(cardData);
            }

            BossData bossData = new BossData(name, bossType, bornPos, appearPath, finalInitPos, cards);
            _dicBossData.Add(stage, bossData);
        }
    }

    public BossData GetBossData(string stageName)
    {
        if (!_dicBossData.ContainsKey(stageName))
        {
            Debug.LogError("该场景不存在Boss，场景名为：" + stageName);
            return null;
        }

        return _dicBossData[stageName];
    }
    #endregion

    #region EliteData
    private void InitEliteData()
    {
        TextAsset config = LoadMgr.Single.LoadConfig(Paths.CONFIG_ELITE);
        JsonData json = JsonMapper.ToObject(config.text);

        List<ElitleData> elitles = new List<ElitleData>();
        for(EliteStage i = EliteStage.stage2_2; i < EliteStage.COUNT; ++i)
        {
            string stageName = i.ToString();

            JsonData stageData = json[stageName];
            for(int j = 0; j < stageData.Count; ++j)
            {
                JsonData data = stageData[j];

                string enemyType = GetValue<string>(data["enemyType"]).Trim('"');
                Vector3 bornPos = GetVector3(GetValue<string>(data["bornPos"]).Trim('"'));
                Vector3 appearPath = GetVector3(GetValue<string>(data["appearPath"]).Trim('"'));

                string strMovePath = GetValue<string>(data["movePath"]).Trim('"');
                List<List<Vector3>> movePath = GetMulPath(strMovePath);

                float moveTime = GetValue<float>(data["moveTime"]);
                List<float> delay = GetMulFloat(GetValue<string>(data["delay"]).Trim('"'));
                List<float> duration = GetMulFloat(GetValue<string>(data["duration"]).Trim('"'));
                Vector3 exitPath = GetVector3(GetValue<string>(data["exitPath"]).Trim('"'));

                float timeLimit = GetValue<float>(data["timeLimit"]);

                string emitterNames = GetValue<string>(data["emitter"]).Trim('"');
                List<EmitterProfile> emitters = new List<EmitterProfile>();
                string[] tempEmitterName = emitterNames.Split('|');
                for(int k = 0; k < tempEmitterName.Length; ++k)
                {
                    EmitterProfile profile = GetEmit(stageName, j + "_" + tempEmitterName[k]);
                    emitters.Add(profile);
                }

                string strEmitterPos = GetValue<string>(data["emitterPos"]).Trim('"');
                List<Vector3> emitterPos = new List<Vector3>();
                string[] tempPos = strEmitterPos.Split('|');
                for(int k = 0; k < tempPos.Length; ++k)
                {
                    Vector3 pos = GetVector3(tempPos[k]);

                    emitterPos.Add(pos);
                }

                int hp = GetValue<int>(data["hp"]);
                int pCount = GetValue<int>(data["pCount"]);
                int pointCount = GetValue<int>(data["pointCount"]);
                int lifeCount = GetValue<int>(data["lifeCount"]);
                int BombCount = GetValue<int>(data["BombCount"]);

                ElitleData elitleData = new ElitleData(enemyType, bornPos, appearPath, 
                    movePath, moveTime, delay, duration, exitPath, timeLimit, 
                    emitters, emitterPos, hp, pCount, pointCount, lifeCount, BombCount);

                elitles.Add(elitleData);
            }

            _dicEliteData.Add(stageName, elitles);
        }
    }

    public List<ElitleData> GetElitleData(string stageName)
    {
        if (!_dicEliteData.ContainsKey(stageName))
        {
            Debug.LogError("该场景不存在此精英怪序列，场景名为：" + stageName);
            return null;
        }

        return _dicEliteData[stageName];
    }
    #endregion

    #region ChatPicData
    private void InitChatPicData()
    {
        TextAsset config = LoadMgr.Single.LoadConfig(Paths.CONFIG_CHAT_PICTURE);

        JsonData json = JsonMapper.ToObject(config.text);
        JsonData pics = json["pic"];

        for(int i = 0; i < pics.Count; ++i)
        {
            JsonData pic = pics[i];

            int character = GetValue<int>(pic["character"]);
            string size = GetValue<string>(pic["size"]).Trim('"');
            int index = GetValue<int>(pic["index"]);

            ChatPicData picData = new ChatPicData(GetVector2(size), index);

            _dicChatPicData.Add(character, picData);
        }
    }

    public Dictionary<int, ChatPicData> GetChatPicData()
    {
        return _dicChatPicData;    
    }
    #endregion

    #region CardPicData
    private void InitCardPicData()
    {
        TextAsset config = LoadMgr.Single.LoadConfig(Paths.CONFIG_CARD_PICTURE);

        JsonData json = JsonMapper.ToObject(config.text);
        JsonData pics = json["pic"];

        for (int i = 0; i < pics.Count; ++i)
        {
            JsonData pic = pics[i];

            string bossType = GetValue<string>(pic["bossType"]).Trim('"');
            string picName = GetValue<string>(pic["picName"]).Trim('"');
            string size = GetValue<string>(pic["size"]).Trim('"');
            int index = GetValue<int>(pic["index"]);

            CardPicData cardPicData = new CardPicData(picName, GetVector2(size), index);
            _dicCardPicData.Add(bossType, cardPicData);
        }
    }

    public Dictionary<string, CardPicData> GetCardPicData()
    {
        return _dicCardPicData;
    } 
    #endregion

    #region EmitData
    private void InitEmitData()
    {
        for (NormalStage i = NormalStage.stage1_1; i < NormalStage.COUNT; ++i)
        {
            _dicStageEmitter[i.ToString()] = new Dictionary<string, EmitterProfile>();

            EmitterProfile[] emitters = LoadMgr.Single.LoadAll<EmitterProfile>(Paths.ASSET_BULLET_FOLDER + i.ToString());
            for (int j = 0; j < emitters.Length; ++j)
            {
                _dicStageEmitter[i.ToString()].Add(emitters[j].name, emitters[j]);
            }
        }

        for(BossStage i = BossStage.stage_B1; i < BossStage.COUNT; ++i)
        {
            _dicStageEmitter[i.ToString()] = new Dictionary<string, EmitterProfile>();

            EmitterProfile[] emitters = LoadMgr.Single.LoadAll<EmitterProfile>(Paths.ASSET_BULLET_FOLDER + i.ToString());
            for (int j = 0; j < emitters.Length; ++j)
            {
                _dicStageEmitter[i.ToString()].Add(emitters[j].name, emitters[j]);
            }
        }

        for(EliteStage i = EliteStage.stage2_2; i < EliteStage.COUNT; ++i)
        {
            _dicStageEmitter[i.ToString()] = new Dictionary<string, EmitterProfile>();

            EmitterProfile[] emitters = LoadMgr.Single.LoadAll<EmitterProfile>(Paths.ASSET_BULLET_FOLDER + i.ToString());
            for (int j = 0; j < emitters.Length; ++j)
            {
                _dicStageEmitter[i.ToString()].Add(emitters[j].name, emitters[j]);
            }
        }
    }

    private EmitterProfile GetEmit(string stageName, string name)
    {
        if (!_dicStageEmitter[stageName].ContainsKey(name))
        {
            Debug.LogError("在面" + stageName + "不存在该弹幕，弹幕名为：" + name);
            return null;
        }

        return _dicStageEmitter[stageName][name];
    }
    #endregion

    #region AudioData
    private void InitAudioData()
    {
        TextAsset config = LoadMgr.Single.LoadConfig(Paths.CONFIG_VOLUME);

        JsonData json = JsonMapper.ToObject(config.text);

        for (int i = 0; i < json.Count; ++i)
        {
            JsonData audio = json[i];
            string typeName = GetValue<string>(audio["type"]).Trim('"');
            string name = GetValue<string>(audio["name"]).Trim('"');
            float vol = GetValue<float>(audio["vol"]);

            AudioType type = (AudioType)Enum.Parse(typeof(AudioType), typeName);
            AudioData data = new AudioData()
            {
                Name = name,
                Volume = vol
            };

            _dicTypeAudio.Add(type, data);
        }
    }

    public AudioData GetAudioData(AudioType type)
    {
        if (!_dicTypeAudio.ContainsKey(type))
        {
            Debug.LogError("不存在的音频枚举，名字为：" + type);
            return null;
        }

        return _dicTypeAudio[type];
    }
    #endregion

    #region BGMData
    private void InitBGMData()
    {
        TextAsset config = LoadMgr.Single.LoadConfig(Paths.CONFIG_BGM);

        JsonData json = JsonMapper.ToObject(config.text);

        for(int i = 0; i < json.Count; ++i)
        {
            JsonData bgm = json[i];
            string stageName = GetValue<string>(bgm["stageName"]).Trim('"');
            string trueName = bgm["trueName"].ToString();
            float vol = GetValue<float>(bgm["volume"]);

            AudioData audioData = new AudioData()
            {
                Name = trueName,
                Volume = vol
            };

            _dicBGMName.Add(stageName, audioData);
        }
    }

    public AudioData GetBGMData(string stageName)
    {
        if(!_dicBGMName.ContainsKey(stageName))
        {
            Debug.LogError("不存在该面的BGM，面数为：" + stageName);
            return null;
        }

        return _dicBGMName[stageName];
    }

    #endregion

    #region SpriteData
    private void InitNameSpriteData()
    {
        Sprite[] allPic = LoadMgr.Single.LoadAll<Sprite>(Paths.PICTURE_FOLDER);

        for (int i = 0; i < allPic.Length; ++i)
        {
            Sprite s = allPic[i];

            _dicNameSprite.Add(s.name, s);
        }
    }

    public Dictionary<string, Sprite> GetNameSpriteData()
    {
        return _dicNameSprite;
    } 
    #endregion

    #region Tools
    private T GetValue<T>(JsonData json)
    {
        var converter = TypeDescriptor.GetConverter(typeof(T));

        if (converter.CanConvertTo(typeof(T)))
        {
            return (T)converter.ConvertTo(json.ToJson(), typeof(T));
        }

        return (T)(object)json;
    }

    private Vector3 GetVector3(string strPos)
    {
        string[] temp = strPos.Split(',');

        return new Vector3(float.Parse(temp[0]), float.Parse(temp[1]), 0);
    }

    private Vector2 GetVector2(string strPos)
    {
        string[] temp = strPos.Split(',');

        return new Vector2(float.Parse(temp[0]), float.Parse(temp[1]));
    }

    //为路径动画做铺垫
    private List<Vector3> GetPath(string strPath, char c = '#')
    {
        List<Vector3> ret = new List<Vector3>();

        string[] temp = strPath.Split(c);
        for(int i = 0; i < temp.Length; ++i)
        {
            ret.Add(GetVector3(temp[i]));
        }

        return ret;
    }

    private List<float> GetMulFloat(string str)
    {
        List<float> ret = new List<float>();

        string[] temp = str.Split('#');
        for (int i = 0; i < temp.Length; ++i)
        {
            ret.Add(float.Parse(temp[i]));
        }

        return ret;
    }

    private List<string> GetMulString(string str)
    {
        List<string> ret = new List<string>();

        string[] temp = str.Split('#');
        for (int i = 0; i < temp.Length; ++i)
        {
            ret.Add(temp[i]);
        }

        return ret;
    }

    private List<List<Vector3>> GetMulPath(string strPath)
    {
        List<List<Vector3>> ret = new List<List<Vector3>>();

        string[] temp = strPath.Split('|');
        for (int i = 0; i < temp.Length; ++i)
        {
            List<Vector3> path = new List<Vector3>();
            string[] subTemp = temp[i].Split('#');
            for (int j = 0; j < subTemp.Length; ++j)
            {
                path.Add(GetVector3(subTemp[j]));
            }
            ret.Add(path);
        }

        return ret;
    }
    #endregion
}
