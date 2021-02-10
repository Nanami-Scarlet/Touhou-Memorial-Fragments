using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NormalSingleton<T> where T : class, new()
{
    private static T _single = null;

    public static T Single
    {
        get
        {
            if (_single == null)
            {
                T t = new T();

                if(t is MonoBehaviour)
                {
                    Debug.LogError("该类是MonoBehavior类，请使用MonoBehavior单例");
                    return null;
                }
                _single = t;
            }

            return _single;
        }
    }
}
