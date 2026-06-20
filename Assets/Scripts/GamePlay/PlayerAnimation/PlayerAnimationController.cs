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
    private Transform spineBone;
    private Quaternion spineInitialLocalRot;

    void Start()
    {
        animator = GetComponent<Animator>();

        // 获取第三人称身体的脊椎骨骼（Generic 只能用名字查找）
        if (bodyAnimator != null)
        {
            spineBone = FindDeepChild(bodyAnimator.transform, "spine_03");
            if (spineBone != null)
            {
                spineInitialLocalRot = spineBone.localRotation;
            }
            else
            {
                Debug.LogWarning("[PlayerAnimationController] 未找到 spine_03，上半身倾斜失效");
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        // 每帧在 Update 里，让当前的 Aiming 参数平滑逼近我们的 targetAimValue目标
        float currentAim = animator.GetFloat("Aiming");
        // 0.05f 是过渡时间，Time.deltaTime / 0.05f 可以做到匀速且精准到达 0 或 1
        float nextAim = Mathf.MoveTowards(currentAim, targetAimValue, Time.deltaTime / 0.05f);
        animator.SetFloat("Aiming", nextAim);
    }

    /// <summary>
    /// LateUpdate 在 Animator 更新完骨骼之后执行，确保我们的旋转不会被动画覆盖
    /// </summary>
    void LateUpdate()
    {
        if (spineBone != null)
        {
            // 抬头(pitch为负) → 脊椎后仰(X正)，低头(pitch为正) → 脊椎前倾(X负)
            float tiltAngle = -pitchAngle * 0.5f;
            tiltAngle = Mathf.Clamp(tiltAngle, -30f, 30f);
            spineBone.localRotation = spineInitialLocalRot * Quaternion.Euler(0f, 0f, -tiltAngle);
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
