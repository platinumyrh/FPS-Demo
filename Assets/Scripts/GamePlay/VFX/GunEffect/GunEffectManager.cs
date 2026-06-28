using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunEffectManager : BaseManager<GunEffectManager>
{
    private Dictionary<string, string> registeredEffects = new Dictionary<string, string>();
    private bool isInitialized;

    /// <summary>
    /// 注册特效：绑定 poolKey ↔ prefabPath，自动在 PoolManager 中创建池
    /// </summary>
    public void RegisterEffect(string poolKey, string prefabPath, int poolSize = 10)
    {
        if (registeredEffects.ContainsKey(poolKey)) return;
        registeredEffects[poolKey] = prefabPath;
        PoolManager.GetInstance().CreateEffectPool(poolKey, prefabPath, poolSize);
    }

    public void Initialize()
    {
        if (isInitialized) return;

        GameEventBus.GetInstance().Subscribe<Shooted>(GameEventType.OnShooted, OnWeaponFired);
        // 后续订阅其他事件：弹孔、弹壳...
        // GameEventBus.GetInstance().Subscribe<BulletHitEventData>(GameEventType.OnBulletHit, OnBulletHit);
        GameEventBus.GetInstance().Subscribe<BulletHitEventData>(GameEventType.OnBulletHit, OnBulletHit);

        isInitialized = true;
    }

    /// <summary>
    /// 在指定位置播放特效，播完自动归还池
    /// </summary>
    public void PlayAt(string poolKey, Vector3 position, Quaternion rotation, int layer = 0)
    {
        ParticleSystem ps = PoolManager.GetInstance().GetEffect(poolKey);
        if (ps == null) return;

        ps.transform.position = position;
        ps.transform.rotation = rotation;
        if (layer != 0)
            ps.gameObject.layer = layer;

        var allPS = ps.GetComponentsInChildren<ParticleSystem>();
        float maxDuration = 0f;
        foreach (var p in allPS)
            if (p.main.duration > maxDuration) maxDuration = p.main.duration;

        if (maxDuration <= 0f) maxDuration = 2f;

        MonoManager.GetInstance().StartCoroutine(ReleaseAfterDelay(poolKey, ps, maxDuration));
    }

    private void OnWeaponFired(Shooted data)
    {
        PlayAt("MuzzleFlash", data.FirePointPosition, data.FirePointRotation, data.EffectLayer);
    }

    private void OnBulletHit(BulletHitEventData data)
    {
        // PlayAt("BulletHit",data.HitInfo.transform.position,data.HitInfo.qua)
        PlayAt("BulletImpact", data.HitInfo.point, Quaternion.LookRotation(data.HitInfo.normal));
        //Debug.Log("子弹击中");
    }

    private IEnumerator ReleaseAfterDelay(string poolKey, ParticleSystem ps, float delay)
    {
        yield return new WaitForSeconds(delay);
        PoolManager.GetInstance().ReleaseEffect(poolKey, ps);
    }
}
