using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 移动输入数据
/// </summary>
public class InputEventData : GameEventData
{
    public Vector2 MoveDirection;

    public InputEventData(Vector2 dirction)
    {
        MoveDirection = dirction;
    }
}
/// <summary>
/// 视角/瞄准输入数据
/// </summary>
public class InputLookData : GameEventData
{
    public Vector2 LookDelta;       // 视角增量

    public InputLookData(Vector2 delta)
    {
        LookDelta = delta;
    }
}
/// <summary>
/// 简单动作输入数据（Jump/Attack/Interact 等）
/// </summary>
public class InputActionData : GameEventData
{
    public string ActionName;       // 动作名称

    public InputActionData(string actionName)
    {
        ActionName = actionName;
    }
}
/// <summary>
/// 长按输入数据
/// </summary>
public class InputHoldingData : GameEventData
{
    public bool IsHolding;

    public InputHoldingData(bool isHolding)
    {
        IsHolding = isHolding;
    }
}


public class BulletHitEventData : GameEventData
{
    public RaycastHit HitInfo; // 射线检测结果
    public float Damage;         // 造成的伤害值
    public GunBase sourceWeapon;     // 造成伤害的枪械

    public BulletHitEventData(RaycastHit hitInfo, float damage, GunBase source)
    {
        HitInfo = hitInfo;
        Damage = damage;
        sourceWeapon = source;
    }
}