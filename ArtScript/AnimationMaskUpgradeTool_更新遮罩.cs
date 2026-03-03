using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System;
using System.Reflection;

public class AnimationMaskUpgradeTool
{
    [MenuItem("Assets/批量更新动画遮罩节点", false, 103)]
    public static void UpdateAnimationMaskForSelectedFBX()
    {
        UnityEngine.Object[] selectedObjects = Selection.objects;
        List<string> fbxPaths = new List<string>();

        foreach (UnityEngine.Object obj in selectedObjects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (Path.GetExtension(path).Equals(".fbx", StringComparison.OrdinalIgnoreCase))
            {
                fbxPaths.Add(path);
            }
        }

        if (fbxPaths.Count == 0)
        {
            EditorUtility.DisplayDialog("提示", "请先选择一个或多个FBX文件", "确定");
            return;
        }

        foreach (string fbxPath in fbxPaths)
        {
            ProcessFBXFile(fbxPath);
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("完成", $"已成功处理 {fbxPaths.Count} 个FBX文件的动画遮罩", "确定");
    }

    private static void ProcessFBXFile(string fbxPath)
    {
        ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer == null)
        {
            Debug.LogError($"无法获取 {fbxPath} 的ModelImporter");
            return;
        }

        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (modelAsset == null)
        {
            Debug.LogError($"无法加载 {fbxPath} 模型");
            return;
        }

        // 获取所有变换路径
        List<string> transformPaths = new List<string>();
        GetAllTransformPaths(modelAsset.transform, "", transformPaths);
        
        // 调试输出前10个路径
        Debug.Log($"收集到的路径数量: {transformPaths.Count}");
        for (int i = 0; i < Mathf.Min(10, transformPaths.Count); i++)
        {
            Debug.Log($"  [{i}] {transformPaths[i]}");
        }

        // 使用SerializedObject修改
        SerializedObject serializedImporter = new SerializedObject(importer);
        SerializedProperty clipAnimationsProp = serializedImporter.FindProperty("m_ClipAnimations");

        if (clipAnimationsProp == null || clipAnimationsProp.arraySize == 0)
        {
            Debug.LogWarning($"{fbxPath} 没有动画剪辑");
            return;
        }

        bool hasChanges = false;

        for (int i = 0; i < clipAnimationsProp.arraySize; i++)
        {
            SerializedProperty clipProp = clipAnimationsProp.GetArrayElementAtIndex(i);
            
            // 获取当前动画剪辑的名称
            SerializedProperty nameProp = clipProp.FindPropertyRelative("name");
            string clipName = nameProp != null ? nameProp.stringValue : $"Clip_{i}";
            Debug.Log($"处理动画剪辑: {clipName}");

            // 设置 maskType (CreateFromThisModel)
            // Unity 2022+ 枚举值需要通过试验确定
            SerializedProperty maskTypeProp = clipProp.FindPropertyRelative("maskType");
            if (maskTypeProp != null)
            {
                int oldValue = maskTypeProp.intValue;
                // 尝试值 2，因为 0=None, 1=CopyFromOther, 3=无
                int targetValue = 2;
                if (oldValue != targetValue)
                {
                    maskTypeProp.intValue = targetValue;
                    hasChanges = true;
                    Debug.Log($"  设置 maskType: {oldValue} -> {targetValue}");
                }
            }

            // 清空 maskSource
            SerializedProperty maskSourceProp = clipProp.FindPropertyRelative("maskSource");
            if (maskSourceProp != null && maskSourceProp.objectReferenceValue != null)
            {
                maskSourceProp.objectReferenceValue = null;
                hasChanges = true;
                Debug.Log($"  清空 maskSource");
            }

            // 填充 transformMask
            SerializedProperty transformMaskProp = clipProp.FindPropertyRelative("transformMask");
            if (transformMaskProp != null && transformMaskProp.isArray)
            {
                // 强制重新填充，确保路径正确
                FillTransformMask(transformMaskProp, transformPaths);
                hasChanges = true;
                Debug.Log($"  填充 transformMask: {transformPaths.Count} 个节点");
            }
        }

        if (hasChanges)
        {
            serializedImporter.ApplyModifiedProperties();
            EditorUtility.SetDirty(importer);
            AssetDatabase.ImportAsset(fbxPath, ImportAssetOptions.ForceUpdate);
            Debug.Log($"已更新 {fbxPath}");
            
            // 自动触发"更新遮罩"操作
            UpdateMaskFromModel(fbxPath);
        }
        else
        {
            Debug.Log($"{fbxPath} 无需更新");
        }
    }

    private static void UpdateMaskFromModel(string fbxPath)
    {
        try
        {
            // 通过反射调用Unity内部的更新遮罩方法
            // 方法路径: UnityEditor.ModelImporterClipEditor.UpdateMaskFromModel
            var assembly = Assembly.GetAssembly(typeof(ModelImporter));
            var clipEditorType = assembly.GetType("UnityEditor.ModelImporterClipEditor");
            
            if (clipEditorType != null)
            {
                var updateMethod = clipEditorType.GetMethod("UpdateMaskFromModel", 
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                
                if (updateMethod != null)
                {
                    updateMethod.Invoke(null, new object[] { fbxPath });
                    Debug.Log($"  自动触发更新遮罩成功");
                    return;
                }
            }
            
            // 备选方案: 重新导入资源
            Debug.Log($"  使用备选方案重新导入资源");
            AssetDatabase.ImportAsset(fbxPath, ImportAssetOptions.ForceSynchronousImport);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"  自动触发更新遮罩失败: {e.Message}");
        }
    }

    private static void FillTransformMask(SerializedProperty transformMaskProp, List<string> paths)
    {
        transformMaskProp.arraySize = paths.Count;

        for (int i = 0; i < paths.Count; i++)
        {
            SerializedProperty entryProp = transformMaskProp.GetArrayElementAtIndex(i);
            
            // Unity内部使用 m_Path 和 m_Weight
            SerializedProperty pathProp = entryProp.FindPropertyRelative("m_Path");
            SerializedProperty weightProp = entryProp.FindPropertyRelative("m_Weight");

            if (pathProp != null)
                pathProp.stringValue = paths[i];
            
            if (weightProp != null)
                weightProp.floatValue = 1f;
        }
    }

    private static void GetAllTransformPaths(Transform transform, string parentPath, List<string> paths, bool isRoot = true)
    {
        // 跳过根节点（FBX文件名）
        if (isRoot)
        {
            // 添加一个空路径作为第一个元素（对应根节点）
            paths.Add("");
            
            // 从根节点的子节点开始收集
            for (int i = 0; i < transform.childCount; i++)
            {
                CollectTransformPathsRecursive(transform.GetChild(i), "", paths);
            }
        }
    }

    private static void CollectTransformPathsRecursive(Transform transform, string parentPath, List<string> paths)
    {
        // Unity的transformMask使用层级路径格式
        // 第一个元素是空路径（对应根节点）
        // 然后是: Bip001, Bip001/Bip001 Pelvis, Bip001/Bip001 Pelvis/Bip001 L Thigh 等
        string currentPath = string.IsNullOrEmpty(parentPath) ? transform.name : parentPath + "/" + transform.name;
        paths.Add(currentPath);

        for (int i = 0; i < transform.childCount; i++)
        {
            CollectTransformPathsRecursive(transform.GetChild(i), currentPath, paths);
        }
    }
}
