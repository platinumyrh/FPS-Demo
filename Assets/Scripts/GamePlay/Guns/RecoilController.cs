using UnityEngine;

/// <summary>
/// 后坐力控制器。挂在和 PlayerController 同一 GameObject 上。
///
/// 工作方式（delta 模式）：
///   开枪 → ApplyRecoil() 累加到 recoilPosition
///   每帧 → ConsumeDelta() 返回 (当前值 - 上一帧值)，同时内部衰减回零
///   PlayerController 把 delta 应用到 cameraPitch 和 yaw 旋转
/// </summary>
public class RecoilController : MonoBehaviour
{
    /// <summary>当前累积的后坐力偏移（度）</summary>
    public Vector2 recoilPosition;

    private Vector2 prevRecoil;
    private float recoverySpeed = 8f;

    /// <summary>
    /// 由 GunBase.FireWeapon() 每发调用，累加后坐力。
    /// </summary>
    public void ApplyRecoil(float vertical, float horizontalMin, float horizontalMax,
        float speed, float maxVert)
    {
        float horizontal = Random.Range(horizontalMin, horizontalMax);

        recoilPosition.x += vertical;
        recoilPosition.y += horizontal;

        // 防止无限累积
        recoilPosition.x = Mathf.Min(recoilPosition.x, maxVert);

        recoverySpeed = speed;
    }

    /// <summary>
    /// 由 PlayerController.Update() 调用。
    /// 衰减 recoilPosition 并返回本帧的变化量（delta）。
    /// </summary>
    public Vector2 ConsumeDelta()
    {
        // 衰减回零
        if (recoilPosition != Vector2.zero)
        {
            float decay = recoverySpeed * Time.deltaTime;
            recoilPosition.x = Mathf.MoveTowards(recoilPosition.x, 0f, decay);
            recoilPosition.y = Mathf.MoveTowards(recoilPosition.y, 0f, decay * 2f);
        }

        Vector2 delta = recoilPosition - prevRecoil;
        prevRecoil = recoilPosition;
        return delta;
    }
}
