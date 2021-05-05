using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameRoot : MonoBehaviour
{
    public void Init()      //加载场景进行必要的初始化
    {
        GameModel.Single.StageNum = 0;      //todo:暂时这么处理，不可能总是从一面进游戏
        GameModel.Single.Score = 0;
        PlayerModel.Single.Mana = 100;       //todo:这里也不是总是从100开始
        PlayerModel.Single.State = PlayerState.NORMAL;
        PlayerModel.Single.MemoryProcess = 0;
        PlayerModel.Single.MemoryFragment = 0;

        if (GameStateModel.Single.SelectedDegree == Degree.NORMAL)
        {
            PlayerModel.Single.Life = 2;
            PlayerModel.Single.Bomb = 3;
            PlayerModel.Single.Init_Point = 10000;
        }
        else
        {
            PlayerModel.Single.Life = 7;
            PlayerModel.Single.Bomb = 5;
            PlayerModel.Single.Init_Point = 20000;
        }

        UIManager.Single.Hide(Paths.PREFAB_PLAYER_VIEW);
        UIManager.Single.Show(Paths.PREFAB_GAME_VIEW);
        UIManager.Single.Show(Paths.PREFAB_DYNAMIC_VIEW);

        gameObject.AddComponent<GameProcessMgr>().Init();

        GameObject player = LoadMgr.Single.LoadPrefabAndInstantiate(Paths.PREFAB_PLAYER);
        LoadMgr.Single.LoadPrefabAndInstantiate(Paths.PREFAB_BULLET_SETTING);
        player.GetComponent<PlayerView>().Init();
        player.GetComponent<PlayerController>().Init();
        player.GetComponent<PlayerBehaviour>().Init();

        //AudioMgr.Single.PlayBGM(Paths.AUDIO_ONE_ONE_BGM);
    }
}
