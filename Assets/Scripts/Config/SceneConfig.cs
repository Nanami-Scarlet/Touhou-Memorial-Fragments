using DG.Tweening;
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
            PoolMgr.Single.Init();

            GameObject root = new GameObject("GameRoot");
            root.AddComponent<GameRoot>().Init();
        });

        SceneMgr.Single.AddSceneLoad(SceneName.Main, () =>
        {
            DOTween.KillAll();

            UIManager.Single.Show(Paths.PREFAB_START_VIEW);     //这里为了重放动画，上面全部把动画都杀掉了
        });
    }
}
