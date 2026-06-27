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


    private bool isRunningPressed = false;

    // 缓存事件数据对象，避免每帧堆分配
    private InputEventData cachedMoveEvent = new InputEventData(Vector2.zero);
    private InputLookData cachedLookEvent = new InputLookData(Vector2.zero);
    private InputHoldingData cachedHoldTrue = new InputHoldingData(true);
    private InputHoldingData cachedHoldFalse = new InputHoldingData(false);

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

        isInitialized = true;
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

        //丢枪
        player.DropWeapon.performed += OnDropWeaponPerformed;

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
        cachedMoveEvent.MoveDirection = currentMoveDirection;
        GameEventBus.GetInstance().Publish(GameEventType.OnMoveInput, cachedMoveEvent);
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        currentMoveDirection = Vector2.zero;
        cachedMoveEvent.MoveDirection = Vector2.zero;
        GameEventBus.GetInstance().Publish(GameEventType.OnMoveCanceled, cachedMoveEvent);
    }

    private void OnRunningPerformed(InputAction.CallbackContext context)
    {
        isRunningPressed = true;
        GameEventBus.GetInstance().Publish(GameEventType.OnRunInput, cachedHoldTrue);
    }

    private void OnRunningCanceled(InputAction.CallbackContext context)
    {
        isRunningPressed = false;
        GameEventBus.GetInstance().Publish(GameEventType.OnRunInput, cachedHoldFalse);
    }

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        GameEventBus.GetInstance().Publish(GameEventType.OnJumpInput,
            new InputActionData("Jump"));
    }


    private void OnLookPerformed(InputAction.CallbackContext context)
    {
        cachedLookEvent.LookDelta = context.ReadValue<Vector2>();
        GameEventBus.GetInstance().Publish(GameEventType.OnLookInput, cachedLookEvent);
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
        if (scrollValue.y > 0)
            GameEventBus.GetInstance().Publish(GameEventType.OnNextWeapon, new InputActionData("NextWeapon"));
        else if (scrollValue.y < 0)
            GameEventBus.GetInstance().Publish(GameEventType.OnPrevWeapon, new InputActionData("PrevWeapon"));
    }

    private void OnShootPerformed(InputAction.CallbackContext context)
    {
        GameEventBus.GetInstance().Publish(GameEventType.OnShoot, cachedHoldTrue);
    }
    private void OnShootCanceled(InputAction.CallbackContext context)
    {
        GameEventBus.GetInstance().Publish(GameEventType.OnShoot, cachedHoldFalse);
    }
    private void OnReloadPerformed(InputAction.CallbackContext context)
    {
        GameEventBus.GetInstance().Publish(GameEventType.OnReload, new InputActionData("Reload"));
    }
    private void OnAimPerformed(InputAction.CallbackContext context)
    {
        GameEventBus.GetInstance().Publish(GameEventType.OnAim, cachedHoldTrue);
    }
    private void OnAimCanceled(InputAction.CallbackContext context)
    {
        GameEventBus.GetInstance().Publish(GameEventType.OnAim, cachedHoldFalse);
    }
    private void OnInspectPerformed(InputAction.CallbackContext context)
    {
        GameEventBus.GetInstance().Publish(GameEventType.OnInspect, new InputActionData("Inspect"));
    }

    private void OnDropWeaponPerformed(InputAction.CallbackContext conext)
    {
        GameEventBus.GetInstance().Publish(GameEventType.OnDropInput, new InputActionData("DropWeapon"));
    }

    private void OnCrouchPerformed(InputAction.CallbackContext context)
    {
        GameEventBus.GetInstance().Publish(GameEventType.OnCrouchInput, new InputActionData("Crouch"));
    }

    #endregion


    // ===== 对外提供的查询接口（可选）=====

    /// <summary>
    /// 获取当前移动方向（供需要直接查询的地方使用）
    /// </summary>
    public Vector2 GetCurrentMoveDirection() => currentMoveDirection;

}
