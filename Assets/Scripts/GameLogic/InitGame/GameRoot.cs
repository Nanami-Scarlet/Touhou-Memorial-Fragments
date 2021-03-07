using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameRoot : MonoBehaviour, IInit
{
    public void Init()      //加载场景进行必要的初始化
    {
        DataMgr.Single.Init();

        gameObject.AddComponent<GameProcessMgr>().Init();


        GameObject player = LoadMgr.Single.LoadPrefabAndInstantiate(Paths.PREFAB_PLAYER);
        player.GetComponent<PlayerView>().Init();
        player.GetComponent<PlayerController>().Init();


        //todo:预初始化两个UI--PauseView、CheatView
        UIManager.Single.PreLoad(Paths.PREFAB_PAUSE_VIEW);
    }
}
