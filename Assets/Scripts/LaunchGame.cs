using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
        DOTween.SetTweensCapacity(1000, 50);
        LifeCycleMgr.Single.Init();
        UIManager.Single.Show(Paths.PREFAB_START_VIEW);
        AudioMgr.Single.PlayBGM(Paths.AUDIO_TITLE_BGM);
        GameStateModel.Single.CurrentScene = SceneName.Main;
        GameStateModel.Single.IsPause = true;               //在UI界面中游戏是默认暂停的

        DontDestroyOnLoad(gameObject);
    }
}
