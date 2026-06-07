using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;


/// <summary>
/// 场景切换模块
/// </summary>
public class GameScenesManager :BaseManager<GameScenesManager>
{
    public void LoadScene(string sceneName,UnityAction action)
    {
        //同步加载
        SceneManager.LoadScene(sceneName);
        action?.Invoke();
    }
    /// <summary>
    /// 提供外部调用的异步加载场景的方法
    /// </summary>
    /// <param name="sceneName"></param>
    /// <param name="action"></param>
    public void LoadSceneAsync(string sceneName,UnityAction action)
    {
        //异步加载
        MonoManager.GetInstance().StartCoroutine(LoadSceneAsyncCoroutine(sceneName,action));

    }
    private IEnumerator LoadSceneAsyncCoroutine(string name, UnityAction action)
    {
        var ao = SceneManager.LoadSceneAsync(name);
        while (!ao.isDone)
        {
            GameEventBus.GetInstance().Publish(GameEventType.TestEvent);//发布 进度加载事件
            //可以在这里添加加载进度的显示
            yield return ao.progress;
        }

        yield return ao;
        action?.Invoke();
    }
}
