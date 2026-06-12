using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 武器 UI 控制器（纯 C# 类）
/// </summary>
public class WeaponUIController : BaseController<WeaponStatusPanel,WeaponModel>
{
    private float lowAmmoFlashTimer = 0f; // 借用 Mono 帧更新做特效的示例变量

    // 构造函数：基类会通过反射自动 new 出泛型对应的 WeaponModel
    public WeaponUIController(WeaponStatusPanel view, WeaponModel model = null) : base(view, model)
    {
    }

    /// <summary>
    /// 控制器激活枢纽
    /// </summary>
    public override void Init()
    {
        base.Init();

        // 1. 监听自己 Model 的数据变化通知 -> 去驱动 View 刷新
        model.OnDataChanged += RefreshView;

        // 2. 订阅全局战斗事件总线（监听到 GunBase 开火、换弹、切枪时发出的通知）
        GameEventBus.GetInstance().Subscribe<WeaponUIData>(GameEventType.OnWeaponUIUpdate, OnWeaponDataReceived);

        // 3. 联动你的 MonoManager！让纯 C# 类拥有每帧更新的能力（例如制作低弹药呼吸闪烁特效）
        MonoManager.GetInstance().AddUpdateListener(UpdateFlashEffect);

        


        MonoManager.GetInstance().StartCoroutine(DelayedPullInitialData());
    }


    /// <summary>
    /// 延迟到下一帧再拉取初始数据，确保 WeaponStatusPanel.Start() 已执行完毕
    /// </summary>
    private System.Collections.IEnumerator DelayedPullInitialData()
    {
        yield return null;  // 等一帧，让 Unity 的 Start() 生命周期先跑完
        TryPullInitialWeaponData();
    }
    /// <summary>
    /// 主动从 PlayerController 拉取当前武器的初始数据，解决初始化时序竞态
    /// </summary>
    private void TryPullInitialWeaponData()
    {
        var playerCtrl = Object.FindObjectOfType<PlayerController>();
        // 确保场景里有玩家，且玩家手里有枪
        if (playerCtrl != null && playerCtrl.currentWeapon != null)
        {
            // 修复：利用 CreateUIData 拿到包含图片的完整结构，解决第一帧 UI 缺失图片问题
            WeaponUIData data = playerCtrl.currentWeapon.CreateUIData();
            model.SetWeaponData(data);
        }
    }

    /// <summary>
    /// 当监听到枪械系统发送的数据变更事件
    /// </summary>
    private void OnWeaponDataReceived(WeaponUIData data)
    {
        if (data == null || model == null) return;

        // Controller 将数据安全解包，喂给自己的 Model
        model.SetWeaponData(data);
    }

    /// <summary>
    /// 读取 Model 层的最新安全数据，直接刷给 View
    /// </summary>
    private void RefreshView()
    {
        if (view != null && model != null)
        {
            view.UpdateDisplay(model.WeaponName, model.CurrentAmmo, model.MaxAmmoInClip, model.TotalAmmo,model.WeaponBodySprite,model.GripSprite,
                model.MagzineSprite,model.LazerSprite,model.MuzzleSprite,model.ScopeSprite);
        }
    }

    /// <summary>
    /// 借助 MonoManager 驱动的帧更新函数
    /// </summary>
    private void UpdateFlashEffect()
    {
        if (model == null || view == null) return;

        // 示例逻辑：如果子弹低于20%，利用时间让 UI 产生一点小特效（比如动态闪烁）
        if (model.CurrentAmmo <= model.MaxAmmoInClip * 0.2f && model.CurrentAmmo > 0)
        {
            lowAmmoFlashTimer += Time.deltaTime;
            // 可以在这里扩展更丰富的动画表现，或者利用 MonoManager.StartCoroutine 启动一个平滑淡出协程
        }
    }


    /// <summary>
    /// 核心清理防错：重写基类解绑
    /// </summary>
    protected override void UnsubscribeAllEvents()
    {
        base.UnsubscribeAllEvents();

        // 1. 解绑 Model 变化监听
        if (model != null)
        {
            model.OnDataChanged -= RefreshView;
        }

        // 2. 极其重要：务必在总线中取消订阅！否则面板关闭后，射击仍会触发此方法导致空引用报错
        GameEventBus.GetInstance().Unsubscribe<WeaponUIData>(GameEventType.OnWeaponUIUpdate, OnWeaponDataReceived);

        // 3. 移除 MonoManager 帧更新监听
        MonoManager.GetInstance().RemoveUpdateListener(UpdateFlashEffect);
    }

    public override void Destroy()
    {
        // 调用基类的 Destroy，会自动触发 UnsubscribeAllEvents，并 Dispose 掉 model
        base.Destroy();
    }
}
