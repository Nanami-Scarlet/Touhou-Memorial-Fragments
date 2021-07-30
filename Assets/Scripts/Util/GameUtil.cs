using System.Collections.Generic;
using UnityEngine;

public class GameUtil
{
    /// <summary>
    /// 激活位于index的子物体
    /// </summary>
    public static Transform SetSubActive(Transform trans, int index)
    {
        for (int i = 0; i < trans.childCount; ++i)
        {
            if (i == index)
            {
                trans.GetChild(i).gameObject.SetActive(true);
                continue;
            }
            trans.GetChild(i).gameObject.SetActive(false);
        }

        return trans.GetChild(index);
    }

    public static Transform SetSubActive(List<Transform> trans, int index)
    {
        for (int i = 0; i < trans.Count; ++i)
        {
            if (i == index)
            {
                trans[i].gameObject.SetActive(true);
                continue;
            }
            trans[i].gameObject.SetActive(false);
        }

        return trans[index];
    }

    /// <summary>
    /// 玩家可移动判断边界
    /// </summary>
    /// <returns></returns>
    public static bool JudgeBorderUp(Vector3 pos)
    {
        return pos.y <= 3.9f;
    }

    public static bool JudgeBorderDown(Vector3 pos)
    {
        return pos.y >= -4.3f;
    }

    public static bool JudgeBorderLeft(Vector3 pos)
    {
        return pos.x >= -4.9f ;
    }

    public static bool JudgeBorderRight(Vector3 pos)
    {
        return pos.x <= 3.2f;
    }

    public static bool JudgePrecision(Vector3 pos)
    {
        return pos.y <= -4f;
    }

    /// <summary>
    /// 仅供数组专用
    /// </summary>
    /// <returns></returns>
    public static int GetManaLevel()
    {
        return PlayerModel.Single.Mana / 100 - 1;
    }

    public static float GetDistance(Vector3 pos1, Vector3 pos2)
    {
        return (pos1 - pos2).magnitude;
    }

    public static float GetDistance(Transform trans1, Transform trans2)
    {
        return GetDistance(trans1.position, trans2.position);
    }

    /// <summary>
    /// 妖精可发射弹幕的范围
    /// </summary>
    public static bool JudgeEnemyShot(Vector3 pos)
    {
        return pos.x > -4.5f && pos.x < 2.7f && pos.y > -3.5f && pos.y < 4.5f;
    }

    public static bool JudgeBoundary(Vector3 pos)
    {
        return pos.x > -5.1f && pos.x < 3.4f && pos.y > -4.7f && pos.y < 4.7f; 
    }
}
