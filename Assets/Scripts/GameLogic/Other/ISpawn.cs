using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISpawn
{
    void Init();
    GameObject Spawn(EntityData data);
}
