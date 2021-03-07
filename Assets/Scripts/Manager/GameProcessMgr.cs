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

        CoroutineMgr.Single.Execute(OnState("stage1_1"));
        GameModel.Single.StageNum = 0;      //todo:暂时这么处理，不可能总是从一面进游戏
    }

    public void UpdateFun()
    {

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
                    _spawnMgr.Spawn(enemyData);
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
