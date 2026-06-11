using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 武器状态 UI 专用的事件传递数据包
/// </summary>
public class WeaponUIData : GameEventData
{
    public string WeaponName { get; private set; }
    public int CurrentAmmo { get; private set; }
    public int MaxAmmoInClip { get; private set; }
    public int TotalAmmo { get; private set; }

    public WeaponUIData(string name, int current, int max, int total)
    {
        WeaponName = name;
        CurrentAmmo = current;
        MaxAmmoInClip = max;
        TotalAmmo = total;
    }
}
