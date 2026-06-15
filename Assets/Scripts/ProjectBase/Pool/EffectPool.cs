using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 特效对象池，专门用来存放特效对象的对象池
/// </summary>
public class EffectPool :BasePool<ParticleSystem>
{
    public string prefabPath { get; private set; }
    public Transform parentTransform { get; set; }

    public EffectPool(string prefabPath, Transform parent = null, int maxSize = 10)
     : base(null, null, null, null, maxSize)
    {
        this.prefabPath = prefabPath;
        this.parentTransform = parent;

        createFunc = CreateEffect;
        onGet = OnGetEffect;
        onRelease = OnReleaseEffect;
        onDestroy = OnDestroyEffect;
    }

    private ParticleSystem CreateEffect()
    {
        GameObject prefab = Resources.Load<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"找不到特效预制体: {prefabPath}");
            return null;
        }

        GameObject obj = GameObject.Instantiate(prefab);
        obj.name = prefab.name;

        if (parentTransform != null)
        {
            obj.transform.SetParent(parentTransform);
        }

        ParticleSystem ps = obj.GetComponentInChildren<ParticleSystem>();
        if (ps == null)
        {
            Debug.LogError($"预制体 {prefabPath} 没有 ParticleSystem 组件");
            GameObject.Destroy(obj);
            return null;
        }

        return ps;
    }
    private void OnGetEffect(ParticleSystem effect)
    {
        effect.gameObject.SetActive(true);
        var allPS = effect.GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in allPS)
            ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        foreach (var ps in allPS)
            ps.Play();
    }
    private void OnReleaseEffect(ParticleSystem effect)
    {
        var allPS = effect.GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in allPS)
            ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);

        effect.gameObject.SetActive(false);

        if (parentTransform != null)
        {
            effect.transform.SetParent(parentTransform, false);
        }
    }
    private void OnDestroyEffect(ParticleSystem effect)
    {
        GameObject.Destroy(effect.gameObject);
    }
}
