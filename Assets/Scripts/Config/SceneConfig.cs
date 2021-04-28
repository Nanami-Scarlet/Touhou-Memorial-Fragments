using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneConfig : NormalSingleton<SceneConfig>, IInit
{
    public void Init()
    {
        SceneMgr.Single.AddSceneLoad(SceneName.Game, () =>
        {
            //todo:预初始化两个UI--PauseView、CheatView
            UIManager.Single.PreLoad(Paths.PREFAB_PAUSE_VIEW);
            UIManager.Single.PreLoad(Paths.PREFAB_DYNAMIC_VIEW);

            PoolMgr.Single.Init();

            GameObject root = new GameObject("GameRoot");
            root.AddComponent<GameRoot>().Init();
        });

        //SceneMgr.Single.AddSceneLoad(SceneName.Main, () =>
        //{
        //    PoolMgr.Single.ClearPool();
        //    TimeMgr.Single.ClearAllTask();
        //});
    }
}
