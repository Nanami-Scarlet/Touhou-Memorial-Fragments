using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIUtil : MonoBehaviour
{
    private Dictionary<string, UIData> _dicUIData;

    public void Init()
    {
        _dicUIData = new Dictionary<string, UIData>();

        foreach(RectTransform rect in transform)
        {
            string name = rect.name;
            _dicUIData.Add(name, new UIData(rect));
        }
    }

    public UIData Get(string name)
    {
        if (!_dicUIData.ContainsKey(name))
        {
            Transform trans = transform.Find(name);
            _dicUIData.Add(name, new UIData(trans));
        }

        return _dicUIData[name];
    }
}

public class UIData
{
    public Text Txt { get; }
    public Image Img { get; }
    public GameObject GO { get; }

    public UIData(Transform trans)
    {
        Txt = trans.GetComponent<Text>();
        Img = trans.GetComponent<Image>();
        GO = trans.gameObject;
    }

    public void SetSprite(Sprite sprite)
    {
        if(Img == null)
        {
            Debug.LogError("该物体不存在Image组件，物体名称为：" + GO.name);
            return;
        }

        Img.sprite = sprite;
    }

    public void SetText(int num)
    {
        SetText(num.ToString());
    }

    public void SetText(string text)
    {
        if(Txt == null)
        {
            Debug.LogError("该物体不存在Text组件，物体名称为：" + GO.name);
            return;
        }

        Txt.text = text;
    }

    public T Get<T>() where T : MonoBehaviour
    {
        return GO.GetComponent<T>();
    }

    public T Add<T>() where T : MonoBehaviour
    {
        return GO.AddComponent<T>();
    }

    public Transform GetTrans()
    {
        return GO.transform;
    }
}
