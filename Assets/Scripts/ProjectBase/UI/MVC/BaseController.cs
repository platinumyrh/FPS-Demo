using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
/// <summary>
/// MVC — Controller 层基类
/// 
/// 职责：
///  - 纯 C# 类，不继承 MonoBehaviour
///  - 连接 View 和 Model 的桥梁
///  - 处理所有业务逻辑
///  - 绑定 View 的控件事件 → 转发到业务方法
///  - 调用 Model 读写数据
///  - 调用 View 更新显示
///  - 通过 GameEventBus 与其他模块通信
///  
/// 使用方式：
///  - 继承 BaseController&lt;TView, TModel&gt;
///  - TView: 对应的 View 类型（继承 BasePanel）
///  - TModel: 对应的 Model 类型（继承 BaseModel）
///  - 在 Init() 中绑定事件、初始化数据、订阅总线事件
///  
/// 约束：
///  - ❌ 不直接 GetControl 操作控件（通过 View 的方法）
///  - ❌ 不直接 btn.onClick 绑定（通过 View 提供的 AddXxxListener 方法）
///  - ❌ 不直接存储业务数据（数据放 Model）
/// </summary>
public class BaseController<TView, TModel> where TView : BasePanel where TModel : BaseModel
{

    // ===== 引用 =====

    protected TView view;
    protected TModel model;

    // 标记是否已初始化，防止重复初始化
    protected bool isInitialized = false;


    // ===== 构造 =====

    /// <summary>
    /// 构造函数：传入 View 和 Model 的引用
    /// 通常在 UIManager.ShowPanel 中创建 Controller 时调用
    /// </summary>
    /// <param name="view">对应的 View 面板</param>
    /// <param name="model">对应的数据模型，传 null 则自动创建</param>
    public BaseController(TView view, TModel model = null)
    {
        this.view = view;
        this.model = model ?? Activator.CreateInstance<TModel>();
    }

    /// <summary>
    /// 初始化控制器
    /// 在面板加载完成后由 UIManager 或外部调用
    /// 子类必须重写此方法来：
    ///   1. 绑定 View 的控件事件
    ///   2. 初始化 Model 数据
    ///   3. 订阅 GameEventBus 事件
    ///   4. 初始刷新 View 显示
    /// </summary>
    public virtual void Init()
    {
        if (isInitialized) return;
        isInitialized = true;
    }

    /// <summary>
    /// 销毁控制器
    /// 面板关闭时调用，做清理工作：
    ///   1. 取消 GameEventBus 事件订阅
    ///   2. 清理 Model
    /// </summary>
    public virtual void Destroy()
    {
        UnsubscribeAllEvents();

        if (model != null)
        {
            model.Dispose();
            model = null;
        }

        view = null;
        isInitialized = false;
    }

    // ===== 辅助方法 =====

    /// <summary>
    /// 取消所有 GameEventBus 事件订阅
    /// 子类如果有订阅 Bus 事件，应该重写此方法来取消订阅
    /// </summary>
    protected virtual void UnsubscribeAllEvents()
    {
        // 子类重写，取消自己在 Init 中订阅的所有事件
        // 例如：
        // GameEventBus.GetInstance().Unsubscribe<XXXData>(GameEventType.XXX, OnXXX);
    }

    /// <summary>
    /// 安全地执行操作（判空保护）
    /// </summary>
    protected void SafeInvoke(UnityAction action)
    {
        action?.Invoke();
    }

}
