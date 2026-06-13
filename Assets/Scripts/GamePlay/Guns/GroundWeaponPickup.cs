using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 地面武器拾取物组件
/// 
/// 挂载在场景中可见的武器模型上，玩家靠近并交互时可以捡起。
/// 它保存了被丢弃武器的完整状态数据（ID、类型、弹药），捡起时由 PlayerWeaponManager 读取。
/// </summary>
public class GroundWeaponPickup : MonoBehaviour
{
    [Header("武器信息（由 PlayerWeaponManager.DropCurrentWeapon 自动写入）")]
    public string weaponId;            // 武器唯一标识（对应 GunBase.GetWeaponId()）
    public WeaponType weaponType;      // 武器类型（决定装入哪个槽位）
    public int savedCurrentAmmo;       // 丢弃时弹夹内的弹药
    public int savedTotalAmmo;         // 丢弃时的总备用弹药

    [Header("可视化设置")]
    [Tooltip("悬浮旋转动画速度（度/秒）")]
    [SerializeField] private float rotateSpeed = 60f;
    [Tooltip("悬浮动画幅度")]
    [SerializeField] private float floatAmplitude = 0.15f;
    [Tooltip("悬浮动画速度")]
    [SerializeField] private float floatSpeed = 1.5f;

    private Vector3 startPosition;
    private float spawnTime;


    #region 初始化

    /// <summary>
    /// 由 PlayerWeaponManager.SpawnGroundPickup 调用，写入武器数据
    /// </summary>
    public void Setup(string id, WeaponType type, int currentAmmo, int totalAmmo)
    {
        this.weaponId = id;
        this.weaponType = type;
        this.savedCurrentAmmo = currentAmmo;
        this.savedTotalAmmo = totalAmmo;

        startPosition = transform.position;
        spawnTime = Time.time;

        Debug.Log($"[GroundWeaponPickup] 初始化: {id} ({type}), 弹药 {currentAmmo}/{totalAmmo}");
    }

    #endregion


    #region 可视化效果

    void Update()
    {
        // 悬浮旋转动画
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);

        // 上下浮动
        if (floatAmplitude > 0f)
        {
            float offset = Mathf.Sin((Time.time - spawnTime) * floatSpeed) * floatAmplitude;
            transform.position = startPosition + Vector3.up * offset;
        }
    }

    #endregion


    #region 拾取交互

    /// <summary>
    /// 当玩家触发拾取时调用（由交互系统调用）
    /// </summary>
    public void OnPickedUp(PlayerWeaponManager picker)
    {
        if (picker == null) return;

        Debug.Log($"[GroundWeaponPickup] 被捡起: {weaponId}");

        // 委托给 PlayerWeaponManager 处理所有逻辑（槽位判断、交换、弹药恢复等）
        picker.PickupWeapon(this);
    }

    #endregion


    #region 调试

    void OnDrawGizmosSelected()
    {
        // 在 Scene 视图中绘制拾取范围提示
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 1.5f);
    }

    #endregion
}
