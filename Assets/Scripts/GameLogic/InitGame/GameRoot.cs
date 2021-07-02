using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameRoot : MonoBehaviour
{
    public void Init()      //加载场景进行必要的初始化
    {
        GameStateModel.Single.GameMode = Mode.NORMAL;      //todo:这里不可能总是普通模式
        GameStateModel.Single.IsPause = false;
        GameModel.Single.StageNum = 0;      //todo:不可能总是从一面进游戏
        GameModel.Single.Score = 0;
        PlayerModel.Single.Mana = 100;       //todo:这里也不是总是从100开始
        PlayerModel.Single.State = PlayerState.NORMAL;
        PlayerModel.Single.MemoryProcess = 0;
        PlayerModel.Single.MemoryFragment = 0;

        if (GameStateModel.Single.GameDegree == Degree.NORMAL)
        {
            PlayerModel.Single.Life = 2;
            PlayerModel.Single.Bomb = 7;
            PlayerModel.Single.MAX_GET_POINT = 10000;
        }
        else
        {
            PlayerModel.Single.Life = 7;
            PlayerModel.Single.Bomb = 2;
            PlayerModel.Single.MAX_GET_POINT = 20000;
        }

        UIManager.Single.Hide(Paths.PREFAB_PLAYER_VIEW);
        UIManager.Single.Show(Paths.PREFAB_GAME_VIEW);
        UIManager.Single.Show(Paths.PREFAB_DYNAMIC_VIEW);

        //LoadMgr.Single.LoadPrefabAndInstantiate(Paths.PREFAB_BULLET_SETTING);
        gameObject.AddComponent<GameProcessMgr>().Init();

        GameObject player = LoadMgr.Single.LoadPrefabAndInstantiate(Paths.PREFAB_PLAYER);
        player.GetComponent<PlayerView>().Init();
        player.GetComponent<PlayerController>().Init();
        player.GetComponent<PlayerBehaviour>().Init();
    }
}
