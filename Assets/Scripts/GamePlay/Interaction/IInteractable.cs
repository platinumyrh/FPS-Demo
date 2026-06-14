/// <summary>
/// 所有可交互物体必须实现的接口
/// </summary>
public interface IInteractable
{
    // 交互时显示的提示文本（如 "按 E 拾取 AR-15"、"按 F 开门"）
    string GetInteractionPrompt();

    // 玩家按下 E 时执行的实际交互逻辑
    void OnInteract(PlayerController player);

    // 玩家进入交互范围时调用（高亮、显示提示UI等）
    void OnFocusEnter();

    // 玩家离开交互范围时调用（取消高亮、隐藏提示）
    void OnFocusExit();
}