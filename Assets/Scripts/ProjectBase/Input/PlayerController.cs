using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Camera playerCamera;
    private Animator animator;
    private CharacterController characterController; // 引用根对象上的CC

    [Header("视角设置")]
    [SerializeField] private float mouseSensitivity = 1.0f; // 鼠标灵敏度系数
    [SerializeField] private float minPitch = -85f;         // 抬头最大角度
    [SerializeField] private float maxPitch = 85f;          // 低头最大角度

    [Header("移动设置")]
    [SerializeField] private float moveSpeed = 5f;         // 移动速度
    private Vector2 currentInputMove; // 缓存当前的移动输入，在 Update 中统一处理

    [Header("跳跃相关")]
    [SerializeField] private float jumpHeight = 1.5f;       // 想跳跃的物理高度（米）
    [SerializeField] private float gravity = -9.81f;      // 重力加速度
    [SerializeField] private bool isGrounded;           // 是否在地面上
    private float verticalVelocity;   // 当前垂直方向的速度（Y轴速度）


    private float cameraPitch = 0f;       // 累积相机的上下旋转量量值
    private bool isCursorLocked = true;   // 鼠标锁定状态变量





    private void Awake()
    {
        // 初始化输入系统
        InputManager.GetInstance().Initialize();
    }
    private void Start()
    {
        playerCamera = GetComponentInChildren<Camera>();
        characterController = GetComponentInParent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        SetCursorState(true); // 游戏开始时默认锁定鼠标


    }
    private void OnEnable()
    {
        //订阅输入事件
        GameEventBus.GetInstance().Subscribe<InputEventData>(GameEventType.OnMoveInput, OnMove);
        GameEventBus.GetInstance().Subscribe<InputEventData>(GameEventType.OnMoveCanceled, OnMove); // 复用同一个回调，输入取消时传入 Vector2.zero
        GameEventBus.GetInstance().Subscribe<InputActionData>(GameEventType.OnJumpInput, OnJump);
        GameEventBus.GetInstance().Subscribe<InputActionData>(GameEventType.OnAttackInput, OnAttack);
        GameEventBus.GetInstance().Subscribe<InputLookData>(GameEventType.OnLookInput, OnLook);

    }
    private void OnDisable()
    {
        // 取消订阅，防止内存泄漏
        GameEventBus.GetInstance().Unsubscribe<InputEventData>(GameEventType.OnMoveInput, OnMove);
        GameEventBus.GetInstance().Unsubscribe<InputEventData>(GameEventType.OnMoveCanceled, OnMove);
        GameEventBus.GetInstance().Unsubscribe<InputActionData>(GameEventType.OnJumpInput, OnJump);
        GameEventBus.GetInstance().Unsubscribe<InputActionData>(GameEventType.OnAttackInput, OnAttack);
        GameEventBus.GetInstance().Unsubscribe<InputLookData>(GameEventType.OnLookInput, OnLook);
    }

    private void Update()
    {
        // 监听 Esc 键切换鼠标锁定状态
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetCursorState(!isCursorLocked);
        }

        // 测试卸载输入系统
        if (Input.GetKeyDown(KeyCode.U))
        {
            InputManager.GetInstance().Dispose();
            Debug.Log("输入系统已卸载");
        }
        ApplyMovementAndGravity(); // 每帧调用一次，处理移动和重力逻辑
    }

    private void OnMove(InputEventData data)
    {
        //  Debug.Log($"移动方向: {data.MoveDirection}");
        // 处理移动逻辑...
        //后面要改成角色控制器的移动，这里先简单实现
        // transform.Translate(new Vector3(data.MoveDirection.x, 0, data.MoveDirection.y) * Time.deltaTime * 5f);
        currentInputMove = data.MoveDirection;
    }

    private void OnJump(InputActionData data)
    {
        Debug.Log("跳跃！");
        // 处理跳跃逻辑...
        // 只有当在地面上时，才允许起跳
        if (isGrounded)
        {
            // 根据公式：v = sqrt(h * -2 * g) 计算出达到特定高度所需的起跳初速度
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            Debug.Log("跳跃成功！起跳速度：" + verticalVelocity);
        }
    }
    /// <summary>
    /// 每帧执行：将水平移动、重力、跳跃速度结合，最终传给 CharacterController
    /// </summary>
    private void ApplyMovementAndGravity()
    {
        if (characterController == null) return;

        // 1. 检测是否在地面上（直接使用 CC 自带的 isGrounded 属性）
        isGrounded = characterController.isGrounded;

        if (isGrounded && verticalVelocity < 0)
        {
            // 当人在地面上时，给一个微小的向下压的速度（比如 -2f），防止人在走斜坡时“悬空”导致 isGrounded 忽真忽假
            verticalVelocity = -2f;
        }
        else
        {
            // 如果在空中，持续每帧累加重力加速度（注意：物理公式需要乘以 Time.deltaTime）
            verticalVelocity += gravity * Time.deltaTime;
        }

        // 2. 计算水平移动向量 (X 和 Z 轴)
        Vector3 moveMovement = transform.right * currentInputMove.x + transform.forward * currentInputMove.y;
        Vector3 finalVelocity = moveMovement * moveSpeed;
       
        // 3. 将计算好的垂直速度 (Y 轴) 融合进最终速度向量中
        finalVelocity.y = verticalVelocity;

        // 4. 最终调用一次 Move 方法（统一乘以 Time.deltaTime）
        characterController.Move(finalVelocity * Time.deltaTime);
    }

    private void OnAttack(InputActionData data)
    {
        Debug.Log("攻击！");
        // 处理攻击逻辑...
    }

    /// <summary>
    /// 处理视角调整逻辑
    /// </summary>
    private void OnLook(InputLookData data)
    {
        // 如果鼠标未锁定（比如按了ESC弹出了菜单），则不响应视角转动
        if (!isCursorLocked) return;
        if (animator == null) return;

        // 1. 获取输入增量，并乘以灵敏度
        // 新版输入系统的 Mouse Delta 内部已经处理过像素缩放，这里可以乘以一个基础调节系数
        float mouseX = data.LookDelta.x * mouseSensitivity * 0.1f;
        float mouseY = data.LookDelta.y * mouseSensitivity * 0.1f;

        // 2. 左右看：直接旋转【根对象】（也就是挂有 CC 的当前物体）的 Y 轴
        transform.Rotate(Vector3.up * mouseX);

        // 3. 上下看：通过累积变量来控制【子对象摄像机】的局部 X 轴旋转
        // 鼠标向上推(mouseY为正)时，镜头应该往上看(Pitch减小)，所以这里用减法
        cameraPitch -= mouseY;
        // 限制抬头低头的角度，防止翻转过头
        cameraPitch = Mathf.Clamp(cameraPitch, minPitch, maxPitch);

        // 4. 应用相机的局部旋转。强制 Y 和 Z 轴为 0，彻底根治“镜头变歪”和“万向节死锁”
        animator.transform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    /// <summary>
    /// 统一控制鼠标状态的方法
    /// </summary>
    private void SetCursorState(bool locked)
    {
        isCursorLocked = locked;

        if (locked)
        {
            Cursor.lockState = CursorLockMode.Locked; // 锁定鼠标到屏幕中心并自动隐藏
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;   // 解除锁定
            Cursor.visible = true;                    // 显示鼠标
        }
    }
}
