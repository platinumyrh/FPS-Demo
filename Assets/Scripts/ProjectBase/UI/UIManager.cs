using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum UI_Layer
{
    Bottom,
    Top,
    Middle,
    System,
}

/// <summary>
/// UI管理器
/// </summary>
public class UIManager : BaseManager<UIManager>
{
    public Dictionary<string, BasePanel> panelDic = new Dictionary<string, BasePanel>();
    private Dictionary<string, GameObject> simplePanelDic = new Dictionary<string, GameObject>();


    // 接管并存储当前各活跃面板对应的纯 C# Controller 实例
    private Dictionary<string, object> controllerDic = new Dictionary<string, object>();

    private Transform canvas;

    private Transform bottom;
    private Transform top;
    private Transform middle;
    private Transform system;

    public UIManager()
    {
        // 1. 同步加载主大 Canvas 容器
        GameObject obj = ResManager.GetInstance().Load<GameObject>("UI/P_LPSP_UI_Canvas");
        if (obj != null)
        {
            GameObject canvasInstance = GameObject.Instantiate(obj);

            // 🚨 【核心修正 1】：强行洗掉 (Clone) 后缀，确保名字绝对纯净
            canvasInstance.name = "P_LPSP_UI_Canvas";

            canvas = canvasInstance.transform;

            // 强行正位，防止继承场景里的畸变
            canvas.SetParent(null);
            canvas.localPosition = Vector3.zero;
            canvas.localRotation = Quaternion.identity;
            canvas.localScale = Vector3.one;

            GameObject.DontDestroyOnLoad(canvasInstance);

            // 2. 提取并注入 MVC 控制器
            BasePanel panel = canvasInstance.GetComponent<BasePanel>();
            if (panel != null)
            {
                // 用规范化后的纯净名字作为 Key 存入字典
                panelDic[canvasInstance.name] = panel;

                // 此时匹配进去的就是 "P_LPSP_UI_Canvas"，完美契合路由！
                BindControllerForPanel(canvasInstance.name, panel);
            }

            // 3. 寻找层级容器
            bottom = canvas.Find("Bottom");
            top = canvas.Find("Top");
            middle = canvas.Find("Middle");
            system = canvas.Find("System");

            
        }

        // 加载EventSystem
        obj = ResManager.GetInstance().Load<GameObject>("UI/EventSystem");
        if (obj != null)
        {
            GameObject eventSystemInstance = GameObject.Instantiate(obj);
            eventSystemInstance.transform.SetParent(null);
            GameObject.DontDestroyOnLoad(eventSystemInstance);
        }
    }

    public void ShowPanel(string panelName, UI_Layer layer)
    {
        // 🚨 【核心修正 2】：智能截取纯文件名（比如把 "UI/P_LPSP_UI_Canvas" 或 "P_LPSP_UI_Canvas" 统一变成 "P_LPSP_UI_Canvas"）
        string purePanelName = panelName.Contains("/") ? panelName.Substring(panelName.LastIndexOf('/') + 1) : panelName;

        // 拦截机制：如果主大 Canvas 已经在构造函数里初始化过了，直接 Show，拒绝二次重复创建！
        if (panelDic.ContainsKey(purePanelName))
        {
            panelDic[purePanelName].Show();
            return;
        }

        // 补全路径防御机制：如果传入的名字不带文件夹前缀，自动帮它补上 "UI/" 路径再加载
        string realLoadPath = panelName.StartsWith("UI/") ? panelName : "UI/" + panelName;

        ResManager.GetInstance().LoadAsync<GameObject>(realLoadPath, (obj) =>
        {
            if (obj == null)
            {
                Debug.LogError($"[UIManager] 异步加载面板失败，请检查 Resources 下是否存在该路径: {realLoadPath}");
                return;
            }

            GameObject panelInstance = GameObject.Instantiate(obj);
            panelInstance.name = purePanelName; // 使用纯净名字命名

            // 设置父物体
            Transform father = GetLayerTransform(layer);
            panelInstance.transform.SetParent(father);

            // 重置变换
            ResetTransform(panelInstance.transform);

            // 存储 Panel 并绑定 Controller
            BasePanel panel = panelInstance.GetComponent<BasePanel>();
            if (panel != null)
            {
                panelDic[purePanelName] = panel;
                BindControllerForPanel(purePanelName, panel);
            }
        });
    }

    /// <summary>加载一个简单的非 MVC 面板（不做 ResetTransform，不绑定 Controller）</summary>
    public GameObject ShowSimplePanel(string panelName, UI_Layer layer)
    {
        // 避免重复加载
        if (simplePanelDic.ContainsKey(panelName))
        {
            simplePanelDic[panelName].SetActive(true);
            return simplePanelDic[panelName];
        }

        string realLoadPath = panelName.StartsWith("UI/") ? panelName : "UI/" + panelName;
        GameObject obj = ResManager.GetInstance().Load<GameObject>(realLoadPath);

        if (obj == null)
        {
            Debug.LogError($"[UIManager] 加载简单面板失败: {realLoadPath}");
            return null;
        }

        GameObject instance = GameObject.Instantiate(obj);
        Transform father = GetLayerTransform(layer);

        // false = 保持预制体原始 RectTransform 尺寸
        instance.transform.SetParent(father, false);

        simplePanelDic[panelName] = instance;
        return instance;
    }


    private Transform GetLayerTransform(UI_Layer layer)
    {
        switch (layer)
        {
            case UI_Layer.Bottom: return bottom ?? canvas;
            case UI_Layer.Top: return top ?? canvas;
            case UI_Layer.Middle: return middle ?? canvas;
            case UI_Layer.System: return system ?? canvas;
            default: return bottom ?? canvas;
        }
    }

    private void ResetTransform(Transform trans)
    {
        trans.localPosition = Vector3.zero;
        trans.localRotation = Quaternion.identity;
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
        string purePanelName = panelName.Contains("/") ? panelName.Substring(panelName.LastIndexOf('/') + 1) : panelName;
        if (panelDic.TryGetValue(purePanelName, out BasePanel panel))
        {
            panel.Hide();
        }
    }

    public void ClosePanel(string panelName)
    {
        string purePanelName = panelName.Contains("/") ? panelName.Substring(panelName.LastIndexOf('/') + 1) : panelName;
        if (panelDic.TryGetValue(purePanelName, out BasePanel panel))
        {
            if (controllerDic.TryGetValue(purePanelName, out object controllerObj))
            {
                var destroyMethod = controllerObj.GetType().GetMethod("Destroy");
                destroyMethod?.Invoke(controllerObj, null);
                controllerDic.Remove(purePanelName);
            }

            GameObject.Destroy(panel.gameObject);
            panelDic.Remove(purePanelName);
        }
    }
    /// <summary>隐藏简单面板（不销毁）</summary>
    public void HideSimplePanel(string panelName)
    {
        if (simplePanelDic.TryGetValue(panelName, out GameObject panel))
        {
            panel.SetActive(false);
        }
    }

    /// <summary>关闭并销毁简单面板</summary>
    public void CloseSimplePanel(string panelName)
    {
        if (simplePanelDic.TryGetValue(panelName, out GameObject panel))
        {
           GameObject.Destroy(panel);
            simplePanelDic.Remove(panelName);
        }
    }
    

    private void BindControllerForPanel(string panelName, BasePanel panel)
    {
        // 🚨 【核心修正 3】：此时传进来的 panelName 必然是完全去除了路径和 (Clone) 的 "P_LPSP_UI_Canvas"
        if (panelName == "P_LPSP_UI_Canvas" && panel is WeaponStatusPanel weaponView)
        {
            WeaponUIController weaponController = new WeaponUIController(weaponView);
            weaponController.Init();
            controllerDic[panelName] = weaponController;

           // Debug.Log($"<color=green>[MVC 成功注入]</color> 初始大Canvas: {panelName} 已完美绑定 WeaponUIController！");
        }
    }
}