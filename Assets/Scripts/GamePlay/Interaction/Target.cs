using System.Collections;
using UnityEngine;

/// <summary>
/// 简单枪靶：被打中时倒下，延迟几秒后自动立起。
/// </summary>
[RequireComponent(typeof(Animation))] // 确保物体上有 Animation 组件
public class Target : MonoBehaviour, IDamageable
{
    [Header("动画配置")]
    [SerializeField] private Animation targetAnimation;
    [SerializeField] private string downAnimName = "A_Target_Down";
    [SerializeField] private string upAnimName = "A_Target_Up";

    [Header("时间配置")]
    [SerializeField] private float resetDelay = 3f; // 倒下后多少秒立起来

    private bool isDown = false; // 标记当前是否已经是倒下状态

    private void Awake()
    {
        if (targetAnimation == null)
            targetAnimation = GetComponent<Animation>();
    }

    public void OnHit(BulletHitEventData data)
    {
        // 如果已经倒下了，就不重复触发
        if (isDown) return;

        // 开启协程处理倒下和复位的逻辑
        StartCoroutine(HitSequence());
    }

    private IEnumerator HitSequence()
    {
        isDown = true;

        // 1. 播放倒下动画
        if (targetAnimation != null && targetAnimation.GetClip(downAnimName) != null)
        {
            targetAnimation.Play(downAnimName);
        }

        // 2. 等待指定的秒数
        yield return new WaitForSeconds(resetDelay);

        // 3. 播放立起动画
        if (targetAnimation != null && targetAnimation.GetClip(upAnimName) != null)
        {
            targetAnimation.Play(upAnimName);

            // 等起立动画播完，或者直接根据动画长度等待，确保状态安全
            // 如果起立动画是 0.5 秒，可以加一句：yield return new WaitForSeconds(0.5f);
        }
        SoundManager.GetInstance().PlaySFX("SFX/Damageabel/Target/S_TargetGoUp");

        // 4. 重置状态，允许下一次击中
        isDown = false;
    }
}