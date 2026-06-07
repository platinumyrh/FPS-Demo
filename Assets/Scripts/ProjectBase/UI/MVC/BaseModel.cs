using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// MVC — Model 层基类
/// 
/// 职责：
///  - 纯数据类，不继承 MonoBehaviour
///  - 存储面板所需的数据
///  - 提供数据的读写接口和校验逻辑
///  
/// 使用方式：
///  - 继承 BaseModel，在子类中定义具体的数据字段
///  - Controller 通过调用 Model 的方法来读写数据
///  
/// 约束：
///  - ❌ 不碰 UI 控件
///  - ❌ 不调用 Unity API
///  - ❌ 不发送 GameEventBus 事件（由 Controller 发）
/// </summary>
public class BaseModel
{
    /// <summary>
    /// 数据发生变化时的回调（可选）
    /// Controller 可以监听这个，在数据变化时更新 View
    /// 如果不需要可以不使用
    /// </summary>
    public event Action OnDataChanged;
    /// <summary>
    /// 通知数据已变化
    /// 子类在修改数据后调用此方法
    /// </summary>
    protected void NotifyDataChanged()
    {
        OnDataChanged?.Invoke();
    }
    /// <summary>
    /// 重置模型数据到初始状态
    /// 子类可 override 实现自己的重置逻辑
    /// </summary>
    public virtual void Reset()
    {
        // 子类按需重写
    }

    /// <summary>
    /// 清理资源（如果 Model 持有需要释放的资源）
    /// </summary>
    public virtual void Dispose()
    {
        OnDataChanged = null;
    }
}
