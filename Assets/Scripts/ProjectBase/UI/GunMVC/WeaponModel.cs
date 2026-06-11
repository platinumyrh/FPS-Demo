using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 武器 UI 的 Model 数据层
/// </summary>
public class WeaponModel : BaseModel
{
    public string WeaponName { get; private set; }
    public int CurrentAmmo { get; private set; }
    public int MaxAmmoInClip { get; private set; }
    public int TotalAmmo { get; private set; }

    /// <summary>
    /// 设置/更新全量武器数据（常用于切枪、初始化）
    /// </summary>
    /// 
    public void SetWeaponData(string name, int current, int max, int total)
    {
        WeaponName = name;
        CurrentAmmo = current;
        MaxAmmoInClip = max;
        TotalAmmo = total;

        // 调用基类 BaseModel 的通知方法，这会触发 OnDataChanged
        NotifyDataChanged();
    }

    /// <summary>
    /// 只更新子弹数量（开火扣子弹、换弹完成时快速调用）
    /// </summary>
    public void UpdateAmmo(int current, int total)
    {
        CurrentAmmo = current;
        TotalAmmo = total;

        NotifyDataChanged();
    }


    /// <summary>
    /// 重置数据到初始状态
    /// </summary>
    public override void Reset()
    {
        base.Reset();
        WeaponName = "No Weapon";
        CurrentAmmo = 0;
        MaxAmmoInClip = 0;
        TotalAmmo = 0;
    }
}
