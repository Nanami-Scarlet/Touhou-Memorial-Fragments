using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Const
{
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
