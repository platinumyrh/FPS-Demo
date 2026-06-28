using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    #region 第一人称动画

    private Animator animator;
    private float targetAimValue = 0f;

    // Animator 参数哈希缓存
    private static readonly int HashAiming    = Animator.StringToHash("Aiming");
    private static readonly int HashRunning   = Animator.StringToHash("Running");
    private static readonly int HashHorizontal = Animator.StringToHash("Horizontal");
    private static readonly int HashVertical  = Animator.StringToHash("Vertical");
    private static readonly int HashMovement  = Animator.StringToHash("Movement");
    private static readonly int HashPlayRate  = Animator.StringToHash("Play Rate Locomotion");
    private static readonly int HashAim       = Animator.StringToHash("Aim");
    private static readonly int HashCrouching = Animator.StringToHash("Crouching");
    private static readonly int HashFire      = Animator.StringToHash("Fire");
    private static readonly int HashReload    = Animator.StringToHash("Reload");
    private static readonly int HashReloadEmpty = Animator.StringToHash("Reload Empty");
    private static readonly int HashInspect   = Animator.StringToHash("Inspect");
    // 第三人称身体动画
    private static readonly int HashTPFire       = Animator.StringToHash("Fire");
    private static readonly int HashTPReload     = Animator.StringToHash("Reload");
    private static readonly int HashTPReloadEmpty = Animator.StringToHash("Reload_Empty");
    private static readonly int HashTPCrouching  = Animator.StringToHash("Crouching");
    private static readonly int HashTPLand       = Animator.StringToHash("Land");

    void Update()
    {
        SmoothAimingParameter();
    }

    private void SmoothAimingParameter()
    {
        float currentAim = animator.GetFloat(HashAiming);
        float nextAim = Mathf.MoveTowards(currentAim, targetAimValue, Time.deltaTime / 0.05f);
        animator.SetFloat(HashAiming, nextAim);
    }

    public void ApplyLocomotion(Vector2 inputDir, bool isRunning)
    {
        float forwardAmount = inputDir.y;
        float strafeAmount = inputDir.x;

        animator.SetBool(HashRunning, isRunning);
        animator.SetFloat(HashHorizontal, forwardAmount, 0.1f, Time.deltaTime);
        animator.SetFloat(HashVertical, strafeAmount, 0.1f, Time.deltaTime);
        animator.SetFloat(HashMovement, 1, 0.1f, Time.deltaTime);
        animator.SetFloat(HashPlayRate, 1, 0.1f, Time.deltaTime);

        SyncBodyLocomotion(forwardAmount, strafeAmount, isRunning);
    }

    public void ApplyAim(bool isAiming)
    {
        targetAimValue = isAiming ? 1f : 0f;
        animator.SetBool(HashAim, isAiming);
    }

    public void ApplyCrouch(bool isCrouching)
    {
        animator.SetBool(HashCrouching, isCrouching);
        if (bodyAnimator != null)
            bodyAnimator.SetBool(HashTPCrouching, isCrouching);
    }

    public void ApplyShoot()
    {
        animator.CrossFade(HashFire, 0.1f, -1, 0f);
        if (bodyAnimator != null)
            bodyAnimator.CrossFade(HashTPFire, 0.1f, -1, 0f);
        if (tpGunAnimController != null)
            tpGunAnimController.PlayShoot();

        // 在 TP 枪口位置也播放枪口火焰（fireSoundPath=null 避免音效重复播放）
        if (activeTPModel != null)
        {
            Transform tpMuzzle = FindDeepChild(activeTPModel, "SOCKET_Muzzle");
            if (tpMuzzle != null)
            {
                GameEventBus.GetInstance().Publish(GameEventType.OnShooted,
                    new Shooted(tpMuzzle.position, Quaternion.LookRotation(-tpMuzzle.up), null, activeTPModel.gameObject.layer));
            }
        }
    }

    public void StopShoot() { }

    public void ApplyReload(bool isEmpty)
    {
        animator.CrossFade(isEmpty ? HashReloadEmpty : HashReload, 0.1f);
        if (bodyAnimator != null)
            bodyAnimator.CrossFade(isEmpty ? HashTPReloadEmpty : HashTPReload, 0.1f);
        if (tpGunAnimController != null)
            tpGunAnimController.PlayReload(isEmpty);
    }

    public void ApplyInspect()
    {
        animator.CrossFade(HashInspect, 0.1f);
        if (tpGunAnimController != null)
            tpGunAnimController.PlayInspect();
    }

    public void OnAnimationEndedReload()
    {
        GameEventBus.GetInstance().Publish<InputActionData>(
            GameEventType.OnReloadComplete,
            new InputActionData("ReloadComplete")
        );
    }

    #endregion

    // ────────────────────────────────────────────────

    #region 第三人称动画

    [SerializeField] private Animator bodyAnimator;
    [SerializeField] private Transform tpWeaponContainer; // TP 武器容器（含所有枪模型，初始全部失活）

    private Transform[] spineBones = new Transform[3];
    private Transform handRBone;
    private Vector3 gripPosOffset;
    private Animator tpGunAnimator;
    private GunAnimationController tpGunAnimController;
    private Transform activeTPModel; // 当前激活的 TP 枪模型

    // 脊椎倾斜比例（静态只读，避免每帧分配数组）
    private static readonly float[] SpineRatios = { 0.20f, 0.35f, 0.45f };
    private static readonly float SpineDefaultOffset = -12f;

    void Start()
    {
        animator = GetComponent<Animator>();
        InitThirdPersonBones();
    }

    void LateUpdate()
    {
        UpdateTPSpine();
        UpdateTPWeapon();
    }

    private void InitThirdPersonBones()
    {
        if (bodyAnimator == null) return;

        string[] spineNames = { "spine_01", "spine_02", "spine_03" };
        for (int i = 0; i < spineNames.Length; i++)
            spineBones[i] = FindDeepChild(bodyAnimator.transform, spineNames[i]);

        handRBone = FindDeepChild(bodyAnimator.transform, "hand_r");
    }

    private void UpdateTPSpine()
    {
        // 从第一人称 Animator 的 localRotation 读取当前俯仰角
        float pitch = animator.transform.localRotation.eulerAngles.x;
        if (pitch > 180f) pitch -= 360f;

        // 直接用 FP 的 pitch，不做额外缩放和钳制
        float tiltAngle = -pitch + SpineDefaultOffset;

        for (int i = 0; i < spineBones.Length; i++)
        {
            if (spineBones[i] != null)
            {
                float boneTilt = tiltAngle * SpineRatios[i];
                spineBones[i].localRotation = spineBones[i].localRotation * Quaternion.Euler(0f, 0f, -boneTilt);
            }
        }
    }

    private void UpdateTPWeapon()
    {
        if (activeTPModel == null) return;

        // 枪口朝向 = FP 相机方向（含 yaw + pitch），跟准星对齐
        activeTPModel.rotation = animator.transform.rotation;

        // 枪位置 = hand_r + 握持偏移，跟随手臂动画
        if (handRBone != null)
        {
            activeTPModel.position = handRBone.TransformPoint(gripPosOffset);
        }
    }

    /// <summary>
    /// 由 PlayerWeaponManager 在切枪时调用，切换 TP 身体动画 + 激活对应枪模型
    /// </summary>
    public void SetTPWeaponOverride(AnimatorOverrideController bodyOverrider, string weaponId)
    {
        // 切换身体动画
        if (bodyAnimator != null && bodyOverrider != null)
            bodyAnimator.runtimeAnimatorController = bodyOverrider;

        // 切换枪模型：失活旧模型，激活新模型
        if (tpWeaponContainer == null) return;

        if (activeTPModel != null)
            activeTPModel.gameObject.SetActive(false);

        Transform newModel = tpWeaponContainer.Find(weaponId);
        if (newModel != null)
        {
            newModel.gameObject.SetActive(true);
            activeTPModel = newModel;

            tpGunAnimator = newModel.GetComponent<Animator>();
            tpGunAnimController = newModel.GetComponent<GunAnimationController>();

            // 抓取握持偏移
            if (handRBone != null)
                gripPosOffset = handRBone.InverseTransformPoint(newModel.position);
        }
    }

    public void PlayLand()
    {
        if (bodyAnimator != null)
            bodyAnimator.CrossFade(HashTPLand, 0.1f);
    }

    private void SyncBodyLocomotion(float forwardAmount, float strafeAmount, bool isRunning)
    {
        if (bodyAnimator != null)
        {
            bodyAnimator.SetFloat(HashHorizontal, forwardAmount);
            bodyAnimator.SetFloat(HashVertical, strafeAmount);
            bodyAnimator.SetBool(HashRunning, isRunning);
        }
    }

    private Transform FindDeepChild(Transform parent, string targetName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == targetName) return child;
            Transform result = FindDeepChild(child, targetName);
            if (result != null) return result;
        }
        return null;
    }

    #endregion
}
