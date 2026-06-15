using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 地面武器拾取物组件
/// 
/// 挂载在场景中可见的武器模型上，玩家靠近并交互时可以捡起。
/// 它保存了被丢弃武器的完整状态数据（ID、类型、弹药），捡起时由 PlayerWeaponManager 读取。
/// </summary>
public class GroundWeaponPickup : MonoBehaviour,IInteractable
{
    [Header("武器信息")]
    [Tooltip("武器唯一标识，需与玩家 INVENTORY 下对应枪的 GunBase.GetWeaponId() 一致")]
    public string weaponId;
    public WeaponType weaponType;
    public int savedCurrentAmmo = 30;
    public int savedTotalAmmo = 90;

    [Header("手动放置配置")]
    [Tooltip("Resources 下模型预制体路径，如 Model/Guns/P_LPSP_WEP_AR_01")]
    [SerializeField] private string modelPath = "";

    private bool didSetup;

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

    void Start()
    {
        // 如果 Setup() 已经被调用过（丢枪生成），跳过自初始化
        if (didSetup) return;

        // 如果 weaponId 为空，说明没配置，跳过
        if (string.IsNullOrEmpty(weaponId))
        {
          //  Debug.LogWarning("[GroundWeaponPickup] weaponId 为空，跳过自初始化。请检查 Inspector 配置。");
            return;
        }

        //Debug.Log($"[GroundWeaponPickup] Start自初始化 — weaponId={weaponId}, type={weaponType}, 弹夹={savedCurrentAmmo}, 备弹={savedTotalAmmo}, modelPath={modelPath}");

        startPosition = transform.position;
        spawnTime = Time.time;

        Setup(weaponId, weaponType, savedCurrentAmmo, savedTotalAmmo, modelPath);
    }

    public void Setup(string id, WeaponType type, int currentAmmo, int totalAmmo, string modelPath)
    {
        this.weaponId = id;
        this.weaponType = type;
        this.savedCurrentAmmo = currentAmmo;
        this.savedTotalAmmo = totalAmmo;
        this.modelPath = modelPath;
        didSetup = true;

        startPosition = transform.position;
        spawnTime = Time.time;

        LoadVisualModel(modelPath);
    }

    #region 模型加载

    /// <summary>
    /// 根据 Resources 路径加载武器模型，并实例化为当前 pickup 的子物体
    /// </summary>
    /// <param name="resourcePath">Resources 目录下的相对路径</param>
    private void LoadVisualModel(string resourcePath)
    {
        if (string.IsNullOrEmpty(resourcePath))
        {
            Debug.LogWarning($"[GroundWeaponPickup] modelPath 为空，跳过模型加载 ({weaponId})");
            return;
        }

        // 从 Resources 加载预制体
        GameObject modelPrefab = ResManager.GetInstance().Load<GameObject>(resourcePath);
        if (modelPrefab == null)
        {
            Debug.LogError($"[GroundWeaponPickup] 无法从 Resources 加载模型: '{resourcePath}' ({weaponId})\n" +
                           $"请确认路径正确且文件存在于 Assets/Resources/ 目录下！");
            return;
        }

        // 实例化为子物体
        GameObject modelInstance = Instantiate(modelPrefab, transform);
        modelInstance.name = "VisualModel";

        // 重置变换，让模型居中显示在 pickup 位置
        modelInstance.transform.localPosition = Vector3.zero;
        modelInstance.transform.localRotation = Quaternion.identity;

        // 可选：自动调整缩放（如果模型太大或太小可以在这里处理）
         modelInstance.transform.localScale = Vector3.one * 2f;

        // 可选：禁用模型上可能存在的脚本组件（地面上的枪不需要射击等逻辑）
        var gunBase = modelInstance.GetComponent<GunBase>();
        if (gunBase != null)
        {
            Destroy(gunBase);  // 销毁 GunBase 脚本，避免地面上的模型响应输入
        }

        // 禁用 Collider 避免干扰拾取物的交互检测（拾取物自身有 Collider）
        var colliders = modelInstance.GetComponentsInChildren<Collider>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }

       // Debug.Log($"[GroundWeaponPickup] 模型已加载: {resourcePath} → {weaponId}");
    }

    #endregion

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

        //Debug.Log($"[GroundWeaponPickup] 被捡起: {weaponId}");

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

    //接口实现
    public string GetInteractionPrompt()
    {
        return $"E to Pick Up {weaponId}";
    }

    public void OnInteract(PlayerController player)
    {
        // 找到玩家的 WeaponManager，执行捡枪逻辑
        var weaponManager = player.GetComponent<PlayerWeaponManager>();
        weaponManager?.PickupWeapon(this);
        
    }

    public void OnFocusEnter()
    {
        // 高亮效果：材质变亮 / 边框发光 / 浮动加速等
      //  Debug.Log($"[交互] 瞄准: {weaponId}");
        UIManager.GetInstance().ShowSimplePanel("Interaction", UI_Layer.System);
    }

    public void OnFocusExit()
    {
        // 取消高亮
        UIManager.GetInstance().HideSimplePanel("Interaction");
    }
}
