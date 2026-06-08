public enum GameEventType
{
    //测试事件
    TestEvent,          // 测试事件

  
    ///移动输入
    OnMoveInput,        
    OnMoveCanceled,

    //动作输入
    OnJumpInput,
    OnAttackInput,
    OnInteractInput,
    OnRunInput,

    //瞄准/视角
    OnLookInput,

    //菜单/系统
    OnPauseInput,

    //武器切换
    OnNextWeapon,     // 切换下一把武器
    OnPrevWeapon      // 切换上一把武器

}