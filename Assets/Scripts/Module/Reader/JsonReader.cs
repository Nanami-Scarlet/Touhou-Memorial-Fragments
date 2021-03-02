using LitJson;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class JsonReader : IReader
{
    private Queue<KeyQueue> _queKeys = new Queue<KeyQueue>();
    private KeyQueue _keys;
    private JsonData _tempData;
    private JsonData _data;

    private bool SetKey<T>(T key)
    {
        if (_data == null || _keys != null)
        {
            if (_keys == null)
            {
                _keys = new KeyQueue();
            }
            Key keyData = new Key();
            keyData.Set(key);
            _keys.Enqueue(keyData);

            return true;
        }

        return false;
    }

    public IReader this[string key]
    {
        get
        {
            if (!SetKey(key))
            {
                try
                {
                    _tempData = _tempData[key];
                }
                catch(Exception e)
                {
                    Debug.LogError("数据无法转化，数据：" + _tempData.ToJson() + "键值：" + key);
                    Debug.LogError(e.ToString());
                }
            }

            return this;
        }
    }

    public IReader this[int key]
    {
        get
        {
            if (!SetKey(key))
            {
                try
                {
                    _tempData = _tempData[key];
                }
                catch
                {
                    Debug.LogError("数据无法转化，数据：" + _tempData.ToJson() + "键值：" + key);
                }
            }

            return this;
        }
    }

    public void Get<T>(Action<T> callback)
    {
        if (_keys != null)
        {
            _keys.OnComplete(tempData =>
            {
                var value = GetValue<T>(tempData);
                ResetData();
                callback(value);
            });

            _queKeys.Enqueue(_keys);
            _keys = null;
            ExecuteKeysQueue();
            return;
        }

        if (callback == null)
        {
            Debug.LogWarning("Get的回调函数为空");
            ResetData();
            return;
        }

        var data = GetValue<T>(_tempData);
        ResetData();
        callback(data);
    }

    public void SetData(string data)
    {
        _data = JsonMapper.ToObject(data);
        ResetData();            //重新设置值，_data自然也要重新设置
        ExecuteKeysQueue();
    }

    public void Count(Action<int> callback)
    {
        bool hasData = SetKey<Action>(() =>
        {
            if (callback != null)
            {
                callback(GetCount());
            }
        });

        if (!hasData)
        {
            callback(GetCount());
        }
        else
        {
            _queKeys.Enqueue(_keys);
            _keys = null;
        }

    }

    private void ResetData()    //每次读数据时，把数据拉回全局，然后通过标识符读取
    {
        _tempData = _data;
    }

    private void ExecuteKeysQueue()
    {
        if (_data == null)
        {
            return;
        }

        IReader reader = null;
        foreach (KeyQueue keyQueue in _queKeys)
        {
            foreach (var value in keyQueue)
            {
                if (value is int)
                {
                    reader = this[(int)value];
                }
                else if (value is string)
                {
                    reader = this[(string)value];
                }
                else if (value is Action)
                {
                    ((Action)value)();
                }
                else
                {
                    Debug.LogError("数据类型错误");
                }
            }

            keyQueue.Complete(_tempData);
        }

        _queKeys.Clear();
    }

    private T GetValue<T>(JsonData data)
    {
        var converter = TypeDescriptor.GetConverter(typeof(T));

        if (converter.CanConvertTo(typeof(T)))
        {
            return (T)converter.ConvertTo(data.ToString(), typeof(T));
        }

        return (T)(object)data;
    }

    private int GetCount()
    {
        return _tempData.IsArray ? _tempData.Count : 0;
    }
}

class Key
{
    private object _key;

    public object Get()
    {
        return _key;
    }

    public void Set<T>(T key)
    {
        _key = key;
    }
}

class KeyQueue : IEnumerable
{
    private Queue<Key> _queKey;
    private Action<JsonData> _complete;

    public KeyQueue()
    {
        _queKey = new Queue<Key>();
        _complete = null;
    }

    public void Enqueue(Key key)
    {
        _queKey.Enqueue(key);
    }

    public Key Dequeue()
    {
        return _queKey.Dequeue();
    }

    public void OnComplete(Action<JsonData> complete)
    {
        _complete = complete;
    }

    public void Complete(JsonData data)
    {
        if (_complete != null)
        {
            _complete(data);
        }
    }

    public IEnumerator GetEnumerator()
    {
        foreach (Key key in _queKey)
        {
            yield return key.Get();
        }
    }
}
