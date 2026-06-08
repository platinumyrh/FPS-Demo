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
    [Tooltip("拖入该枪械对应的 Animator Override Controller (如 OC_LPSP_PCH_RL_01)")]
    [SerializeField] private AnimatorOverrideController weaponOverrideController;

    /// <summary>
    /// 公开接口：获取当前枪械的动画重写控制器
    /// </summary>
    public AnimatorOverrideController GetWeaponOverrideController()
    {
        return weaponOverrideController;
    }

}
