using BulletPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameProcessMgr : MonoBehaviour, IUpdate
{
    private EnemySpawnMgr _spawnMgr;

    public void Init()
    {
        _spawnMgr = gameObject.AddComponent<EnemySpawnMgr>();
        _spawnMgr.Init();

        //CoroutineMgr.Single.Execute(OnState("stage1_1"));
        StartCoroutine(OnState("stage1_1"));
    }

    public void UpdateFun()
    {

    }

    private void OnDestroy()
    {
        GameModel.Single.EnemyCount = 0;

        //TimeMgr.Single.ClearAllTask();
    }

    private IEnumerator OnState(string stateName)
    {
        StageData stageData = DataMgr.Single.GetStageData(stateName);

        MessageMgr.Single.DispatchMsg(MsgEvent.EVENT_STAGE);

        yield return new WaitForSeconds(4f);

        for (int i = 0; i < stageData.ListWaveEnemy.Count; ++i)
        {
            WaveData waveData = stageData.ListWaveEnemy[i];
            float delay = 0;            //累加的延迟

            for (int j = 0; j < waveData.ListEnemy.Count; ++j)
            {
                EnemyData enemyData = waveData.ListEnemy[j];

                TimeMgr.Single.AddTimeTask(() =>
                {
                    if (GameStateModel.Single.CurrentScene == SceneName.Game)
                    {
                        _spawnMgr.Spawn(enemyData);
                    }
                }, delay, TimeUnit.Second);

                ++GameModel.Single.EnemyCount;

                delay += enemyData.Delay;
            }

            while (GameModel.Single.EnemyCount > 0)
            {
                yield return new WaitForEndOfFrame();
            }
        }
    }
}
