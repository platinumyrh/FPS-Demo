using UnityEngine;

/// <summary>
/// 武器纯数据 (ScriptableObject)，和运行时逻辑分离。
/// 右键 → Create → FPS → Weapon Data 创建新武器数据。
/// 支持热更：通过 Addressables / AssetBundle 动态加载。
/// </summary>
[CreateAssetMenu(fileName = "WD_", menuName = "FPS/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("基础属性")]
    public WeaponType weaponType = WeaponType.Primary;
    public string displayName = "";
    public float weaponDamage = 25f;
    public float fireRange = 100f;
    public LayerMask hitLayers = 1 << 0;

    [Header("弹夹")]
    public int maxAmmoInClip = 30;

    [Header("弹道 & 散布")]
    public int pelletCount = 1;
    [Range(0f, 0.5f)] public float spreadAngle = 0.02f;

    [Header("后坐力")]
    public float recoilVertical = 0.5f;      // 每发上跳角度
    public float recoilHorizontalMin = -0.1f; // 水平随机左偏
    public float recoilHorizontalMax = 0.1f;  // 水平随机右偏
    public float recoilRecoverySpeed = 8f;    // 回中速度
    public float recoilMaxVertical = 5f;      // 最大累积上跳（防无限飘）

    [Header("音效")]
    public string fireSoundPath = "SFX/Guns/Fire/AR_Fire";
    public string reloadSoundPath = "SFX/Weapon/Reload";
    public string emptyReloadSoundPath = "SFX/Weapon/Reload_Empty";

    [Header("特效")]
    public string muzzleFlashPath = "MuzzleFlash";

    [Header("UI 图标")]
    public Sprite iconWeaponBody;
    public Sprite iconGrip;
    public Sprite iconMagazine;
    public Sprite iconLaser;
    public Sprite iconMuzzle;
    public Sprite iconScope;
}
