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

    // ====== 新增：用来接管并存储当前各活跃面板对应的纯 C# Controller 实例 ======
    private Dictionary<string, object> controllerDic = new Dictionary<string, object>();

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
                // ====== 核心注入：在 View 实例化完毕的一瞬间，为其匹配并绑定其专属 Controller ======
                BindControllerForPanel(panelName, panel);
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

    /// <summary>
    /// 关闭面板
    /// 核心重构：在彻底销毁 UI 游戏对象前，必须先调用并卸载其对应的 Controller，防止内存泄漏！
    /// </summary>
    public void ClosePanel(string panelName)
    {
        if (panelDic.TryGetValue(panelName, out BasePanel panel))
        {
            // 1. 优先解除并销毁 C# 控制器，触发其 UnsubscribeAllEvents
            if (controllerDic.TryGetValue(panelName, out object controllerObj))
            {
                // 使用反射动态寻找并执行它的 Destroy 方法，彻底断开 EventBus 订阅与 Mono 监听
                var destroyMethod = controllerObj.GetType().GetMethod("Destroy");
                destroyMethod?.Invoke(controllerObj, null);

                // 从接管字典移除
                controllerDic.Remove(panelName);
            }

            // 2. 物理销毁游戏场景内的 UI 物体
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

    /// <summary>
    /// 控制器注入路由：根据面板预制体的路径/名字，在这里 new 出它专属的 C# Controller
    /// </summary>
    private void BindControllerForPanel(string panelName, BasePanel panel)
    {
        // 路由匹配：如果加载的是武器面板
        if (panelName == "P_LPSP_UI_Canvas" && panel is WeaponStatusPanel weaponView)
        {
            // 1. 动态生成控制器（其内部会自动创建与之绑定的 WeaponModel）
            WeaponUIController weaponController = new WeaponUIController(weaponView);

            // 2. 框架约束：必须手动调用 Init() 来开始让它干活、绑定事件、绑定 MonoManager
            weaponController.Init();

            // 3. 丢进字典中统一接管
            controllerDic[panelName] = weaponController;
        }

        // 以后你每增加一个新系统面板（例如背包 Panel），就在这里加一条路由：
        // else if (panelName == "UI/BagPanel" && panel is BagPanel bagView) { ... }
    }
}