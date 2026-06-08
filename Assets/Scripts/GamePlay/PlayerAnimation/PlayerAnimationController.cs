using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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


   
   
}
