﻿﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 武器管理器 — 武器库模式
/// 
/// 核心设计：
///   - 玩家身上 P_LPSP_INVENTORY 下挂有所有枪的 GameObject（常驻，不销毁）
///   - 三槽位（Primary/Secondary/Special）只持有引用，指向库中的枪
///   - 切枪 = SetActive 切换（保留挂点、配件等所有引用）
///   - 丢枪 = 槽位置空 + 在地面生成 GroundWeaponPickup（记录武器ID和弹药数据）
///   - 捡枪 = 根据 Pickup 的武器ID 找到库中对应枪 → RestoreAmmoState → 装入槽位 → 激活
/// </summary>
public class PlayerWeaponManager : MonoBehaviour
{
    #region 三槽位（仅持有引用，不拥有对象）

    private GunBase primarySlot;    // 主武器槽位
    private GunBase secondarySlot;  // 副武器槽位
    private GunBase specialSlot;    // 特殊武器槽位

    /// <summary>当前激活的槽位类型</summary>
    private WeaponType currentSlotType = WeaponType.Primary;

    /// <summary>当前手持的武器（对外只读）</summary>
    public GunBase CurrentWeapon => GetSlot(currentSlotType);

    #endregion

    #region 武器库引用

    [Header("武器库（P_LPSP_INVENTORY 下的所有枪，全部常驻不失活）")]
    [Tooltip("自动从 INVENTORY 子物体中收集所有 GunBase，也可手动指定")]
    [SerializeField] private List<GunBase> weaponLibrary = new List<GunBase>();

    /// <summary>武器ID → GunBase 的查找表（运行时构建）</summary>
    private Dictionary<string, GunBase> libraryLookup = new Dictionary<string, GunBase>();

    #endregion

    #region 开局默认装备

    [Header("开局默认装备（从 weaponLibrary 中选择）")]
    [Tooltip("开局时自动激活的主武器（留空则不装备）")]
    [SerializeField] private string defaultPrimaryId = "";
    [Tooltip("开局时自动激活的副武器（留空则不装备）")]
    [SerializeField] private string defaultSecondaryId = "";
    [Tooltip("开局时自动激活的特殊武器（留空则不装备）")]
    [SerializeField] private string defaultSpecialId = "";

    #endregion

    #region 地面拾取物预制体

    [Header("地面拾取物预制体（丢枪时实例化）")]
    [Tooltip("挂载了 GroundWeaponPickup 组件的预制体（TODO: 后续创建该预制体后拖入）")]
    [SerializeField] private GameObject groundPickupPrefab;

    #endregion

    private Animator playerAnimator;
    private bool isSwitching = false;
    private PlayerController playerController;

    #region 生命周期

    void Start()
    {
        playerAnimator = GetComponentInChildren<Animator>();
        playerController = GetComponent<PlayerController>();

        // 1. 构建武器库查找表
        BuildLibraryLookup();

        // 2. 将所有库中枪初始化为失活状态（除了后续要激活的）
        InitializeAllLibraryWeaponsInactive();

        // 3. 装备默认武器到对应槽位
        EquipDefaultWeapons();

        // 4. 激活第一个有武器的槽位
        ActivateFirstAvailableSlot();
    }

    private void OnEnable()
    {
        GameEventBus.GetInstance().Subscribe<InputActionData>(GameEventType.OnNextWeapon, OnNextWeaponTriggered);
        GameEventBus.GetInstance().Subscribe<InputActionData>(GameEventType.OnPrevWeapon, OnPrevWeaponTriggered);
    }

    private void OnDisable()
    {
        GameEventBus.GetInstance().Unsubscribe<InputActionData>(GameEventType.OnNextWeapon, OnNextWeaponTriggered);
        GameEventBus.GetInstance().Unsubscribe<InputActionData>(GameEventType.OnPrevWeapon, OnPrevWeaponTriggered);
    }

    #endregion

    #region 武器库管理

    /// <summary>构建 ID→GunBase 查找表</summary>
    private void BuildLibraryLookup()
    {
        libraryLookup.Clear();

        // 如果 Inspector 没有手动填，尝试自动从 INVENTORY 子物体收集
        if (weaponLibrary.Count == 0)
        {
            Transform inventory = transform.Find("P_LPSP_INVENTORY");
            if (inventory != null)
            {
                var allGuns = inventory.GetComponentsInChildren<GunBase>(true); // true = 包含失活的
                weaponLibrary = new List<GunBase>(allGuns);
                Debug.Log($"[PlayerWeaponManager] 自动收集到 {weaponLibrary.Count} 把武器");
            }
            else
            {
                Debug.LogWarning("[PlayerWeaponManager] 未找到 P_LPSP_INVENTORY，且 weaponLibrary 为空！");
            }
        }

        // 构建查找表
        foreach (var gun in weaponLibrary)
        {
            string id = gun.GetWeaponId();
            if (!libraryLookup.ContainsKey(id))
            {
                libraryLookup[id] = gun;
            }
            else
            {
                Debug.LogWarning($"[PlayerWeaponManager] 武器ID重复: '{id}' ({gun.gameObject.name})，跳过");
            }
        }

        Debug.Log($"[PlayerWeaponManager] 武器库已就绪，共 {libraryLookup.Count} 把唯一武器");
    }

    /// <summary>根据武器ID从库中查找 GunBase</summary>
    public GunBase FindInLibrary(string weaponId)
    {
        if (string.IsNullOrEmpty(weaponId)) return null;
        libraryLookup.TryGetValue(weaponId, out GunBase gun);
        return gun;
    }

    /// <summary>将库中所有枪设为失活状态</summary>
    private void InitializeAllLibraryWeaponsInactive()
    {
        foreach (var gun in weaponLibrary)
        {
            gun.gameObject.SetActive(false);
        }
    }

    /// <summary>检查某把枪是否已被某个槽位使用</summary>
    public bool IsGunEquipped(GunBase gun)
    {
        return gun != null && (gun == primarySlot || gun == secondarySlot || gun == specialSlot);
    }

    #endregion

    #region 槽位访问辅助

    public GunBase GetSlot(WeaponType type)
    {
        return type switch
        {
            WeaponType.Primary   => primarySlot,
            WeaponType.Secondary => secondarySlot,
            WeaponType.Special   => specialSlot,
            _ => null
        };
    }

    private void SetSlot(WeaponType type, GunBase gun)
    {
        switch (type)
        {
            case WeaponType.Primary:   primarySlot = gun; break;
            case WeaponType.Secondary: secondarySlot = gun; break;
            case WeaponType.Special:   specialSlot = gun; break;
        }
    }

    public bool HasWeaponInSlot(WeaponType type) => GetSlot(type) != null;

    /// <summary>获取所有非空槽位的类型列表（用于滚轮循环）</summary>
    private List<WeaponType> GetFilledSlots()
    {
        var list = new List<WeaponType>(3);
        if (primarySlot != null)   list.Add(WeaponType.Primary);
        if (secondarySlot != null) list.Add(WeaponType.Secondary);
        if (specialSlot != null)   list.Add(WeaponType.Special);
        return list;
    }

    private WeaponType GetNextFilledSlot(WeaponType current)
    {
        var filled = GetFilledSlots();
        if (filled.Count <= 1) return current;
        int idx = filled.IndexOf(current);
        return filled[(idx + 1) % filled.Count];
    }

    private WeaponType GetPrevFilledSlot(WeaponType current)
    {
        var filled = GetFilledSlots();
        if (filled.Count <= 1) return current;
        int idx = filled.IndexOf(current);
        return filled[(idx - 1 + filled.Count) % filled.Count];
    }

    #endregion

    #region 装备 / 激活核心

    /// <summary>将一把库中的枪装入指定槽位（仅设置引用，不处理显示）</summary>
    private void EquipToSlot(WeaponType type, GunBase gun)
    {
        if (gun == null)
        {
            Debug.LogWarning($"[PlayerWeaponManager] 尝试装入空枪到 {type} 槽位");
            return;
        }

        SetSlot(type, gun);
        Debug.Log($"[PlayerWeaponManager] 装备 {type}: {gun.gameObject.name}");
    }

    /// <summary>激活指定槽位的武器（切换动画 + 显示 + 数据绑定）</summary>
    private void ActivateSlot(WeaponType targetSlot)
    {
        GunBase newWeapon = GetSlot(targetSlot);
        if (newWeapon == null)
        {
            Debug.LogWarning($"[PlayerWeaponManager] 尝试激活空槽位: {targetSlot}");
            return;
        }

        // 隐藏当前手持的武器（如果不同）
        GunBase currentWeapon = GetSlot(currentSlotType);
        if (currentWeapon != null && currentWeapon != newWeapon)
        {
            currentWeapon.gameObject.SetActive(false);
        }

        // 更新当前槽位标记
        currentSlotType = targetSlot;

        // 显示新武器（它一直在 INVENTORY 下，只是之前失活了）
        newWeapon.gameObject.SetActive(true);

        // 绑定动画控制器
        AnimatorOverrideController overrider = newWeapon.GetWeaponOverrideController();
        if (overrider != null && playerAnimator != null)
        {
            playerAnimator.runtimeAnimatorController = overrider;
        }
        playerAnimator?.SetBool("Holstered", false);

        // 绑定到 PlayerController
        if (playerController != null)
        {
            playerController.currentWeapon = newWeapon;
        }

        // 推送 UI 全量数据（含图片）
        WeaponUIData uiData = newWeapon.CreateUIData();
        GameEventBus.GetInstance().Publish<WeaponUIData>(GameEventType.OnWeaponUIUpdate, uiData);

        Debug.Log($"[PlayerWeaponManager] ✓ 激活 {targetSlot}: {newWeapon.gameObject.name}");
    }

    /// <summary>开局装备默认武器</summary>
    private void EquipDefaultWeapons()
    {
        if (!string.IsNullOrEmpty(defaultPrimaryId))
        {
            GunBase gun = FindInLibrary(defaultPrimaryId);
            if (gun != null) EquipToSlot(WeaponType.Primary, gun);
            else Debug.LogWarning($"[PlayerWeaponManager] 默认主武器未找到: '{defaultPrimaryId}'");
        }

        if (!string.IsNullOrEmpty(defaultSecondaryId))
        {
            GunBase gun = FindInLibrary(defaultSecondaryId);
            if (gun != null) EquipToSlot(WeaponType.Secondary, gun);
            else Debug.LogWarning($"[PlayerWeaponManager] 默认副武器未找到: '{defaultSecondaryId}'");
        }

        if (!string.IsNullOrEmpty(defaultSpecialId))
        {
            GunBase gun = FindInLibrary(defaultSpecialId);
            if (gun != null) EquipToSlot(WeaponType.Special, gun);
            else Debug.LogWarning($"[PlayerWeaponManager] 默认特殊武器未找到: '{defaultSpecialId}'");
        }
    }

    /// <summary>激活第一个有武器的槽位</summary>
    private void ActivateFirstAvailableSlot()
    {
        if (primarySlot != null)   { ActivateSlot(WeaponType.Primary); return; }
        if (secondarySlot != null) { ActivateSlot(WeaponType.Secondary); return; }
        if (specialSlot != null)   { ActivateSlot(WeaponType.Special); return; }

        Debug.LogWarning("[PlayerWeaponManager] 开局没有任何武器！");
    }

    #endregion

    #region 切换武器（公开接口）

    /// <summary>按槽位类型切换（数字键 1/2/3 调用）</summary>
    public void SwitchToSlot(WeaponType targetSlot)
    {
        if (isSwitching) return;
        if (targetSlot == currentSlotType) return;
        if (GetSlot(targetSlot) == null) return;

        StartCoroutine(SwitchWeaponRoutine(targetSlot));
    }

    /// <summary>滚轮下一切换到下一个有武器的槽位</summary>
    public void SwitchToNext()
    {
        if (isSwitching) return;
        WeaponType next = GetNextFilledSlot(currentSlotType);
        if (next != currentSlotType) StartCoroutine(SwitchWeaponRoutine(next));
    }

    /// <summary>滚轮上一切换到上一个有武器的槽位</summary>
    public void SwitchToPrev()
    {
        if (isSwitching) return;
        WeaponType prev = GetPrevFilledSlot(currentSlotType);
        if (prev != currentSlotType) StartCoroutine(SwitchWeaponRoutine(prev));
    }

    #endregion

    #region 切枪协程

    private IEnumerator SwitchWeaponRoutine(WeaponType targetSlot)
    {
        if (targetSlot == currentSlotType || GetSlot(targetSlot) == null) yield break;

        isSwitching = true;

        // 收枪动画
        playerAnimator?.SetBool("Holstered", true);
        yield return new WaitForSeconds(0.5f);

        // 执行实际切换（纯 SetActive，不销毁不重建）
        ActivateSlot(targetSlot);

        // 拔枪动画
        playerAnimator?.SetBool("Holstered", false);
        yield return new WaitForSeconds(0.5f);

        isSwitching = false;
    }

    #endregion

    #region 输入事件回调

    private void OnNextWeaponTriggered(InputActionData data) => SwitchToNext();
    private void OnPrevWeaponTriggered(InputActionData data) => SwitchToPrev();

    #endregion

    #region 丢武器

    /// <summary>
    /// 丢弃当前手持武器
    /// 流程：读取当前枪的状态 → 在地面生成 Pickup → 槽位置空 → 切到下一把
    /// </summary>
    public void DropCurrentWeapon()
    {
        GunBase gun = GetSlot(currentSlotType);
        if (gun == null)
        {
            Debug.LogWarning("[PlayerWeaponManager] 当前槽位为空，无法丢弃");
            return;
        }

        if (isSwitching)
        {
            Debug.LogWarning("[PlayerWeaponManager] 正在切枪中，无法丢弃");
            return;
        }

        Debug.Log($"[PlayerWeaponManager] 丢弃武器: {gun.gameObject.name} ({currentSlotType})");

        // 1. 先隐藏当前武器
        gun.gameObject.SetActive(false);

        // 2. 在玩家前方地面生成拾取物（携带弹药数据）
        SpawnGroundPickup(gun);

        // 3. 清空槽位（注意：不 Destroy 枪对象！它还在库里，可以被再次捡起）
        ClearSlot(currentSlotType);

        // 4. 自动切换到下一个有武器的槽位
        ActivateFirstAvailableSlot();
    }

    /// <summary>丢弃指定槽位的武器（不切换当前手持）</summary>
    public void DropWeaponFromSlot(WeaponType slotType)
    {
        GunBase gun = GetSlot(slotType);
        if (gun == null) return;

        if (slotType == currentSlotType)
        {
            DropCurrentWeapon();
            return;
        }

        Debug.Log($"[PlayerWeaponManager] 丢弃 {slotType} 槽位: {gun.gameObject.name}");

        gun.gameObject.SetActive(false);
        SpawnGroundPickup(gun);
        ClearSlot(slotType);
    }

    /// <summary>
    /// 在玩家脚下的地面上生成一个 GroundWeaponPickup
    /// TODO: 需要 GroundWeaponPickup 组件和对应的预制体
    /// </summary>
    private void SpawnGroundPickup(GunBase gun)
    {
        if (groundPickupPrefab == null)
        {
            Debug.LogWarning("[PlayerWeaponManager] groundPickupPrefab 未设置，无法生成地面拾取物");
            return;
        }

        // 计算生成位置：玩家前方一点距离，贴地
        Vector3 spawnPos = transform.position + transform.forward * 1.5f + Vector3.up * 0.1f;

        GameObject pickupObj = Instantiate(groundPickupPrefab, spawnPos, Quaternion.Euler(0, Random.Range(0f, 360f), 0));

        // 将武器数据写入 Pickup（需要 GroundWeaponPickup 组件支持）
        var pickup = pickupObj.GetComponent<GroundWeaponPickup>();
        if (pickup != null)
        {
            pickup.Setup(
                gun.GetWeaponId(),           // 武器ID（用来匹配库中的枪）
                gun.GetWeaponType(),         // 武器类型
                gun.GetCurrentAmmoInClip(),  // 弹夹内弹药
                gun.GetTotalAmmo()           // 总备用弹药
            );
        }
        else
        {
            Debug.LogError("[PlayerWeaponManager] groundPickupPrefab 上没有 GroundWeaponPickup 组件！");
        }
    }

    private void ClearSlot(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.Primary:   primarySlot = null; break;
            case WeaponType.Secondary: secondarySlot = null; break;
            case WeaponType.Special:   specialSlot = null; break;
        }
    }

    #endregion

    #region 捡武器

    /// <summary>
    /// 捡起武器 — 武器库模式核心方法
    /// 
    /// 流程：
    ///   1. 根据 pickup.weaponId 从武器库中找到对应的 GunBase 对象
    ///   2. 检查目标槽位是否已有武器
    ///      - 空槽 → 直接装入
    ///      - 已有 + 交换模式 → 把旧的丢地上，装新的
    ///      - 已有 + 替换模式 → 直接覆盖
    ///   3. 用 pickup 里的弹药数据调用 gun.RestoreAmmoState()
    ///   4. 装入槽位并激活
    /// </summary>
    /// <param name="pickup">地面上的拾取物组件</param>
    /// <param name="swapIfOccupied">目标槽位已有武器时是否交换（默认 true）</param>
    public void PickupWeapon(GroundWeaponPickup pickup, bool swapIfOccupied = true)
    {
        if (pickup == null)
        {
            Debug.LogError("[PlayerWeaponManager] PickupWeapon: pickup 为 null");
            return;
        }

        // 1. 从库中查找对应的 GunBase
        GunBase targetGun = FindInLibrary(pickup.weaponId);
        if (targetGun == null)
        {
            Debug.LogError($"[PlayerWeaponManager] 武器库中找不到武器ID: '{pickup.weaponId}'，捡枪失败");
            return;
        }

        // 2. 检查这把枪是不是已经被其他槽位使用了
        if (IsGunEquipped(targetGun))
        {
            Debug.LogWarning($"[PlayerWeaponManager] {pickup.weaponId} 已经在装备中，无需重复捡起");
            return;
        }

        WeaponType targetType = pickup.weaponType;
        GunBase existing = GetSlot(targetType);

        // 3. 处理槽位冲突
        if (existing != null && swapIfOccupied)
        {
            // ===== 交换：旧武器掉地上 =====
            Debug.Log($"[PlayerWeaponManager] 交换 {targetType}: {existing.gameObject.name} → {targetGun.gameObject.name}");

            existing.gameObject.SetActive(false);
            SpawnGroundPickup(existing);
            ClearSlot(targetType);
        }
        else if (existing != null && !swapIfOccupied)
        {
            // ===== 替换：旧武器直接失活回库（不掉地上）=====
            Debug.Log($"[PlayerWeaponManager] 替换 {targetType}: {existing.gameObject.name} → {targetGun.gameObject.name}");
            existing.gameObject.SetActive(false);
            ClearSlot(targetType);
        }
        else
        {
            // ===== 空槽位：直接装入 =====
            Debug.Log($"[PlayerWeaponManager] 装入 {targetType}: {targetGun.gameObject.name}");
        }

        // 4. 用 Pickup 的数据恢复弹药状态（核心！）
        targetGun.RestoreAmmoState(pickup.savedCurrentAmmo, pickup.savedTotalAmmo);

        // 5. 装入槽位
        EquipToSlot(targetType, targetGun);

        // 6. 激活显示（带切枪动画）
        StartCoroutine(SwitchWeaponRoutine(targetType));

        // 7. 销毁地面的拾取物
        if (pickup.gameObject != null)
        {
            Destroy(pickup.gameObject);
        }
    }

    #endregion

    #region 调试 / 辅助

    public WeaponType GetCurrentSlotType() => currentSlotType;

    /// <summary>打印当前所有槽位状态</summary>
    public void LogSlotStatus()
    {
        Debug.Log($"=== 武器槽位 ===" +
            $"\n  Primary:   {(primarySlot   != null ? primarySlot.gameObject.name   : "空")}" +
            $"\n  Secondary: {(secondarySlot != null ? secondarySlot.gameObject.name : "空")}" +
            $"\n  Special:   {(specialSlot   != null ? specialSlot.gameObject.name   : "空")}" +
            $"\n  当前激活: {currentSlotType}" +
            $"\n  库存总数: {libraryLookup.Count}");
    }

    /// <summary>打印武器库中所有武器</summary>
    public void LogLibraryContents()
    {
        Debug.Log("=== 武器库内容 ===");
        foreach (var kvp in libraryLookup)
        {
            string status = IsGunEquipped(kvp.Value) ? "[已装备]" : "[空闲]";
            Debug.Log($"  {kvp.Key} → {kvp.Value.gameObject.name} ({kvp.Value.GetWeaponType()}) {status}");
        }
    }

    #endregion
}
