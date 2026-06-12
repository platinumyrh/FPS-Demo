using System.Collections;
using System.Collections.Generic;
using UnityEditor;
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

    public Sprite WeaponBodySprite { get; private set; }
    public Sprite GripSprite { get; private set; }
    public Sprite MagzineSprite { get; private set; }
    public Sprite LazerSprite { get; private set; }
    public Sprite MuzzleSprite { get; private set; }
    public Sprite ScopeSprite { get; private set; }
    


    /// <summary>
    /// 设置/更新全量武器数据（常用于切枪、初始化）
    /// </summary>
    /// 
    public void SetWeaponData(WeaponUIData data)
    {
        this.WeaponName = data.WeaponName;
        this.CurrentAmmo = data.CurrentAmmo;
        this.MaxAmmoInClip = data.MaxAmmoInClip;
        this.TotalAmmo = data.TotalAmmo;

        // 核心：只有当传入的图片发生变化时（比如换枪、换配件），才更新 Model 里的图片
        if (data.WeaponBodySprite != null) this.WeaponBodySprite = data.WeaponBodySprite;
        if (data.GripSprite != null) this.GripSprite = data.GripSprite;
        if(data.MagazineSprite!=null) this.MagzineSprite = data.MagazineSprite;
        if (data.LaserSprite != null)  this.LazerSprite = data.LaserSprite;
        if(data.MuzzleSprite != null) this.MuzzleSprite = data.MuzzleSprite;
        if (data.ScopeSprite != null) this.ScopeSprite = data.ScopeSprite;



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
