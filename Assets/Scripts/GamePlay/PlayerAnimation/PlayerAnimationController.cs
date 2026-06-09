using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

public class PlayerAnimationController : MonoBehaviour
{
    private Animator animator;
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
       
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
    }

    public void ApplyAim(bool isAiming)
    {
        // 如果是 1D/2D 混合树控制瞄准姿态：
        float targetValue = isAiming ? 1f : 0f;
        animator.SetFloat("Aiming", targetValue, 0.05f, Time.deltaTime); // 用 dampTime 让举枪动作平滑
    }

    public void ApplyShoot()
    {
        animator.CrossFade("Fire", 0.1f, -1, 0f); // 直接播放射击动画，假设动画状态机里有个叫 Shoot 的状态
    }
    public void StopShoot()
    {
        animator.Play("Idle"); // 直接切回待机动画，假设动画状态机里有个叫 Idle 的状态
    }
     public void ApplyReload()
    {
        animator.CrossFade("Reload", 0.1f); // 平滑过渡到换弹动画
    }
     public void ApplyInspect()
    {
        
    }





}
