using UnityEngine;

/// <summary>
/// 伤害分发器。订阅 OnBulletHit，找到击中物体上所有 IDamageable 组件并调用 OnHit。
/// 用法：GameManager 中调用 Initialize()。
/// </summary>
public class DamageManager : BaseManager<DamageManager>
{
    private bool isInitialized;

    public void Initialize()
    {
        if (isInitialized) return;

        GameEventBus.GetInstance().Subscribe<BulletHitEventData>(GameEventType.OnBulletHit, OnBulletHit);

        isInitialized = true;
    }

    private void OnBulletHit(BulletHitEventData data)
    {
        if (data.HitInfo.collider == null) return;

        // 遍历击中物体及其父链上的 IDamageable（父链支持：子弹打中模型子物体，伤害接到根脚本）
        var damageables = data.HitInfo.collider.GetComponentsInParent<IDamageable>();
        foreach (var d in damageables)
        {
            d.OnHit(data);
        }
    }
}
