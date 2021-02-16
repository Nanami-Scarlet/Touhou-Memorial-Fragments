using UnityEngine;

public interface IInput
{
    void AddListener(KeyCode key);
    void RemoveListener(KeyCode key);
}
