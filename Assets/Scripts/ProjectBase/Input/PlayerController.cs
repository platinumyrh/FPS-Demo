using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Camera playerCamera;
    private Animator animator;
    private CharacterController characterController; // 引用根对象上的CC
    private PlayerAnimationController animationController; // 引用子对象上的动画控制器

    [Header("视角设置")]
    [SerializeField] private float mouseSensitivity = 1.0f; // 鼠标灵敏度系数
    [SerializeField] private float minPitch = -85f;         // 抬头最大角度
    [SerializeField] private float maxPitch = 85f;          // 低头最大角度

    [Header("移动设置")]
    [SerializeField] private float moveSpeed = 5f;         // 移动速度
    [SerializeField] private float runMultiplier = 1.5f;    // 奔跑时的速度倍率
    [SerializeField] private bool isRunning;           // 是否正在奔跑
    private Vector2 currentInputMove; // 缓存当前的移动输入，在 Update 中统一处理

    [Header("跳跃相关")]
    [SerializeField] private float jumpHeight = 1.5f;       // 想跳跃的物理高度（米）
    [SerializeField] private float gravity = -9.81f;      // 重力加速度
    [SerializeField] private bool isGrounded;           // 是否在地面上
    private float verticalVelocity;   // 当前垂直方向的速度（Y轴速度）

    [Header("战斗相关")]
    [SerializeField] private bool isAiming = false;
    [SerializeField] private bool isShooting = false;
    [SerializeField] private float fireRate = 0.1f; // 射击间隔（秒），0.1秒一发等于每分钟600发
    private float fireTimer = 0f; // 射击冷却计时器

    
    // 新增：把手里的枪或者枪身上的 GunBase 脚本拖到这个槽位里（或者通过代码换枪时动态赋值）
    
    public GunBase currentWeapon { get; set; }


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
        animationController = GetComponentInChildren<PlayerAnimationController>();
        SetCursorState(true); // 游戏开始时默认锁定鼠标


    }
    private void OnEnable()
    {
        //订阅输入事件
        GameEventBus.GetInstance().Subscribe<InputEventData>(GameEventType.OnMoveInput, OnMove);
        GameEventBus.GetInstance().Subscribe<InputEventData>(GameEventType.OnMoveCanceled, OnMove); // 复用同一个回调，输入取消时传入 Vector2.zero
        GameEventBus.GetInstance().Subscribe<InputActionData>(GameEventType.OnJumpInput, OnJump);
        GameEventBus.GetInstance().Subscribe<InputHoldingData>(GameEventType.OnShoot, OnShootChange);
        GameEventBus.GetInstance().Subscribe<InputLookData>(GameEventType.OnLookInput, OnLook);
        GameEventBus.GetInstance().Subscribe<InputHoldingData>(GameEventType.OnRunInput, OnRun);
        GameEventBus.GetInstance().Subscribe<InputActionData>(GameEventType.OnReload, OnReload);
        GameEventBus.GetInstance().Subscribe<InputHoldingData>(GameEventType.OnAim, OnAimChange);
        GameEventBus.GetInstance().Subscribe<InputActionData>(GameEventType.OnInspect, OnInspect);


    }
    private void OnDisable()
    {
        // 取消订阅，防止内存泄漏
        GameEventBus.GetInstance().Unsubscribe<InputEventData>(GameEventType.OnMoveInput, OnMove);
        GameEventBus.GetInstance().Unsubscribe<InputEventData>(GameEventType.OnMoveCanceled, OnMove);
        GameEventBus.GetInstance().Unsubscribe<InputActionData>(GameEventType.OnJumpInput, OnJump);
        GameEventBus.GetInstance().Unsubscribe<InputHoldingData>(GameEventType.OnShoot, OnShootChange);
        GameEventBus.GetInstance().Unsubscribe<InputLookData>(GameEventType.OnLookInput, OnLook);
        GameEventBus.GetInstance().Unsubscribe<InputHoldingData>(GameEventType.OnRunInput, OnRun);
        GameEventBus.GetInstance().Unsubscribe<InputActionData>(GameEventType.OnReload, OnReload);
        GameEventBus.GetInstance().Unsubscribe<InputHoldingData>(GameEventType.OnAim, OnAimChange);
        GameEventBus.GetInstance().Unsubscribe<InputActionData>(GameEventType.OnInspect, OnInspect);
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

        // 处理全自动连续射击的计时器
        if (fireTimer > 0f)
        {
            fireTimer -= Time.deltaTime;
        }

        // 如果玩家长按着开火键，且冷却好了
        if (isShooting && fireTimer <= 0f)
        {
            HandleContinuousShooting();
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
    private void OnRun(InputHoldingData data)
    {
        isRunning = data.IsHolding;
    }
    /// <summary>
    /// 每帧执行：将水平移动、重力、跳跃速度结合，最终传给 CharacterController
    /// </summary>
    private void ApplyMovementAndGravity()
    {
        if (characterController == null) return;

        // 检测是否在地面上（直接使用 CC 自带的 isGrounded 属性）
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

        //  计算水平移动向量 (X 和 Z 轴)
        Vector3 moveMovement = transform.right * currentInputMove.x + transform.forward * currentInputMove.y;

        bool actualRunning = isRunning && currentInputMove.magnitude > 0.1f;
        float currentSpeed = moveSpeed;
        if (actualRunning && !isAiming)
        {
            currentSpeed = moveSpeed * runMultiplier;
        }
        else if (isAiming)
        {
            currentSpeed = moveSpeed * 0.5f; // 瞄准时走得慢
            actualRunning = false;          // 瞄准时强制不能处于奔跑动画状态
        }

        Vector3 finalVelocity = moveMovement * currentSpeed;
        finalVelocity.y = verticalVelocity;

        characterController.Move(finalVelocity * Time.deltaTime);

        // 驱动移动动画
        animationController.ApplyLocomotion(currentInputMove, actualRunning);
    }

   
    public void OnShoot(InputActionData data)
    {
        Debug.Log($"玩家控制器接收到射击事件，动作名称：{data.ActionName}");
    }

    public void OnShootChange(InputHoldingData data)
    {
        isShooting = data.IsHolding; // 这里的 IsRunning 代表按键是否按住
        if (!isShooting)
        {
            //Debug.Log("停止射击");
            animationController.StopShoot(); // 可选：通知动画停止
        }
    }

    // 执行单发开火逻辑
    private void HandleContinuousShooting()
    {
        fireTimer = fireRate; // 重置冷却
       // Debug.Log("突突突！发射子弹！");

        // 1. 触发玩家角色自身的双臂开火动画表现
        animationController.ApplyShoot();

        // 2. 【核心联动】：通知当前手里的枪执行它自己的射击和枪支动画！
        if (currentWeapon != null)
        {
            currentWeapon.FireWeapon();
        }
    }

    public void OnReload(InputActionData data)
    {
        animationController.ApplyReload(); // 角色播放换弹动画

        if (currentWeapon != null)
        {
            currentWeapon.ReloadWeapon(); // 枪械播放换弹动画
        }
    }

    public void OnAim(InputActionData data)
    {
       // Debug.Log($"玩家控制器接收到瞄准事件，动作名称：{data.ActionName}");
        animationController.ApplyAim(isAiming); // 触发瞄准动画
    }

    public void OnAimChange(InputHoldingData data)
    {
        isAiming = data.IsHolding;
       // Debug.Log(isAiming ? "进入瞄准" : "退出瞄准");

        // 将最新的瞄准状态同步给动画控制器
        animationController.ApplyAim(isAiming);
    }

    public void OnInspect(InputActionData data)
    {
      //  Debug.Log($"玩家控制器接收到检查枪械事件，动作名称：{data.ActionName}");
      animationController.ApplyInspect(); // 触发检查枪械动画
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

