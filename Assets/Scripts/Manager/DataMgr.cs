using LitJson;
using System.Collections;
using System.ComponentModel;
using System.Collections.Generic;
using UnityEngine;

public class DataMgr : NormalSingleton<DataMgr>
{
    private Dictionary<string, StageData> _dicNameStageData = new Dictionary<string, StageData>();

    public void Init()
    {
        InitEnemyConfig(Paths.CONFIG_ENEMY);
    }

    #region EnemyData
    private void InitEnemyConfig(string path)
    {
        TextAsset config = LoadMgr.Single.LoadConfig(path);

        JsonData json = JsonMapper.ToObject(config.text);

        for(Stage i = Stage.stage1_1; i < Stage.COUNT; ++i)
        {
            JsonData stage = json[i.ToString()];
            StageData stageData = new StageData()
            {
                Name = i.ToString(),
                WaveCount = stage.Count,
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

                    EnemyData enemyData = new EnemyData(
                        enemyType, delay, GetVector3(strPos), GetPath(strPath), pauseTime,
                        pathDurUP, pathDurDown);

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
}
