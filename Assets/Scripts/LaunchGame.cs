using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaunchGame : MonoBehaviour
{
    private void Start()
    {
        if (FindObjectsOfType<LaunchGame>().Length > 1)     //防止多次被实例化单例
        {
            Destroy(gameObject);
            return;
        }

        Init();
    }

    private void Init()
    {
        LifeCycleMgr.Single.Init();
        UIManager.Single.Show(Paths.PREFAB_START_VIEW);
        AudioMgr.Single.PlayBGM(Const.TITLE_BGM);
        GameStateModel.Single.CurrentScene = SceneName.Main;
        GameStateModel.Single.Status = GameStatus.Pause;

        DontDestroyOnLoad(gameObject);
    }
}
