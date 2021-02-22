using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneConfig : NormalSingleton<SceneConfig>, IInit
{
    public void Init()
    {
        SceneMgr.Single.AddSceneLoad(SceneName.Game, (Action callback) =>
        {
            GameRoot.Single.Init();
            callback();
        });
    }
}
