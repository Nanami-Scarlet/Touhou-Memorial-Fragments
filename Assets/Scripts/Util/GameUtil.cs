using System.Collections;
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

    /// <summary>
    /// 判断边界
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
}
