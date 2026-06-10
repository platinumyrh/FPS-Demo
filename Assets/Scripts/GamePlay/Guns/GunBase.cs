using System.Collections;
using System.Collections.Generic;
using UnityEditor.UIElements;
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
    private GunAnimationController gunAnimController;


    [Header("枪械属性")]
    private int currentAmmoInClip; // 当前弹夹中的子弹数量
    private int totalAmmo; // 总弹药数量
    private int maxAmmoInClip; // 弹夹容量
    [SerializeField] protected float weaponDamage = 25f; // 子弹伤害
    [SerializeField] protected float fireRange = 100f;   // 最大射程
    [SerializeField] protected LayerMask hitLayers = 1<<0;     // 射线可以击中的层级（防止自己打自己）

    [Header("开火相关")]
    //private float fireRate; // 射速（每秒发射的子弹数量）
    [SerializeField] private Transform firePoint; // 枪口位置，用于实例化子弹或播放射击特效

    [Header("特性弹道设置 (新)")]
    [Tooltip("单次开火射出的弹丸数量。普通枪为1，散弹枪/霰弹枪可设为8-12")]
    [SerializeField] protected int pelletCount = 1;

    [Tooltip("子弹散射角度。0表示绝对精准，数值越大扩散范围越大（建议范围0.01 - 0.15）")]
    [SerializeField] protected float spreadAngle = 0.02f;

    private void Awake()
    {
        gunAnimController = GetComponent<GunAnimationController>();
        firePoint = FindChildDeep(transform, "SOCKET_Muzzle");

        if (firePoint == null)
        {
            Debug.LogError($"[GunBase] 在 {gameObject.name} 的固定路径下未找到 SOCKET_Muzzle，请检查拼写！");
        }
    }
   

    // 【核心改动】：取消事件总线订阅！改由外部普通公共函数让 PlayerController 调用
    public virtual void FireWeapon()
    {
        // 执行减少弹药等逻辑...

        // 播放枪械自身的动画
        if (gunAnimController != null)
        {
            gunAnimController.PlayShoot();
        }

        ExcuteShotgunSpread();
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
            Debug.Log($"[命中] {hit.collider.name}，落点: {hit.point}");

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
        if (gunAnimController != null)
        {
            gunAnimController.PlayReload();
        }
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
