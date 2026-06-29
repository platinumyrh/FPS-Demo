using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家音效管理器：订阅游戏事件，调用 SoundManager 播放对应音效
/// 结构仿照 GunEffectManager
/// </summary>
public class PlayerSoundManager : BaseManager<PlayerSoundManager>
{
    private bool isInitialized;

    // 脚步相关
    private bool isMoving;
    private bool isRunning;
    private AudioSource footstepSource;

    public void Initialize()
    {
        if (isInitialized) return;

        GameEventBus.GetInstance().Subscribe<Shooted>(GameEventType.OnShooted, OnWeaponFired);
        GameEventBus.GetInstance().Subscribe<BulletHitEventData>(GameEventType.OnBulletHit, OnBulletHit);
        GameEventBus.GetInstance().Subscribe<InputActionData>(GameEventType.OnJumpInput, OnJump);
        GameEventBus.GetInstance().Subscribe<InputActionData>(GameEventType.OnCrouchInput, OnCrouch);
        GameEventBus.GetInstance().Subscribe<InputEventData>(GameEventType.OnMoveInput, OnMoveInput);
        GameEventBus.GetInstance().Subscribe<InputEventData>(GameEventType.OnMoveCanceled, OnMoveCanceled);
        GameEventBus.GetInstance().Subscribe<InputHoldingData>(GameEventType.OnRunInput, OnRunInput);

        isInitialized = true;
    }

    #region 移动音效
    private void OnMoveInput(InputEventData data)
    {
        bool wasMoving = isMoving;
        isMoving = data.MoveDirection.magnitude > 0.1f;

        if (isMoving && !wasMoving)
        {
            PlayFootstepLoop();
        }
    }

    private void OnMoveCanceled(InputEventData data)
    {
        isMoving = false;
        StopFootstep();
    }

    private void OnRunInput(InputHoldingData data)
    {
        isRunning = data.IsHolding;
        if (!isMoving) return;

        StopFootstep();
        PlayFootstepLoop();
    }

    private void PlayFootstepLoop()
    {
        string sfxPath = isRunning ? "SFX/Movement/Running" : "SFX/Movement/Walking";
        SoundManager.GetInstance().PlaySFX(sfxPath, (source) =>
        {
            source.loop = true;
            footstepSource = source;
        });
    }

    private void StopFootstep()
    {
        if (footstepSource != null)
        {
            SoundManager.GetInstance().StopSFX(footstepSource);
            footstepSource = null;
        }
    }
    #endregion

    #region 武器音效
    private void OnWeaponFired(Shooted data)
    {
        if (!string.IsNullOrEmpty(data.FireSoundPath))
            SoundManager.GetInstance().PlaySFX(data.FireSoundPath);
       
       
    }

    private void OnBulletHit(BulletHitEventData data)
    {
        string tag = data.HitInfo.collider?.tag;
        switch (tag)
        {
            case "Target":
                SoundManager.GetInstance().PlaySFX("SFX/Damageabel/Target/S_TargetGoDown");
                break;
            default:
                SoundManager.GetInstance().PlaySFX("SFX/Guns/Bullet_Impact/Impact_Bullet_01");
                break;
        }
    }

    #endregion

    private void OnJump(InputActionData data)
    {
        SoundManager.GetInstance().PlaySFX("SFX/Movement/Jump");
    }

    private void OnCrouch(InputActionData data)
    {
        SoundManager.GetInstance().PlaySFX("SFX/Movement/Crouch");
    }
}
