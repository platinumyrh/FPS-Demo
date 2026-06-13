﻿﻿﻿using System.Collections;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;

public enum WeaponType
{
    Primary,    // 主武器 (如步枪、散弹枪)
    Secondary,  // 副武器 (如手枪)
    Special     // 特殊武器 (如火箭筒、雷、近战)
}



/// <summary>
/// 所有枪械的基类，包含了枪械的基本属性和方法，如射击、换弹等。具体的枪械类型（如步枪、手枪、狙击枪等）可以继承这个基类，并实现各自特有的功能。
/// 还提供枪械本身相关动画的animator和玩家握持枪械时的动画控制器PlayerAnimationController的引用，方便子类调用和控制枪械动画。
/// </summary>
public class GunBase : MonoBehaviour
{
    [Header("动画设置")]
    [Tooltip("拖入该枪械对应的 Animator Override Controll   er (如 OC_LPSP_PCH_RL_01)")]
    [SerializeField] private AnimatorOverrideController weaponOverrideController;
    private GunAnimationController gunAnimController;


    [Header("枪械属性")]
    private int currentAmmoInClip; // 当前弹夹中的子弹数量
    private int maxAmmoInClip; // 弹夹容量
    private int totalAmmo; // 总弹药数量
   
    [SerializeField] protected float weaponDamage = 25f; // 子弹伤害
    [SerializeField] protected float fireRange = 100f;   // 最大射程
    [SerializeField] protected LayerMask hitLayers = 1<<0;     // 射线可以击中的层级（防止自己打自己）

    public bool isReloading { get; private set; } = false; // 是否正在换弹
    public bool isInspecting { get; private set; } = false; // 新增：是否正在检视

    public bool isEmpty => currentAmmoInClip <= 0; // 是否弹夹空了

    [Header("开火相关")]
    //private float fireRate; // 射速（每秒发射的子弹数量）
    [SerializeField] private Transform firePoint; // 枪口位置，用于实例化子弹或播放射击特效

    [Header("特性弹道设置 (新)")]
    [Tooltip("单次开火射出的弹丸数量。普通枪为1，散弹枪/霰弹枪可设为8-12")]
    [SerializeField] protected int pelletCount = 1;

    [Tooltip("子弹散射角度。0表示绝对精准，数值越大扩散范围越大（建议范围0.01 - 0.15）")]
    [SerializeField] protected float spreadAngle = 0.02f;


    [Header("武器类型（决定该枪归入主/副/特殊槽位）")]
    [Tooltip("Primary=主武器(步枪/霰弹/SMG)  Secondary=副武器(手枪)  Special=特殊武器(火箭筒/狙击/近战)")]
    [SerializeField] protected WeaponType weaponType = WeaponType.Primary;

    [Header("UI 显示图标设置")]
    [SerializeField] protected Sprite iconWeaponBody;
    [SerializeField] protected Sprite iconGrip;
    [SerializeField] protected Sprite iconMagazine;
    [SerializeField] protected Sprite iconLaser;
    [SerializeField] protected Sprite iconMuzzle;
    [SerializeField] protected Sprite iconScope;

    


    protected void Awake()
    {
        gunAnimController = GetComponent<GunAnimationController>();
        firePoint = FindChildDeep(transform, "SOCKET_Muzzle");

        if (firePoint == null)
        {
            Debug.LogError($"[GunBase] 在 {gameObject.name} 的固定路径下未找到 SOCKET_Muzzle，请检查拼写！");
        }

       
        maxAmmoInClip = 30; // 假设每个弹夹30发
        currentAmmoInClip = maxAmmoInClip; // 初始化时弹夹装满
        totalAmmo = 90; // 假设总弹药90发（3个弹夹）

    }
    #region 数据封装与访问
    public int GetCurrentAmmoInClip() => currentAmmoInClip;

    public int GetMaxAmmoInClip() => maxAmmoInClip;

    public int GetTotalAmmo() => totalAmmo;

    public WeaponType GetWeaponType() => weaponType;

    /// <summary>
    /// 武器唯一标识（用于武器库模式中匹配"地上 Pickup"和"身上库中的枪"）。
    /// 默认使用 GameObject.name，子类可覆盖。
    /// </summary>
    public virtual string GetWeaponId() => gameObject.name;

    /// <summary>
    /// 从外部数据恢复弹药状态（捡枪时由 PlayerWeaponManager 调用）
    /// </summary>
    public void RestoreAmmoState(int currentAmmo, int totalAmmo)
    {
        this.currentAmmoInClip = Mathf.Clamp(currentAmmo, 0, maxAmmoInClip);
        this.totalAmmo = Mathf.Max(0, totalAmmo);
        Debug.Log($"[GunBase] {gameObject.name} 弹药已恢复: {currentAmmoInClip}/{maxAmmoInClip}, 总计: {totalAmmo}");
    }

    #endregion

    // 提供一个公共方法，用来一键打包当前枪械的所有 UI 数据
    public WeaponUIData CreateUIData()
    {
        return new WeaponUIData(
            gameObject.name,
            currentAmmoInClip,
            maxAmmoInClip,
            totalAmmo,
            iconWeaponBody,
            iconGrip,
            iconMagazine,
            iconLaser,
            iconMuzzle,
            iconScope
        );
    }

    // 【核心改动】：取消事件总线订阅！改由外部普通公共函数让 PlayerController 调用
    public virtual void FireWeapon()
    {
        // 执行减少弹药等逻辑...
        if (currentAmmoInClip != 0)
        {
            currentAmmoInClip--;
            Debug.Log($"[射击] {gameObject.name} 开火！剩余弹夹内子弹: {currentAmmoInClip}/{maxAmmoInClip}，总弹药: {totalAmmo}");

            // 播放枪械自身的动画
            if (gunAnimController != null)
            {
                gunAnimController.PlayShoot();
            }

            ExcuteShotgunSpread();
        }
        else
        {
            Debug.Log($"[射击] {gameObject.name} 弹夹空了！请换弹！");
            // 可以在这里播放空弹夹的提示音效或动画
        }
        // 在 GunBase.cs 射击、切枪、换弹成功的数据结算完后：
        WeaponUIData uiData = new WeaponUIData(gameObject.name, currentAmmoInClip, maxAmmoInClip, totalAmmo);
        GameEventBus.GetInstance().Publish<WeaponUIData>(GameEventType.OnWeaponUIUpdate, uiData);


    }


    /// <summary>
    /// 处理多弹丸和弹道散布的核心逻辑
    /// </summary>
    private void ExcuteShotgunSpread()
    {
        if (firePoint == null) return;

        Vector3 baseForward = -firePoint.up; // 基础射线方向（枪口朝向）

        // 寻找与枪口方向垂直的两个轴（右方向和上方向），用于构建二维散射平面
        // 因为基础方向是 -up，所以我们用 forward 和 right 作为平面的基底
        Vector3 rightAxis = firePoint.right;
        Vector3 upAxis = firePoint.forward;

        for (int i = 0; i < pelletCount; i++)
        {
            Vector3 shootDirection = baseForward;

            if (spreadAngle > 0f)
            {
                Vector2 randomPoint = Random.insideUnitCircle; // 在单位圆内随机一个点
                shootDirection+=rightAxis * randomPoint.x * spreadAngle; // 水平散布
                shootDirection+=upAxis * randomPoint.y * spreadAngle;    // 垂直散布

                shootDirection.Normalize(); // 归一化，确保射线方向正确
            }
            SingleRaycastHit(shootDirection);
        }



    }



    /// <summary>
    /// 执行单发子弹的射线检测
    /// </summary>
    private void SingleRaycastHit(Vector3 direction)
    {
        Ray ray = new Ray(firePoint.position, direction);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, fireRange, hitLayers))
        {
           // Debug.Log($"[命中] {hit.collider.name}，落点: {hit.point}");

            // 绘制绿色击中辅助线
            DrawDebugLaser(ray.origin, hit.point, Color.green, 0.5f);

            // 广播事件
            BulletHitEventData hitData = new BulletHitEventData(hit, weaponDamage, this);
            GameEventBus.GetInstance().Publish<BulletHitEventData>(GameEventType.OnBulletHit, hitData);
        }
        else
        {
            // 绘制红色未击中辅助线
            Vector3 endPoint = ray.origin + (direction * fireRange);
            DrawDebugLaser(ray.origin, endPoint, Color.red, 0.5f);
        }
    }


    public virtual void ReloadWeapon()
    {
        // 拦截：如果正在换弹、正在检视、子弹满了、或者没后备弹药，则不触发
        if (isReloading || isInspecting || totalAmmo <= 0 || currentAmmoInClip == maxAmmoInClip) return;

        //Debug.Log($"[GunBase] 开始换弹动画");
        isReloading = true;

        if (gunAnimController != null)
        {
            gunAnimController.PlayReload(isEmpty);
        }
    }

    public virtual void InspectWeapon()
    {
        // 拦截：如果正在换弹或已经在检视，则不触发
        if (isReloading || isInspecting) return;

        Debug.Log($"[GunBase] 开始检视武器");
        isInspecting = true;

        if (gunAnimController != null)
        {
            gunAnimController.PlayInspect();
        }
    }
    public virtual void OnReloadComplete()
    {
        if (!isReloading) return; // 安全锁

        int ammoNeeded = maxAmmoInClip - currentAmmoInClip;
        int ammoToReload = Mathf.Min(ammoNeeded, totalAmmo);

        currentAmmoInClip += ammoToReload;
        totalAmmo -= ammoToReload;

        isReloading = false; // 解开锁，允许再次开火
        Debug.Log($"[GunBase] 换弹数据结算完毕！当前子弹：{currentAmmoInClip}/{maxAmmoInClip}");

        // 通知 UI 更新弹药显示
        WeaponUIData uiData = new WeaponUIData(gameObject.name, currentAmmoInClip, maxAmmoInClip, totalAmmo);
        GameEventBus.GetInstance().Publish<WeaponUIData>(GameEventType.OnWeaponUIUpdate, uiData);
    }


    /// <summary>
    /// 新增：由外部（如 PlayerController）在开局或切枪完毕时调用，强行刷新一次 UI 默认数据
    /// </summary>
    public void InitDefaultUIData()
    {
        // 拼装当前的初始数据，推送到事件总线
        WeaponUIData uiData = new WeaponUIData(gameObject.name, currentAmmoInClip, maxAmmoInClip, totalAmmo);
        GameEventBus.GetInstance().Publish<WeaponUIData>(GameEventType.OnWeaponUIUpdate, uiData);
    }

    public AnimatorOverrideController GetWeaponOverrideController() => weaponOverrideController;



    #region 辅助方法
    /// <summary>
    /// 场景视图射线绘制辅助方法
    /// </summary>
    /// <param name="start">起点（枪口）</param>
    /// <param name="end">终点（击中点或最大距离处）</param>
    /// <param name="color">线条颜色</param>
    /// <param name="duration">在线条在场景中维持显示的时间（秒）</param>
    private void DrawDebugLaser(Vector3 start, Vector3 end, Color color, float duration)
    {
        // Debug.DrawLine 需要传入具体的起点和终点
        Debug.DrawLine(start, end, color, duration);
    }


    // 递归查找子节点的方法
    private Transform FindChildDeep(Transform parent, string targetName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == targetName) return child;
            Transform result = FindChildDeep(child, targetName);
            if (result != null) return result;
        }
        return null;
    }
    #endregion


}
