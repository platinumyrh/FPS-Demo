using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WeaponType
{
    Primary,    // 主武器 (如步枪、散弹枪)
    Secondary,  // 副武器 (如手枪)
    Special     // 特殊武器 (如火箭筒、雷、近战)
}

/// <summary>
/// 所有枪械的基类。纯数据统一存放于 WeaponData (ScriptableObject)，
/// 支持热更新和集中管理。GunBase 只保留运行时状态和场景引用。
/// </summary>
public class GunBase : MonoBehaviour
{
    [Header("武器数据 (ScriptableObject)")]
    [SerializeField] private WeaponData weaponData;

    [Header("动画设置")]
    [Tooltip("拖入该枪械对应的第一人称 Override Controller (如 OC_LPSP_PCH_RL_01)")]
    [SerializeField] private AnimatorOverrideController weaponOverrideController;
    [Tooltip("该枪对应的第三人称 Override Controller")]
    [SerializeField] private AnimatorOverrideController tpWeaponOverrideController;
    private GunAnimationController gunAnimController;

    [Header("枪械模型")]
    [SerializeField] private string modelPath = "Model/Guns/P_LPSP_WEP_AR_01";

    [Header("开火相关")]
    [SerializeField] private Transform firePoint;

    // ===== 运行时弹药状态 =====
    private int currentAmmoInClip;
    private int totalAmmo;

    public bool isReloading { get; private set; } = false;
    public bool isInspecting { get; private set; } = false;
    public bool isEmpty => currentAmmoInClip <= 0;

    protected void Awake()
    {
        gunAnimController = GetComponent<GunAnimationController>();
        firePoint = FindChildDeep(transform, "SOCKET_Muzzle");

        if (firePoint == null)
            Debug.LogError($"[GunBase] 在 {gameObject.name} 的固定路径下未找到 SOCKET_Muzzle，请检查拼写！");

        if (weaponData != null)
        {
            currentAmmoInClip = weaponData.maxAmmoInClip;
            totalAmmo = weaponData.maxAmmoInClip * 3;
        }
        else
        {
            Debug.LogError($"[GunBase] {gameObject.name} 缺少 WeaponData 引用！使用默认值。");
            currentAmmoInClip = 30;
            totalAmmo = 90;
        }
    }

    #region 数据封装与访问

    public int GetCurrentAmmoInClip() => currentAmmoInClip;

    public int GetMaxAmmoInClip() => weaponData != null ? weaponData.maxAmmoInClip : 30;

    public int GetTotalAmmo() => totalAmmo;

    public WeaponType GetWeaponType() => weaponData != null ? weaponData.weaponType : WeaponType.Primary;

    public string GetModelPath() => modelPath;

    public virtual string GetWeaponId() => gameObject.name;

    public void RestoreAmmoState(int currentAmmo, int totalAmmo)
    {
        int max = weaponData != null ? weaponData.maxAmmoInClip : 30;
        this.currentAmmoInClip = Mathf.Clamp(currentAmmo, 0, max);
        this.totalAmmo = Mathf.Max(0, totalAmmo);
    }

    #endregion

    /// <summary>打包当前枪械的 UI 数据</summary>
    public WeaponUIData CreateUIData()
    {
        if (weaponData == null) return null;
        return new WeaponUIData(
            gameObject.name,
            currentAmmoInClip,
            weaponData.maxAmmoInClip,
            totalAmmo,
            weaponData.iconWeaponBody,
            weaponData.iconGrip,
            weaponData.iconMagazine,
            weaponData.iconLaser,
            weaponData.iconMuzzle,
            weaponData.iconScope
        );
    }

    public virtual void FireWeapon()
    {
        if (weaponData == null) return;

        if (currentAmmoInClip != 0)
        {
            currentAmmoInClip--;

            // 在 FireWeapon() 的 currentAmmoInClip-- 之后，加：
            var recoilCtrl = GetComponentInParent<RecoilController>();
            if (recoilCtrl != null)
            {
                recoilCtrl.ApplyRecoil(
                    weaponData.recoilVertical,
                    weaponData.recoilHorizontalMin,
                    weaponData.recoilHorizontalMax,
                    weaponData.recoilRecoverySpeed,
                    weaponData.recoilMaxVertical
                );
            }
            else
            {
                Debug.Log("后座力管理器为空");
            }

            if (gunAnimController != null)
                gunAnimController.PlayShoot();

            GameEventBus.GetInstance().Publish(GameEventType.OnShooted,
                new Shooted(firePoint.position, Quaternion.LookRotation(-firePoint.up),
                    weaponData.fireSoundPath, firePoint.gameObject.layer));

            ExcuteShotgunSpread();
        }

        // UI 更新
        WeaponUIData uiData = new WeaponUIData(gameObject.name, currentAmmoInClip,
            weaponData.maxAmmoInClip, totalAmmo);
        GameEventBus.GetInstance().Publish<WeaponUIData>(GameEventType.OnWeaponUIUpdate, uiData);
    }

    private void ExcuteShotgunSpread()
    {
        if (firePoint == null || weaponData == null) return;

        Vector3 baseForward = -firePoint.up;
        Vector3 rightAxis = firePoint.right;
        Vector3 upAxis = firePoint.forward;

        for (int i = 0; i < weaponData.pelletCount; i++)
        {
            Vector3 shootDirection = baseForward;

            if (weaponData.spreadAngle > 0f)
            {
                Vector2 randomPoint = Random.insideUnitCircle;
                shootDirection += rightAxis * randomPoint.x * weaponData.spreadAngle;
                shootDirection += upAxis * randomPoint.y * weaponData.spreadAngle;
                shootDirection.Normalize();
            }
            SingleRaycastHit(shootDirection);
        }
    }

    private void SingleRaycastHit(Vector3 direction)
    {
        if (weaponData == null) return;

        Ray ray = new Ray(firePoint.position, direction);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, weaponData.fireRange, weaponData.hitLayers))
        {
            DrawDebugLaser(ray.origin, hit.point, Color.green, 0.5f);
            BulletHitEventData hitData = new BulletHitEventData(hit, weaponData.weaponDamage, this);
            GameEventBus.GetInstance().Publish<BulletHitEventData>(GameEventType.OnBulletHit, hitData);
        }
        else
        {
            Vector3 endPoint = ray.origin + (direction * weaponData.fireRange);
            DrawDebugLaser(ray.origin, endPoint, Color.red, 0.5f);
        }
    }

    public virtual void ReloadWeapon()
    {
        if (weaponData == null) return;
        if (isReloading || isInspecting || totalAmmo <= 0 || currentAmmoInClip == weaponData.maxAmmoInClip) return;

        isReloading = true;
        SoundManager.GetInstance().PlaySFX(isEmpty ? weaponData.emptyReloadSoundPath : weaponData.reloadSoundPath);

        if (gunAnimController != null)
            gunAnimController.PlayReload(isEmpty);
    }

    public virtual void InspectWeapon()
    {
        if (isReloading || isInspecting) return;
        isInspecting = true;

        if (gunAnimController != null)
            gunAnimController.PlayInspect();
    }

    public virtual void OnReloadComplete()
    {
        if (!isReloading || weaponData == null) return;

        int ammoNeeded = weaponData.maxAmmoInClip - currentAmmoInClip;
        int ammoToReload = Mathf.Min(ammoNeeded, totalAmmo);

        currentAmmoInClip += ammoToReload;
        totalAmmo -= ammoToReload;
        isReloading = false;

        WeaponUIData uiData = new WeaponUIData(gameObject.name, currentAmmoInClip,
            weaponData.maxAmmoInClip, totalAmmo);
        GameEventBus.GetInstance().Publish<WeaponUIData>(GameEventType.OnWeaponUIUpdate, uiData);
    }

    public void InitDefaultUIData()
    {
        if (weaponData == null) return;
        WeaponUIData uiData = new WeaponUIData(gameObject.name, currentAmmoInClip,
            weaponData.maxAmmoInClip, totalAmmo);
        GameEventBus.GetInstance().Publish<WeaponUIData>(GameEventType.OnWeaponUIUpdate, uiData);
    }

    public AnimatorOverrideController GetWeaponOverrideController() => weaponOverrideController;
    public AnimatorOverrideController GetTPWeaponOverrideController() => tpWeaponOverrideController;

    #region 辅助方法

    private void DrawDebugLaser(Vector3 start, Vector3 end, Color color, float duration)
    {
        Debug.DrawLine(start, end, color, duration);
    }

    private Transform FindChildDeep(Transform parent, string targetName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == targetName) return child;
            Transform result = FindChildDeep(child, targetName);
            if (result != null) return result;
        }
        return null;
    }

    #endregion
}
