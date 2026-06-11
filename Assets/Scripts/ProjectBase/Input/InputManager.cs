using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.iOS;


/// <summary>
/// 输入管理器
/// 职责：
/// 1. 初始化 Unity Input System 的 InputAction
/// 2. 在 Update 中轮询/监听输入变化
/// 3. 将原始输入封装为 GameEventData，通过 GameEventBus 发布
/// 
/// 使用方式：InputManager.GetInstance().Initialize();
/// </summary>
public class InputManager :BaseManager<InputManager>
{
    private PlayerInputAction inputActions;
    

    private Vector2 currentMoveDirection;
    private bool isInitialized = false;


    private bool isRunningPressed = false; // 记录当前是否按下了奔跑键

    ///<summary>
    ///初始化输入系统 在游戏启动时调用一次
    ///<summary>
    public void Initialize()
    {
        if (isInitialized) return;

        //1. 初始化 InputAction
        inputActions = new PlayerInputAction();

        //2.启用输入
        inputActions.Enable();

        // 3. 注册回调（推荐方式：事件驱动）
        RegisterInputCallbacks();

        // 4. 通过 MonoManager 注册 Update，用于需要逐帧轮询的场景
        MonoManager.GetInstance().AddUpdateListener(OnUpdate);

        isInitialized = true;
        Debug.Log("[InputManager] 输入系统初始化完成");
    }
    /// <summary>
    /// 销毁输入系统
    /// </summary>
    public void Dispose()
    {
        if (!isInitialized) return;

        UnregisterInputCallbacks();

        if (inputActions != null)
        {
            inputActions.Disable();
            inputActions.Dispose();
            inputActions = null;
        }

        MonoManager.GetInstance().RemoveUpdateListener(OnUpdate);
        isInitialized = false;
    }

    #region 回调注册

    private void RegisterInputCallbacks()
    {
        var player = inputActions.Player;

        // 移动 - 使用回调方式
        player.Move.performed += OnMovePerformed;
        player.Move.canceled += OnMoveCanceled;

        player.Running.performed += OnRunningPerformed;
        player.Running.canceled += OnRunningCanceled;


        // 跳跃 - 使用回调方式
        player.Jump.performed += OnJumpPerformed;

        // 攻击
       // player.Attack.performed += OnAttackPerformed;
      

        // 视角
        player.Look.performed += OnLookPerformed;

        //下蹲
        player.Crouch.performed += OnCrouchPerformed;

        // 交互
        player.Interact.performed += OnInteractPerformed;

        // 暂停
        player.Pause.performed += OnPausePerformed;

        //鼠标滚轮
        player.Scroll.performed += OnScrollPerformed;

        ///<summary>
        ///枪械相关输入
        ///</summary>
        player.Shoot.performed += OnShootPerformed;
        player.Shoot.canceled += OnShootCanceled;
        player.Reload.performed += OnReloadPerformed;
        player.Aim.performed += OnAimPerformed;
        player.Aim.canceled += OnAimCanceled;
        player.Inspect.performed += OnInspectPerformed;
    }

    private void UnregisterInputCallbacks()
    {
        if (inputActions == null) return;

        var player = inputActions.Player;

        player.Move.performed -= OnMovePerformed;
        player.Move.canceled -= OnMoveCanceled;

        player.Running.performed -= OnRunningPerformed;
        player.Running.canceled -= OnRunningCanceled;

        player.Jump.performed -= OnJumpPerformed;
      //  player.Attack.performed -= OnAttackPerformed;
        player.Shoot.performed -= OnShootPerformed;

        player.Look.performed -= OnLookPerformed;
        player.Interact.performed -= OnInteractPerformed;
        player.Pause.performed -= OnPausePerformed;

        ///<summary>
        ///枪械相关输入
        ///</summary>
        player.Shoot.performed -= OnShootPerformed;
        player.Shoot.canceled -= OnShootCanceled;
        player.Aim.performed -= OnAimPerformed;
        player.Aim.canceled -= OnAimCanceled;


    }

    #endregion

    #region 输入事件处理 → 发布到 GameEventBus

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        currentMoveDirection = context.ReadValue<Vector2>();
        // 发布移动事件
        GameEventBus.GetInstance().Publish(GameEventType.OnMoveInput,
            new InputEventData(currentMoveDirection));
       
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        currentMoveDirection = Vector2.zero;
        GameEventBus.GetInstance().Publish(GameEventType.OnMoveCanceled,
            new InputEventData(Vector2.zero));
    }

    private void OnRunningPerformed(InputAction.CallbackContext context)
    {
        isRunningPressed = true;
        // 发布开始奔跑事件（假设你在 GameEventType 里加了 OnRunInput）
        GameEventBus.GetInstance().Publish(GameEventType.OnRunInput, new InputHoldingData(true));
    }

    private void OnRunningCanceled(InputAction.CallbackContext context)
    {
        isRunningPressed = false;
        // 发布停止奔跑事件
        GameEventBus.GetInstance().Publish(GameEventType.OnRunInput, new InputHoldingData(false));
    }

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        GameEventBus.GetInstance().Publish(GameEventType.OnJumpInput,
            new InputActionData("Jump"));
    }


    private void OnLookPerformed(InputAction.CallbackContext context)
    {
        Vector2 lookDelta = context.ReadValue<Vector2>();
        GameEventBus.GetInstance().Publish(GameEventType.OnLookInput,
            new InputLookData(lookDelta));
      //  Debug.Log($"Look Input: {lookDelta}");
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        GameEventBus.GetInstance().Publish(GameEventType.OnInteractInput,
            new InputActionData("Interact"));
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        GameEventBus.GetInstance().Publish(GameEventType.OnPauseInput,
            new InputActionData("Pause"));
    }

    private void OnScrollPerformed(InputAction.CallbackContext context)
    {
        Vector2 scrollValue = context.ReadValue<Vector2>();

        // 我们主要关心 y 轴（上下滚动）
        if (scrollValue.y > 0)
        {
            //Debug.Log("滚轮向上：切下一把武器");
            // 可以通过 GameEventBus 发布一个切换下一把枪的事件
            GameEventBus.GetInstance().Publish(GameEventType.OnNextWeapon,new InputActionData("NextWeapon"));
        }
        else if (scrollValue.y < 0)
        {
           // Debug.Log("滚轮向下：切上一把武器");
            // 可以通过 GameEventBus 发布一个切换上一把枪的事件
            GameEventBus.GetInstance().Publish(GameEventType.OnPrevWeapon, new InputActionData("PrevWeapon"));

        }
    }

    private void OnShootPerformed(InputAction.CallbackContext context)
    {
        GameEventBus.GetInstance().Publish(GameEventType.OnShoot,
            new InputHoldingData(true));
        
    }
    private void OnShootCanceled(InputAction.CallbackContext context)
    {
        GameEventBus.GetInstance().Publish(GameEventType.OnShoot,
            new InputHoldingData(false));
    }
    private void OnReloadPerformed(InputAction.CallbackContext context)
    {
        GameEventBus.GetInstance().Publish(GameEventType.OnReload,
            new InputActionData("Reload"));
    }
    private void OnAimPerformed(InputAction.CallbackContext context)
    {
        GameEventBus.GetInstance().Publish(GameEventType.OnAim,
            new InputHoldingData(true));
    }
    private void OnAimCanceled(InputAction.CallbackContext context)
    {
        GameEventBus.GetInstance().Publish(GameEventType.OnAim,
            new InputHoldingData(false));
    }
    private void OnInspectPerformed(InputAction.CallbackContext context)
    {
        GameEventBus.GetInstance().Publish(GameEventType.OnInspect,
            new InputActionData("Inspect"));
    }

    private void OnCrouchPerformed(InputAction.CallbackContext context)
    {
        GameEventBus.GetInstance().Publish(GameEventType.OnCrouchInput,
            new InputActionData("Crouch"));
    }

    #endregion


    /// <summary>
    /// 逐帧更新（如果有些输入需要逐帧读取值，可以放这里）
    /// 比如：持续按住的输入、需要平滑处理的输入等
    /// </summary>
    private void OnUpdate()
    {
        // 示例：如果需要每帧获取当前移动方向（用于平滑处理等）
        // 大多数情况下用上面的回调就够了，这里作为补充
        // 每帧读取当前移动输入值
        if (inputActions != null)
        {
            Vector2 moveValue = inputActions.Player.Move.ReadValue<Vector2>();

            // 只有当值不为零时才发布事件（避免每帧都发无用事件）
            // 或者你可以每帧都发，让业务层自己判断
            if (moveValue != Vector2.zero)
            {
                GameEventBus.GetInstance().Publish(GameEventType.OnMoveInput,
                    new InputEventData(moveValue));
            }
        }
    }

    // ===== 对外提供的查询接口（可选）=====

    /// <summary>
    /// 获取当前移动方向（供需要直接查询的地方使用）
    /// </summary>
    public Vector2 GetCurrentMoveDirection() => currentMoveDirection;

}
