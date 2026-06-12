using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 负责管理玩家的武器系统，包括切换武器、拾取武器、丢弃武器等功能。
/// 它与玩家的输入系统和动画系统紧密结合，确保玩家在游戏中能够流畅地使用各种武器，并且在切换武器时能够正确地触发相应的动画和效果。
/// </summary>
public class PlayerWeaponManager : MonoBehaviour
{
    [Header("武器列表")]
    [Tooltip("按顺序把玩家身上的枪械物体拖进来（确保它们身上挂了 GunBase）")]
    [SerializeField] private List<GunBase> weaponSlots = new List<GunBase>();

    private Animator playerAnimator;     // 玩家手部的 Animator 组件
    private int currentWeaponIndex = -1;  // 当前装备的武器索引
    private bool isSwitching = false;     // 状态锁：防止在切枪动画播放时连续滚动重复触发

    private PlayerController playerController; // 引用玩家控制器，方便后续扩展（如切枪时调整移动速度等）
    void Start()
    {
        playerAnimator = GetComponentInChildren<Animator>();

        playerController = GetComponent<PlayerController>();

        if (weaponSlots.Count > 0 && playerAnimator != null)
        {
            currentWeaponIndex = 0;
            InitializeWeapon(currentWeaponIndex);
        }
    }

    private void InitializeWeapon(int index)
    {
        weaponSlots[index].gameObject.SetActive(true);
        playerAnimator.runtimeAnimatorController = weaponSlots[index].GetWeaponOverrideController();
        playerAnimator.SetBool("Holstered", false);

        // 【核心解耦点】：主动把当前枪“喂”给 Controller
        if (playerController != null)
        {
            playerController.currentWeapon = weaponSlots[index];
        }

        GunBase gun = weaponSlots[index];
        WeaponUIData weaponUIData = gun.CreateUIData();
        GameEventBus.GetInstance().Publish<WeaponUIData>(GameEventType.OnWeaponUIUpdate, weaponUIData);
       
    }

    private void OnEnable()
    {
        // 订阅滚轮切枪事件
        GameEventBus.GetInstance().Subscribe<InputActionData>(GameEventType.OnNextWeapon, OnNextWeaponTriggered);
        GameEventBus.GetInstance().Subscribe<InputActionData>(GameEventType.OnPrevWeapon, OnPrevWeaponTriggered);
    }

    private void OnDisable()
    {
        // 取消订阅，防止内存泄漏
        GameEventBus.GetInstance().Unsubscribe<InputActionData>(GameEventType.OnNextWeapon, OnNextWeaponTriggered);
        GameEventBus.GetInstance().Unsubscribe<InputActionData>(GameEventType.OnPrevWeapon, OnPrevWeaponTriggered);
    }


    /// <summary>
    ///切枪核心逻辑  
    ///</summary>
    public void SwitchWeapon(int index)
    {
        if (index < 0 || index >= weaponSlots.Count || index == currentWeaponIndex) return;

        if (currentWeaponIndex >= 0 && currentWeaponIndex < weaponSlots.Count)
        {
            weaponSlots[currentWeaponIndex].gameObject.SetActive(false);
        }

        currentWeaponIndex = index;
        GunBase newWeapon = weaponSlots[currentWeaponIndex];
        newWeapon.gameObject.SetActive(true);

        // 切换动画控制器
        AnimatorOverrideController newOverrider = newWeapon.GetWeaponOverrideController();
        if (newOverrider != null) playerAnimator.runtimeAnimatorController = newOverrider;

        // 修复：切枪时必须发送包含完整配件图标的 UIData，避免上一把枪的配件UI残留在屏幕上
        WeaponUIData switchData = newWeapon.CreateUIData();
        GameEventBus.GetInstance().Publish<WeaponUIData>(GameEventType.OnWeaponUIUpdate, switchData);

        // 更新 Controller 手里的枪
        if (playerController != null)
        {
            playerController.currentWeapon = newWeapon;
        }
    }

    // 接收到“下一把”事件的回调
    private void OnNextWeaponTriggered(InputActionData data)
    {
        if (isSwitching) return; // 如果正在切枪，锁住输入
        int nextIndex = (currentWeaponIndex + 1) % weaponSlots.Count;
        StartCoroutine(SwitchWeaponRoutine(nextIndex));
    }
    private void OnPrevWeaponTriggered(InputActionData data)
    {
        if (isSwitching) return; // 如果正在切枪，锁住输入
        int prevIndex = (currentWeaponIndex - 1 + weaponSlots.Count) % weaponSlots.Count;
        StartCoroutine(SwitchWeaponRoutine(prevIndex));
    }


    private IEnumerator SwitchWeaponRoutine(int targetIndex)
    {
        if (targetIndex == currentWeaponIndex || targetIndex >= weaponSlots.Count) yield break;

        isSwitching = true;

        //收枪
        playerAnimator.SetBool("Holstered", true);

        //等待动画播放完毕
        yield return new WaitForSeconds(0.5f); // 这里的时间需要根据实际动画长度调整

        //切枪
         SwitchWeapon(targetIndex);

        //拔枪
        playerAnimator.SetBool("Holstered", false);

        yield return new WaitForSeconds(0.5f); // 等待拔枪动画播放完毕
        isSwitching = false; // 解锁输入
    }
}
