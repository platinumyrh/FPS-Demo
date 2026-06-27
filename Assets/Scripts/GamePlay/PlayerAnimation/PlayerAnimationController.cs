using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

public class PlayerAnimationController : MonoBehaviour
{
    private Animator animator;
    [SerializeField] private Animator bodyAnimator;

    


    // 新增：用于在 Update 中记录当前的瞄准目标值
    private float targetAimValue = 0f;

    // 第三人称上半身随鼠标俯仰倾斜
    public float pitchAngle;
    private Transform[] spineBones = new Transform[3];

    // 第三人称武器跟随
    [SerializeField] private Transform tpWeaponModel;
    private Transform handRBone;
    private Vector3 gripPosOffset;
    private Quaternion gripRotOffset;

    void Start()
    {
        animator = GetComponent<Animator>();

        if (bodyAnimator != null)
        {
            string[] spineNames = { "spine_01", "spine_02", "spine_03" };
            for (int i = 0; i < spineNames.Length; i++)
                spineBones[i] = FindDeepChild(bodyAnimator.transform, spineNames[i]);

            handRBone = FindDeepChild(bodyAnimator.transform, "hand_r");

            // 记录武器相对手的初始握持偏移
            if (tpWeaponModel != null && handRBone != null)
            {
                gripPosOffset = handRBone.InverseTransformPoint(tpWeaponModel.position);
                gripRotOffset = Quaternion.Inverse(handRBone.rotation) * tpWeaponModel.rotation;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        float currentAim = animator.GetFloat("Aiming");
        float nextAim = Mathf.MoveTowards(currentAim, targetAimValue, Time.deltaTime / 0.05f);
        animator.SetFloat("Aiming", nextAim);
    }

    /// <summary>
    /// LateUpdate 在 Animator 更新完骨骼之后执行，确保我们的旋转不会被动画覆盖
    /// </summary>
    void LateUpdate()
    {
        // 默认姿态上仰补偿 + 鼠标俯仰
        float spineDefaultOffset = -12f;
        float tiltAngle = -pitchAngle * 0.5f + spineDefaultOffset;
        tiltAngle = Mathf.Clamp(tiltAngle, -30f, 30f);

        float[] ratios = { 0.20f, 0.35f, 0.45f };

        for (int i = 0; i < spineBones.Length; i++)
        {
            if (spineBones[i] != null)
            {
                float boneTilt = tiltAngle * ratios[i];
                spineBones[i].localRotation *= Quaternion.Euler(0f, 0f, -boneTilt);
            }
        }

        // 第三人称武器跟随 hand_r，保留初始握持偏移
        if (tpWeaponModel != null && handRBone != null)
        {
            tpWeaponModel.position = handRBone.TransformPoint(gripPosOffset);
            tpWeaponModel.rotation = handRBone.rotation * gripRotOffset;
        }
    }

    public void ApplyLocomotion(Vector2 inputDir,bool isRunning)
    {
        // 将输入方向转换为动画参数
        float forwardAmount = inputDir.y; // 前后输入
        float strafeAmount = inputDir.x;  // 左右输入
        // 设置动画参数
        animator.SetBool("Running", isRunning);
        animator.SetFloat("Horizontal", forwardAmount, 0.1f, Time.deltaTime); // 平滑过渡
        animator.SetFloat("Vertical", strafeAmount, 0.1f, Time.deltaTime);  // 平滑过渡
        animator.SetFloat("Movement", 1,0.1f, Time.deltaTime);
        animator.SetFloat("Play Rate Locomotion", 1, 0.1f, Time.deltaTime);

        if (bodyAnimator != null)
        {
            bodyAnimator.SetFloat("Horizontal", forwardAmount);
            bodyAnimator.SetFloat("Vertical", strafeAmount);
            bodyAnimator.SetBool("Running", isRunning);
        }
        else
        {
            Debug.Log("bodyAnimator为空");
        }
    }

    public void ApplyAim(bool isAiming)
    {
        // 如果是 1D/2D 混合树控制瞄准姿态：
        targetAimValue = isAiming ? 1f : 0f;

        animator.SetBool("Aim", isAiming);
    }
    public void ApplyCrouch(bool isCrouching)
    {
        animator.SetBool("Crouching", isCrouching);
    }


    public void ApplyShoot()
    {
        animator.CrossFade("Fire", 0.1f, -1, 0f); // 直接播放射击动画，假设动画状态机里有个叫 Shoot 的状态
    }
    public void StopShoot()
    {
        //animator.Play("Idle"); // 直接切回待机动画，假设动画状态机里有个叫 Idle 的状态
    }
     public void ApplyReload(bool isEmpty )
    {
        if (isEmpty)
        {
            animator.CrossFade("Reload Empty", 0.1f); // 平滑过渡到空弹换弹动画
        }
        else
        {
            animator.CrossFade("Reload", 0.1f); // 平滑过渡到换弹动画
        }
        
    }
     public void ApplyInspect()
    {
       animator.CrossFade("Inspect", 0.1f); // 平滑过渡到检查枪械动画   
    }

    /// <summary>
    /// 动画事件回调：由换弹动画结束时通过 Animation Event 自动调用
    /// </summary>
    public void OnAnimationEndedReload()
    {
        //Debug.Log($"[PlayerController] 收到换弹动画结束事件，正在通知武器...");

        // 往事件总线广播：换弹动画结束了
        GameEventBus.GetInstance().Publish<InputActionData>(
            GameEventType.OnReloadComplete, // 你可以自定义一个这样的枚举事件
            new InputActionData("ReloadComplete")
        );
    }

    /// <summary>
    /// 递归查找子节点（不限深度）
    /// </summary>
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

}
