using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunAnimationController : MonoBehaviour
{
    

    private Animator gunAnimator; // 枪械本身的 Animator 组件

    // 这里可以定义枪械独有的平滑参数，比如枪支开火后的回弹速度等
    private float targetRecoil = 0f;

    private void Awake()
    {
        gunAnimator = GetComponent<Animator>();
    }
    void Start()
    {
        
    }

   
    void Update()
    {
        
    }

    public void PlayShoot()
    {
        // 建议使用 Hash 值提升性能
        gunAnimator.CrossFade("Fire", 0.1f, -1, 0f); // 直接播放射击动画，假设动画状态机里有个叫 Shoot 的状态
        //Debug.Log("[动画] 播放射击动画");
    }

    public void PlayReload(bool isEmpty)
    {
        if (isEmpty)
        {
            gunAnimator.CrossFade("Reload Empty", 0.1f);
            Debug.Log("[动画] 播放空弹换弹动画");
            return;
        }
        else
        {
            gunAnimator.CrossFade("Reload", 0.1f);
            Debug.Log("[动画] 播放换弹动画");
        }
           
        
    }

    public void PlayInspect()
    {
        //gunAnimator.CrossFade("Inspect", 0.1f);
    }

    public void SetAimingValue(float value)
    {
        // 如果枪自身的模型在瞄准时也有特定的参数混合
       // gunAnimator.SetFloat("Aiming", value);
    }
}
