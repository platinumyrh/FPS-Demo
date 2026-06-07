using UnityEngine;
using UnityEditor;

public class MissingScriptsCleaner : EditorWindow
{
    [MenuItem("Tools/Clean Missing Scripts in Selection")]
    private static void CleanInSelection()
    {
        // 1. 获取选中的所有物体
        GameObject[] rootObjects = Selection.gameObjects;
        if (rootObjects == null || rootObjects.Length == 0)
        {
            Debug.LogWarning("[清理取消] 你没有选中任何游戏物体！请先在层级或项目面板选中物体。");
            return;
        }

        int compCount = 0;
        int goCount = 0;

        // 2. 遍历每一个选中的“根”物体
        foreach (GameObject rootGo in rootObjects)
        {
            // true 表示包含隐藏的/未激活的子物体
            Transform[] allTransforms = rootGo.GetComponentsInChildren<Transform>(true);

            foreach (Transform t in allTransforms)
            {
                GameObject currentGo = t.gameObject;

                // 执行官方清理方法
                int count = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(currentGo);
                if (count > 0)
                {
                    compCount += count;
                    goCount++;
                    // 标记脏数据，确保 Unity 能够保存修改（如果是场景物体会标记场景，如果是预制体会标记预制体）
                    EditorUtility.SetDirty(currentGo);
                }
            }
        }

        // 3. 如果修改的是场景中的物体，强制刷新场景以防没有保存
        if (goCount > 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        }

        Debug.Log($"[✨ 深度清理完成] 一共扫描了选中物体及其所有子代，从 {goCount} 个游戏物体中移除了 {compCount} 个丢失的脚本组件！");
    }
}