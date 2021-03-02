using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//一个协程管理类，什么协程都由它来执行，减少Mono的数量
//同时也防止了由于GameObject销毁而协程停止
public class CoroutineMgr : MonoSingleton<CoroutineMgr>
{
    public void Execute(IEnumerator routine)
    {
        StartCoroutine(routine);
    }
}
