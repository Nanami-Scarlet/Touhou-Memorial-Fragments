using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public static class ExendUtil
{
    public static T Add<T>(this Transform trans) where T : Component
    {
        return trans.gameObject.AddComponent<T>();
    }
}
