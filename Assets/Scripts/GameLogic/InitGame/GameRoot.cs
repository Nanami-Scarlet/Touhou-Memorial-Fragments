using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameRoot : NormalSingleton<GameRoot>, IInit
{
    public GameRoot()
    {

    }

    public void Init()      //加载场景进行必要的初始化
    {
        GameObject player = LoadMgr.Single.LoadPrefabAndInstantiate(Paths.PREFAB_PLAYER);
        player.GetComponent<PlayerView>().Init();
        player.GetComponent<PlayerController>().Init();


        //todo:预初始化两个UI--PauseView、CheatView
        UIManager.Single.PreLoad(Paths.PREFAB_PAUSE_VIEW);
    }
}
