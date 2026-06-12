using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 武器状态 UI 专用的事件传递数据包
/// </summary>
public class WeaponUIData : GameEventData
{
    // ==== 1. 基础/高频数据 ====
    public string WeaponName { get; private set; }
    public int CurrentAmmo { get; private set; }
    public int MaxAmmoInClip { get; private set; }
    public int TotalAmmo { get; private set; }

    // ==== 2. 枪体及配件图片 (低频/动态数据) ====
    public Sprite WeaponBodySprite { get; private set; }
    public Sprite GripSprite { get; private set; }
    public Sprite MagazineSprite { get; private set; }
    public Sprite LaserSprite { get; private set; }
    public Sprite MuzzleSprite { get; private set; }
    public Sprite ScopeSprite { get; private set; }

    // 构造函数
    public WeaponUIData(string name, int current, int max, int total,
                        Sprite body = null, Sprite grip = null, Sprite mag = null,
                        Sprite laser = null, Sprite muzzle = null, Sprite scope = null)
    {
        WeaponName = name;
        CurrentAmmo = current;
        MaxAmmoInClip = max;
        TotalAmmo = total;

        WeaponBodySprite = body;
        GripSprite = grip;
        MagazineSprite = mag;
        LaserSprite = laser;
        MuzzleSprite = muzzle;
        ScopeSprite = scope;
    }
}
