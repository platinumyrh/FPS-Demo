/// <summary>
/// 所有可被子弹击中的对象实现此接口。
/// DamageManager 在收到 OnBulletHit 事件后，遍历击中物体上的 IDamageable 组件并调用 OnHit。
/// </summary>
public interface IDamageable
{
    void OnHit(BulletHitEventData data);
}
