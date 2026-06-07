using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 资源加载模块
/// </summary>
public class ResManager : BaseManager<ResManager>
{
    //同步加载资源
    public T Load<T>(string name) where T : Object
    {
        T res = Resources.Load<T>(name);
        //if (res is GameObject)
        //{
        //    //如果加载的资源是游戏对象，则直接实例化它
        //    res = GameObject.Instantiate(res);
        //    Debug.Log($"同步加载资源: {name}");           这里感觉没有太大必要
        //}
        return res;
    }

    /// 异步加载资源
    public void LoadAsync<T>(string name,UnityAction<T> callBack) where T : Object
    {
        MonoManager.GetInstance().StartCoroutine(LoadAsyncCoroutine(name,callBack));
    }

    private IEnumerator LoadAsyncCoroutine<T>(string name, UnityAction<T> callBack) where T : Object
    {
        var request = Resources.LoadAsync(name);
        yield return request;
        
        callBack?.Invoke(request.asset as T);

         
    }
}
