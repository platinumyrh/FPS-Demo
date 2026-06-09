using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 所有枪械的基类，包含了枪械的基本属性和方法，如射击、换弹等。具体的枪械类型（如步枪、手枪、狙击枪等）可以继承这个基类，并实现各自特有的功能。
/// 还提供枪械本身相关动画的animator和玩家握持枪械时的动画控制器PlayerAnimationController的引用，方便子类调用和控制枪械动画。
/// </summary>
public class GunBase : MonoBehaviour
{
    [Header("动画设置")]
    [Tooltip("拖入该枪械对应的 Animator Override Controll   er (如 OC_LPSP_PCH_RL_01)")]
    [SerializeField] private AnimatorOverrideController weaponOverrideController;
    [SerializeField] private Animator  gunAnimator; // 枪械本身的 Animator 组件


    [Header("枪械属性")]
    private int currentAmmoInClip; // 当前弹夹中的子弹数量
    private int totalAmmo; // 总弹药数量
    private int maxAmmoInClip; // 弹夹容量

    private void OnEnable()
    {
        gunAnimator = GetComponent<Animator>();
        GameEventBus.GetInstance().Subscribe<InputActionData>(GameEventType.OnShoot,OnShoot);
        GameEventBus.GetInstance().Subscribe<InputActionData>(GameEventType.OnReload, OnReload);
        GameEventBus.GetInstance().Subscribe<InputActionData>(GameEventType.OnAim, OnAim);
        GameEventBus.GetInstance().Subscribe<InputActionData>(GameEventType.OnInspect, OnInspect);
    }

    public virtual void OnShoot(InputActionData data)
    {
        // 实现射击逻辑，如减少弹药、播放射击动画等
        Debug.Log($"射击事件触发，动作名称：{data.ActionName}");
    }

    public virtual void OnReload(InputActionData data)
    {
        // 实现换弹逻辑，如补充弹药、播放换弹动画等
        //Debug.Log($"换弹事件触发，动作名称：{data.ActionName}");
        gunAnimator.CrossFade("Reload", 0.1f); // 平滑过渡到换弹动画
    }
    public virtual void OnAim(InputActionData data)
    {
        // 实现瞄准逻辑，如调整枪械位置、播放瞄准动画等
        //Debug.Log($"瞄准事件触发，动作名称：{data.ActionName}");
         
    }
    public virtual void OnInspect(InputActionData data)
    {
        // 实现检查枪械逻辑，如播放检查动画等
        Debug.Log($"检查枪械事件触发，动作名称：{data.ActionName}");
    }

    /// <summary>
    /// 公开接口：获取当前枪械的动画重写控制器
    /// </summary>
    public AnimatorOverrideController GetWeaponOverrideController()
    {
        return weaponOverrideController;
    }

}
