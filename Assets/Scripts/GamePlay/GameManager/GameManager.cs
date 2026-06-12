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
        UIManager.GetInstance().ShowPanel("P_LPSP_UI_Canvas", UI_Layer.Bottom);

    }
}
