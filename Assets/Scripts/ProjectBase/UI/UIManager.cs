using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum UI_Layer
{ 
    Bottom,
    Top,
    Middle,
    System,


}
/// <summary>
/// UI管理器
/// 管理所有显示面板
/// 为外部提供显示 隐藏等接口
/// </summary>
public class UIManager : BaseManager<UIManager>
{
    public Dictionary<string, BasePanel> panelDic = new Dictionary<string, BasePanel>();

    private Transform canvas;

    private Transform bottom;
    private Transform top;
    private Transform middle;
    private Transform system;

    public UIManager()
    {
        // 加载Canvas
        GameObject obj = ResManager.GetInstance().Load<GameObject>("UI/Canvas");
        if (obj != null)
        {
            GameObject canvasInstance = GameObject.Instantiate(obj);  // ✅ 实例化
            canvas = canvasInstance.transform;
            GameObject.DontDestroyOnLoad(canvasInstance);

            // 找到各层
            bottom = canvas.Find("Bottom");
            top = canvas.Find("Top");
            middle = canvas.Find("Middle");
            system = canvas.Find("System");
        }

        // 加载EventSystem（只需加载一次）
        obj = ResManager.GetInstance().Load<GameObject>("UI/EventSystem");
        if (obj != null)
        {
            GameObject.DontDestroyOnLoad(obj);
        }
    }

    public void ShowPanel(string panelName, UI_Layer layer)
    {
        // 避免重复加载
        if (panelDic.ContainsKey(panelName))
        {
            panelDic[panelName].Show();
            return;
        }

        ResManager.GetInstance().LoadAsync<GameObject>(panelName, (obj) =>
        {
            if (obj == null)
            {
                Debug.LogError($"加载面板失败：{panelName}");
                return;
            }

            // 实例化
            GameObject panelInstance = GameObject.Instantiate(obj);
            panelInstance.name = panelName; // 方便调试

            // 设置父物体
            Transform father = GetLayerTransform(layer);
            panelInstance.transform.SetParent(father);

            // 重置变换
            ResetTransform(panelInstance.transform);

            // 存储Panel脚本
            BasePanel panel = panelInstance.GetComponent<BasePanel>();
            if (panel != null)
            {
                panelDic[panelName] = panel;
            }
        });
    }

    private Transform GetLayerTransform(UI_Layer layer)
    {
        switch (layer)
        {
            case UI_Layer.Bottom: return bottom;
            case UI_Layer.Top: return top;
            case UI_Layer.Middle: return middle;
            case UI_Layer.System: return system;
            default: return bottom;
        }
    }

    private void ResetTransform(Transform trans)
    {
        trans.localPosition = Vector3.zero;
        trans.localScale = Vector3.one;

        RectTransform rect = trans as RectTransform;
        if (rect != null)
        {
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
        }
    }

    public void HidePanel(string panelName)
    {

        if (panelDic.TryGetValue(panelName, out BasePanel panel))
        {
            panel.Hide();
        }


        //Debug.Log($"HidePanel 被调用，面板名：{panelName}");
        //Debug.Log($"panelDic 中是否有：{panelDic.ContainsKey(panelName)}");

        //if (panelDic.TryGetValue(panelName, out BasePanel panel))
        //{
        //    Debug.Log($"找到面板，调用 Hide()");
        //    panel.Hide();
        //}
        //else
        //{
        //    Debug.LogWarning($"面板 {panelName} 不在 panelDic 中！");
        //    // 打印所有已加载的面板
        //    foreach (var key in panelDic.Keys)
        //    {
        //        Debug.Log($"已加载的面板：{key}");
        //    }
        //}
    }

    public void ClosePanel(string panelName)
    {
        if (panelDic.TryGetValue(panelName, out BasePanel panel))
        {
            GameObject.Destroy(panel.gameObject);
            panelDic.Remove(panelName);
        }
    }

    public T GetPanel<T>(string panelName)
        where T : BasePanel
    {
        if (panelDic.TryGetValue(panelName, out BasePanel panel))
        {
            return panel as T;
        }

        Debug.LogWarning($"GetPanel 找不到面板: {panelName}");
        return null;
    }
}