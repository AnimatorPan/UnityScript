using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using Spine.Unity;
using System.IO;

public class CreateSkeletonGraphic : MonoBehaviour
{
    [MenuItem("Assets/生成SkeletonGraphic (UI)", true)]
    private static bool ValidateCreateSkeletonGraphic()
    {
        // 确保至少选择了一个_SkeletonData文件
        foreach (Object obj in Selection.objects)
        {
            if (obj is SkeletonDataAsset)
            {
                return true;
            }
        }
        return false;
    }

    [MenuItem("Assets/生成SkeletonGraphic (UI)")]
    private static void CreateSkeletonGraphicFromSelected()
    {
        foreach (Object obj in Selection.objects)
        {
            if (obj is SkeletonDataAsset skeletonDataAsset)
            {
                string assetPath = AssetDatabase.GetAssetPath(skeletonDataAsset);
                string folderPath = Path.GetDirectoryName(assetPath);
                CreateSkeletonGraphicObject(skeletonDataAsset, folderPath);
            }
        }
    }

    private static void CreateSkeletonGraphicObject(SkeletonDataAsset skeletonDataAsset, string folderPath)
    {
        string folderName = new DirectoryInfo(folderPath).Name;

        // 创建一个新的GameObject
        GameObject skeletonGraphicGO = new GameObject($"{folderName}Show");
        skeletonGraphicGO.layer = LayerMask.NameToLayer("UI"); // 设置Layer为UI
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }
        skeletonGraphicGO.transform.SetParent(canvas.transform, false);

        // 添加SkeletonGraphic组件
        SkeletonGraphic skeletonGraphic = skeletonGraphicGO.AddComponent<SkeletonGraphic>();
        skeletonGraphic.skeletonDataAsset = skeletonDataAsset;
        skeletonGraphic.Initialize(true);

        // 设置起始动画为idle
        var skeletonData = skeletonGraphic.skeletonDataAsset.GetSkeletonData(true);
        if (skeletonGraphic.AnimationState != null && skeletonData.FindAnimation("idle") != null)
        {
            skeletonGraphic.AnimationState.SetAnimation(0, "idle", true);
        }

        // 查找文件夹内的材质球并设置Shader为spine/skeletongraphic
        string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { folderPath });
        foreach (string guid in materialGuids)
        {
            string materialPath = AssetDatabase.GUIDToAssetPath(guid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material != null)
            {
                material.shader = Shader.Find("Spine/SkeletonGraphic");
                skeletonGraphic.material = material;
                break; // 只使用找到的第一个材质球
            }
        }

        // 将生成的SkeletonGraphic保存到当前文件夹
        string newPath = Path.Combine(folderPath, $"{folderName}Show.prefab");
        PrefabUtility.SaveAsPrefabAsset(skeletonGraphicGO, newPath);

        // 删除场景中的临时GameObject
        DestroyImmediate(skeletonGraphicGO);

        // 刷新资产数据库
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}