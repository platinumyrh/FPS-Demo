using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrosshairView : MonoBehaviour
{
    [Header("准星四方向图片组件")]
    [SerializeField] private RectTransform partsTop;
    [SerializeField] private RectTransform partsBottom;
    [SerializeField] private RectTransform partsLeft;
    [SerializeField] private RectTransform partsRight;

    [Header("散布与尺寸设置")]
    [Tooltip("角色完全静止时的基础间距")]
    [SerializeField] private float baseSpread = 20f;
    [Tooltip("奔跑/移动时准星额外放大的最大间距")]
    [SerializeField] private float maxMovementSpread = 60f;
    [Tooltip("每开一枪准星瞬间增加的扩张距离")]
    [SerializeField] private float fireExpandForce = 15f;
    [Tooltip("开火扩张后的最大上限，防止准星飞出屏幕")]
    [SerializeField] private float maxFireSpread = 120f;

    [Header("平滑过渡速度")]
    [Tooltip("放大时的插值速度（数值越大越跟手）")]
    [SerializeField] private float expandSpeed = 15f;
    [Tooltip("回缩时的回复速度（数值越大缩回越快）")]
    [SerializeField] private float shrinkSpeed = 8f;


    // 内部状态跟踪变量
    private float currentSpread = 0f;
    private float targetSpread = 0f;
    private float shootingSpreadOffset = 0f; // 开火带来的额外累积扩张量

    private Vector2 currentMoveInput = Vector2.zero;
    private bool isRunning = false;

    private void Start()
    {
        currentSpread = baseSpread;
    }

    private void OnEnable()
    {
        // 1. 订阅移动输入事件（用来感知玩家是否在走动）
        GameEventBus.GetInstance().Subscribe<InputEventData>(GameEventType.OnMoveInput, OnMoveChanged);
        GameEventBus.GetInstance().Subscribe<InputEventData>(GameEventType.OnMoveCanceled, OnMoveChanged);

        // 2. 订阅跑动事件
        GameEventBus.GetInstance().Subscribe<InputHoldingData>(GameEventType.OnRunInput, OnRunChanged);

        // 3. 订阅开火事件（当 GunBase 发射成功，或者 PlayerController 执行 HandleContinuousShooting 时广播）
        // 确保你的 GameEventType 里有这个对应的开火广播
        GameEventBus.GetInstance().Subscribe<WeaponUIData>(GameEventType.OnWeaponUIUpdate, OnWeaponFiredNotification);
    }
    private void OnDisable()
    {
        // 安全解绑
        GameEventBus.GetInstance().Unsubscribe<InputEventData>(GameEventType.OnMoveInput, OnMoveChanged);
        GameEventBus.GetInstance().Unsubscribe<InputEventData>(GameEventType.OnMoveCanceled, OnMoveChanged);
        GameEventBus.GetInstance().Unsubscribe<InputHoldingData>(GameEventType.OnRunInput, OnRunChanged);
        GameEventBus.GetInstance().Unsubscribe<WeaponUIData>(GameEventType.OnWeaponUIUpdate, OnWeaponFiredNotification);
    }


    private void OnMoveChanged(InputEventData data)
    {
        currentMoveInput = data.MoveDirection;
    }

    private void OnRunChanged(InputHoldingData data)
    {
        isRunning = data.IsHolding;
    }
    // 核心技巧：我们可以通过监听武器UI更新包，如果子弹变少了，说明开火了！
    // 或者你也可以让 GunBase 在开火时单独发一个纯粹的 GameEventType.OnPlayerFire 事件
    private int lastAmmo = 0;
    private void OnWeaponFiredNotification(WeaponUIData data)
    {
        // 如果收到的子弹数变少了，判定为“开了一枪”
        if (data.CurrentAmmo < lastAmmo)
        {
            // 瞬间给准星施加一个外扩的冲力
            shootingSpreadOffset = Mathf.Clamp(shootingSpreadOffset + fireExpandForce, 0f, maxFireSpread);
        }
        lastAmmo = data.CurrentAmmo;
    }

    private void Update()
    {
        // ===== 1. 计算移动带来的目标扩张量 =====
        float moveMagnitude = currentMoveInput.magnitude;
        float moveTarget = 0f;

        if (moveMagnitude > 0.1f)
        {
            // 如果在跑，撑到最大移动散布；如果在走，撑到一半
            moveTarget = isRunning ? maxMovementSpread : maxMovementSpread * 0.5f;
        }

        // ===== 2. 计算最终的总目标散布值 =====
        // 基准大小 + 移动加成 + 开火冲力加成
        targetSpread = baseSpread + moveTarget + shootingSpreadOffset;

        // ===== 3. 让当前数值向目标数值进行平滑插值 =====
        // 如果是在放大，用 expandSpeed，如果是回缩，用 shrinkSpeed
        float activeLerpSpeed = (targetSpread > currentSpread) ? expandSpeed : shrinkSpeed;
        currentSpread = Mathf.Lerp(currentSpread, targetSpread, Time.deltaTime * activeLerpSpeed);

        // ===== 4. 开火冲力每帧自动衰减恢复 =====
        if (shootingSpreadOffset > 0f)
        {
            // 让开火扩大的数值以收缩速度自动回弹归零
            shootingSpreadOffset = Mathf.MoveTowards(shootingSpreadOffset, 0f, Time.deltaTime * shrinkSpeed * 10f);
        }

        // ===== 5. 将计算出的当前间距应用到 4 个 UI 的坐标上 =====
        ApplySpreadToUI(currentSpread);
    }

    private void ApplySpreadToUI(float spread)
    {
        if (partsTop == null || partsBottom == null || partsLeft == null || partsRight == null) return;

        // 改变局部坐标（本地轴心偏离）
        partsTop.anchoredPosition = new Vector2(0f, spread);
        partsBottom.anchoredPosition = new Vector2(0f, -spread);
        partsLeft.anchoredPosition = new Vector2(-spread, 0f);
        partsRight.anchoredPosition = new Vector2(spread, 0f);
    }

}
