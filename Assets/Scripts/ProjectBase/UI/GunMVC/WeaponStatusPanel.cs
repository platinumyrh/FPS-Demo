using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


/// <summary>
/// 武器状态 UI 的 View 视图层
/// </summary>
public class WeaponStatusPanel :BasePanel
{
    private TextMeshProUGUI txtWeaponName;
    private TextMeshProUGUI txtCurrentAmmo;
    private TextMeshProUGUI txtTotalAmmo;

    // 因为基类的 FindChildrenControl 是在 Awake 中执行的
    // 我们在 Start 里获取引用，确保 Awake 的搜集字典已经彻底就绪
    private void Start()
    {
        txtWeaponName = GetControl<TextMeshProUGUI>("");
        txtCurrentAmmo = GetControl<TextMeshProUGUI>("Text Ammunition Current");
        txtTotalAmmo = GetControl<TextMeshProUGUI>("Text Ammunition Total");
    }


    /// <summary>
    /// 提供给 Controller 调用的纯粹刷新方法。
    /// 遵循 MVC 约束：View 不做复杂逻辑，只负责具体的组件展现。
    /// </summary>
    public void UpdateDisplay(string name, int current, int max, int total)
    {
        if (txtWeaponName != null) txtWeaponName.text = name;
        if (txtCurrentAmmo != null) txtCurrentAmmo.text = current.ToString();
        if (txtTotalAmmo != null) txtTotalAmmo.text = total.ToString();

        else
        {
            Debug.LogWarning($"[WeaponStatusPanel] 某个文本组件未找到，无法更新显示！");
        }

        // 可以在 View 层编写纯视觉的表现逻辑（例如：低弹药文本变红提示）
        if (txtCurrentAmmo != null)
        {
            txtCurrentAmmo.color = (current <= max * 0.2f) ? Color.red : Color.white;
        }
    }
}
