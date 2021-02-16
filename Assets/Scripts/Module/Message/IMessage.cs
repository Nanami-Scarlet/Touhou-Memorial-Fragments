using System;

public interface IMessage
{
    void AddListener(int key, Action<object[]> callback);
    void RemoveListener(int key, Action<object[]> callback);
    void DispatchMsg(int key, params object[] args);

    void AddListener(string key, Action<object[]> callback);
    void RemoveListener(string key, Action<object[]> callback);
    void DispatchMsg(string key, params object[] args);
}
