using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 交互检测器 — 挂在玩家身上
/// 
/// 职责：
///   1. 每帧从相机位置发射射线，检测前方是否有 IInteractable 物体
///   2. 管理当前聚焦目标（进入/离开的 Focus 事件分发）
///   3. 当玩家按下交互键时，触发当前目标的 OnInteract()
/// 
/// 使用方式：
///   挂到 Player GameObject 上，Inspector 中配置检测距离和层级
/// </summary>
public class InteractionDetector : MonoBehaviour
{
    [Header("检测设置")]
    [Tooltip("交互检测的最大距离（米）")]
    [SerializeField] private float detectDistance = 3f;

    [Tooltip("射线检测的层级")]
    [SerializeField] private LayerMask interactableLayer;

    [Header("引用（自动获取，也可手动拖入）")]
    [Tooltip("玩家的相机（射线从这发出）")]
    private Camera playerCamera;

    /// <summary>当前正在聚焦的可交互物体</summary>
    public IInteractable CurrentTarget { get; private set; }

    /// <summary>当前聚焦目标的提示文本（供 UI 显示用）</summary>
    public string CurrentPrompt { get; private set; }

    void Start()
    {
        // 自动获取相机
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }
    }

    void Update()
    {
        DetectInteractable();
    }



    #region 射线检测核心
    /// <summary>每帧执行：检测前方是否有可交互物体</summary>
    private void DetectInteractable()
    {
        if (playerCamera == null) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, detectDistance, interactableLayer))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            // 命中了可交互物体
            if (interactable != null)
            {
                if (CurrentTarget != interactable)
                { // 目标切换：旧目标失去焦点 → 新目标获得焦点
                    SwitchFocus(interactable);
                }
            }
            else
            {
                // 命中了不可交互的物体（比如普通墙壁）
                ClearFocus();
            }
        }
        else
        {
            // 什么都没命中
            ClearFocus();
        }
    }
    #endregion

    #region 聚焦管理
    /// <summary>切换焦点到新的可交互物体</summary>
    private void SwitchFocus(IInteractable newTarget)
    {
        // 先让旧目标退出焦点
        if (CurrentTarget != null)
        {
            CurrentTarget.OnFocusExit();
        }

        // 切换到新目标
        CurrentTarget = newTarget;
        CurrentPrompt = newTarget.GetInteractionPrompt();

        // 让新目标进入焦点
        CurrentTarget.OnFocusEnter();

        //Debug.Log($"[InteractionDetector] 聚焦: {CurrentPrompt}");
    }

    /// <summary>清除当前焦点</summary>
    private void ClearFocus()
    {
        if (CurrentTarget != null)
        {
            CurrentTarget.OnFocusExit();
           // Debug.Log($"[InteractionDetector] 取消聚焦: {CurrentPrompt}");

            CurrentTarget = null;
            CurrentPrompt = null;
        }
    }

    #endregion
    #region 交互触发（由 PlayerController 在按 E 时调用）

    /// <summary>
    /// 尝试与当前聚焦的物体进行交互
    /// 由 PlayerController.OnInteract() 调用
    /// </summary>
    /// <param name="player">玩家控制器引用（传给 OnInteract）</param>
    public void TryInteract(PlayerController player)
    {
        if (CurrentTarget == null)
        {
            Debug.Log("[InteractionDetector] 当前没有可交互的目标");
            return;
        }

        Debug.Log($"[InteractionDetector] 触发交互: {CurrentPrompt}");

        // 调用目标物体的交互方法
        CurrentTarget.OnInteract(player);

        // 交互后清除焦点（防止重复触发）
        // 注意：某些场景下可能不需要清除（比如连续对话），这里先清除
        ClearFocus();
    }

    #endregion


    #region 调试可视化

    void OnDrawGizmos()
    {
        if (playerCamera == null) return;

        // 在 Scene 视图中绘制检测射线
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(
            playerCamera.transform.position,
            playerCamera.transform.position + playerCamera.transform.forward * detectDistance
        );

        // 绘制终点球
        Gizmos.color = new Color(1, 1, 0, 0.3f);
        Gizmos.DrawWireSphere(
            playerCamera.transform.position + playerCamera.transform.forward * detectDistance,
            0.1f
        );
    }

    #endregion


}
