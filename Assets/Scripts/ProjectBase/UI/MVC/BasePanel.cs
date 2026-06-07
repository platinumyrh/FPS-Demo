using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
/// <summary>
/// 面板基类
/// 找到所有自己面板下的控件对象
/// 提供显示 隐藏的行为
/// </summary>
public class BasePanel : MonoBehaviour
{
    //通过里氏代换存储所有的UI组件
    private Dictionary<string,List<UIBehaviour>> controlDic = new Dictionary<string, List<UIBehaviour>>();

    // Start is called before the first frame update
    void Awake()
    {
        FindChildrenControl<Button>();
        FindChildrenControl<Image>();
        FindChildrenControl<Text>();
        FindChildrenControl<Scrollbar>();
        //后续添加
       

      

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //将所有的UI组件存储到字典中的方法
    private void FindChildrenControl<T>()
        where T : UIBehaviour
    {
        T[] controls = this.GetComponentsInChildren<T>();
        foreach (T control in controls)
        {
            if (controlDic.ContainsKey(control.name))
            {
                controlDic[control.name].Add(control);
            }
            else
            {
                controlDic.Add(control.name, new List<UIBehaviour>() { control });
            }
        }
    }

    protected T GetControl<T>(string controlName)
        where T : UIBehaviour
    {
        if (controlDic.ContainsKey(controlName))
        {
            foreach (T control in controlDic[controlName])
            {
                if (control is T)  
                    return control as T;
            }
        }
        return null;
    }


    public virtual void Show()
    {
        gameObject.SetActive(true);
    }
    public virtual void Hide()
    {
        gameObject.SetActive(false);
    }
}
