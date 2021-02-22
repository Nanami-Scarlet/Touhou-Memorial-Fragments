using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Const
{
    /// <summary>
    /// view的优先级
    /// </summary>
    public const int BIND_PREFAB_PRIORITY_VIEW = 0;
    /// <summary>
    /// controller的优先级
    /// </summary>
    public const int BIND_PREFAB_PRIORITY_CONTROLLER = 1;

    /// <summary>
    /// 一些音频的名称
    /// </summary>
    public const string TITLE_BGM = "Title_BGM";
    public const string ONE_ONE_BGM = "One_One_BGM";
    public const string SELECT_EFF = "Select_Eff";
    public const string SURE_EFF = "Sure_Eff";
    public const string CANCAL_EFF = "Cancal_Eff";
    public const string PAUSE_EFF = "Pause_Eff";


    public static Vector3 BORN_POS = new Vector3(-0.7f, -3.5f, 0);
    public static Vector3 DEAD_POS = new Vector3(-0.7f, -5.5f, 0);

    public static Color ColorSelect
    {
        get
        {
            return Color.white;
        }
    }

    public static Color ColorUnSelect
    {
        get
        {
            return Color.black;
        }
    }
}
