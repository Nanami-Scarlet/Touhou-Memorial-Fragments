using BulletPro;
using LitJson;
using System.Collections;
using System.ComponentModel;
using System.Collections.Generic;
using UnityEngine;
using System;

public class DataMgr : NormalSingleton<DataMgr>, IInit
{
    private Dictionary<string, StageData> _dicNameStageData = new Dictionary<string, StageData>();
    private Dictionary<string, Dictionary<string, EmitterProfile>> _dicSceneBullet = new Dictionary<string, Dictionary<string, EmitterProfile>>();
    private Dictionary<AudioType, AudioData> _dicTypeAudio = new Dictionary<AudioType, AudioData>();

    public void Init()
    {
        InitEmitData();
        InitAudioData();

        InitStageConfig(Paths.CONFIG_ENEMY);
    }

    #region EnemyConfig
    private void InitStageConfig(string path)
    {
        TextAsset config = LoadMgr.Single.LoadConfig(path);

        JsonData json = JsonMapper.ToObject(config.text);

        for(Stage i = Stage.stage1_1; i < Stage.COUNT; ++i)
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
                        enemyType, delay, GetVector3(strPos), GetPath(strPath), pauseTime,
                        pathDurUP, pathDurDown, emitter, hp, pCount, pointCount);

                    waveData.ListEnemy.Add(enemyData);
                }

                stageData.ListWaveEnemy.Add(waveData);
            }
            _dicNameStageData.Add(i.ToString(), stageData);
        }
    }

    public StageData GetStageData(string name)
    {
        if (!_dicNameStageData.ContainsKey(name))
        {
            Debug.LogError("没有加载该场景中的数据，场景名为：" + name);
            return null;
        }

        return _dicNameStageData[name];
    }
    #endregion

    #region EmitData
    private void InitEmitData()
    {
        for (Stage i = Stage.stage1_1; i < Stage.COUNT; ++i)
        {
            _dicSceneBullet[i.ToString()] = new Dictionary<string, EmitterProfile>();

            EmitterProfile[] emitters = LoadMgr.Single.LoadAll<EmitterProfile>(Paths.ASSET_BULLET_FOLDER + i.ToString());
            for (int j = 0; j < emitters.Length; ++j)
            {
                _dicSceneBullet[i.ToString()].Add(emitters[j].name, emitters[j]);
            }
        }
    }

    private EmitterProfile GetEmit(string stageName, string name)
    {
        if (!_dicSceneBullet[stageName].ContainsKey(name))
        {
            Debug.LogError("在面" + stageName + "不存在该弹幕，弹幕名为：" + name);
            return null;
        }

        return _dicSceneBullet[stageName][name];
    }
    #endregion

    private void InitAudioData()
    {
        TextAsset config = LoadMgr.Single.LoadConfig(Paths.CONFIG_AUDIO);

        JsonData json = JsonMapper.ToObject(config.text);

        for(int i = 0; i < json.Count; ++i)
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

    private List<List<Vector3>> GetPath(string strPath)
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
