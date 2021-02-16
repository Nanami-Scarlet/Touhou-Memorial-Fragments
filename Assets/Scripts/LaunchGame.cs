using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaunchGame : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        LifeCycleMgr.Single.Init();
        UIManager.Single.Show(Paths.PREFAB_START_VIEW);
        AudioMgr.Single.PlayBGM(Const.TITLE_BGM);
        GameStateModel.Single.CurrentScene = SceneName.Main;
    }
}
