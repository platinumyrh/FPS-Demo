using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{

    CharacterController characterController;
    PlayerWeaponManager playerWeaponManager;
    PlayerAnimationController playerAnimationController;

    private void Awake()
    {
        //  UIManager.GetInstance().ShowPanel("P_LPSP_UI_Canvas", UI_Layer.Bottom);

        UIManager.GetInstance().ShowPanel("P_LPSP_UI_Canvas", UI_Layer.Bottom);

        PoolManager.GetInstance().Initialize();

        GunEffectManager.GetInstance().RegisterEffect("MuzzleFlash", "VFX/FirePointFlame", 15);
        GunEffectManager.GetInstance().RegisterEffect("BulletImpact", "VFX/P_IMP_Metal", 15);
        GunEffectManager.GetInstance().Initialize();

        PlayerSoundManager.GetInstance().Initialize();
    }
}
