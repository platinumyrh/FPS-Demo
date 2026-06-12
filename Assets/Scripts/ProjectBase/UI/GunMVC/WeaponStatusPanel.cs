using System.Collections;
using System.Collections.Generic;
using System.Net.Mail;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// 武器状态 UI 的 View 视图层
/// </summary>
public class WeaponStatusPanel :BasePanel
{
    private TextMeshProUGUI txtWeaponName;
    private TextMeshProUGUI txtCurrentAmmo;
    private TextMeshProUGUI txtTotalAmmo;

    private Image weaponBody;
    private Image weaponAttachmentGrip;
    private Image weaponAttachmentMagzine;
    private Image weaponAttachmentLaser;
    private Image weaponAttachmentMuzzle;
    //private Image weaponAttachmentScope; 暂时用不到
    private Image weaponAttachmentScopDefault; 


    // 因为基类的 FindChildrenControl 是在 Awake 中执行的
    // 我们在 Start 里获取引用，确保 Awake 的搜集字典已经彻底就绪
    private void Start()
    {
        txtWeaponName = GetControl<TextMeshProUGUI>("");
        txtCurrentAmmo = GetControl<TextMeshProUGUI>("Text Ammunition Current");
        txtTotalAmmo = GetControl<TextMeshProUGUI>("Text Ammunition Total");

        weaponBody = GetControl<Image>("Image Weapon Body");
        weaponAttachmentGrip = GetControl<Image>("Image Weapon Attachment Grip");
        weaponAttachmentMagzine = GetControl<Image>("Image Weapon Attachment Magazine");
        weaponAttachmentLaser = GetControl<Image>("Image Weapon Attachment Laser");
        weaponAttachmentMuzzle = GetControl<Image>("Image Weapon Attachment Muzzle");
        
        weaponAttachmentScopDefault = GetControl<Image>("Image Weapon Attachment Scope Default");
    }


    /// <summary>
    /// 提供给 Controller 调用的纯粹刷新方法。
    /// 遵循 MVC 约束：View 不做复杂逻辑，只负责具体的组件展现。
    /// </summary>
    public void UpdateDisplay(string name, int current, int max, int total,Sprite body,Sprite grip,Sprite magzine,Sprite lazer,Sprite muzzle,Sprite scope)
    {
        if (txtWeaponName != null) txtWeaponName.text = name;
        if (txtCurrentAmmo != null) txtCurrentAmmo.text = current.ToString();
        if (txtTotalAmmo != null) txtTotalAmmo.text = total.ToString();

        if (weaponBody.sprite != body)
        {
            weaponBody.sprite = body;
            weaponBody.gameObject.SetActive(body != null);
        }
        if (weaponAttachmentGrip.sprite != grip)
        {
            weaponAttachmentGrip.sprite = grip;
            weaponAttachmentGrip.gameObject.SetActive(grip != null);
        }
        if (weaponAttachmentMagzine.sprite != magzine)
        {
            weaponAttachmentMagzine.sprite = magzine;
            weaponAttachmentMagzine.gameObject.SetActive(magzine != null);
        }
        if (weaponAttachmentLaser.sprite != lazer)
        {
            weaponAttachmentLaser.sprite = lazer;
            weaponAttachmentLaser.gameObject.SetActive(lazer != null);
        }
        if (weaponAttachmentMuzzle.sprite != muzzle)
        {
            weaponAttachmentMuzzle.sprite = muzzle;
            weaponAttachmentMuzzle.gameObject.SetActive(muzzle != null);
        }
        if (weaponAttachmentScopDefault.sprite != scope)
        { 
            weaponAttachmentScopDefault.sprite = scope;
            weaponAttachmentScopDefault.gameObject.SetActive(scope != null);
        }
       

        // 可以在 View 层编写纯视觉的表现逻辑（例如：低弹药文本变红提示）
        if (txtCurrentAmmo != null)
        {
            txtCurrentAmmo.color = (current <= max * 0.2f) ? Color.red : Color.white;
        }
    }
}
