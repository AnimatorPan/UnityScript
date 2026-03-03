using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

public class ExtractFBXAnimations : MonoBehaviour
{
    [MenuItem("Assets/提取FBX动画切片", true)]
    private static bool ValidateExtractAnimations()
    {
        // 检查是否选择了FBX文件或文件夹
        foreach (Object obj in Selection.objects)
        {
            string assetPath = AssetDatabase.GetAssetPath(obj);
            // 如果是FBX文件，返回true
            if (Path.GetExtension(assetPath).ToLower() == ".fbx")
            {
                return true;
            }
            // 如果是文件夹，检查是否包含FBX文件
            if (Directory.Exists(assetPath))
            {
                if (Directory.GetFiles(assetPath, "*.fbx", SearchOption.AllDirectories).Length > 0)
                {
                    return true;
                }
            }
        }
        return false;
    }

    [MenuItem("Assets/提取FBX动画切片")]
    public static void ExtractAnimations()
    {
        foreach (Object obj in Selection.objects)
        {
            string assetPath = AssetDatabase.GetAssetPath(obj);
            // 如果是FBX文件
            if (Path.GetExtension(assetPath).ToLower() == ".fbx")
            {
                ExtractAnimationClips(assetPath);
            }
            // 如果是文件夹
            else if (Directory.Exists(assetPath))
            {
                ProcessDirectory(assetPath);
            }
        }
    }

    // 处理文件夹中的所有FBX文件
    private static void ProcessDirectory(string directoryPath)
    {
        // 获取文件夹中所有的FBX文件（包括子文件夹）
        string[] fbxFiles = Directory.GetFiles(directoryPath, "*.fbx", SearchOption.AllDirectories);
        
        foreach (string fbxFile in fbxFiles)
        {
            // 将系统路径转换为Unity资源路径
            string assetPath = fbxFile.Replace(Application.dataPath, "Assets").Replace('\\', '/');
            ExtractAnimationClips(assetPath);
        }
    }

    private static void ExtractAnimationClips(string fbxPath)
    {
        // 获取当前文件夹路径
        string folderPath = Path.GetDirectoryName(fbxPath);

        // 加载FBX文件中的所有资源
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        foreach (Object asset in assets)
        {
            if (asset is AnimationClip)
            {
                AnimationClip clip = asset as AnimationClip;

                // 跳过带有前缀名为__preview__的动画切片
                if (clip.name.StartsWith("__preview__"))
                {
                    continue;
                }

                string clipPath = Path.Combine(folderPath, $"{clip.name}.anim");

                // 检查是否已经存在同名的动画切片
                if (File.Exists(clipPath))
                {
                    // 替换现有的动画切片
                    AnimationClip existingClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                    EditorUtility.CopySerialized(clip, existingClip);
                }
                else
                {
                    // 创建新的动画切片
                    AnimationClip newClip = new AnimationClip();
                    EditorUtility.CopySerialized(clip, newClip);
                    AssetDatabase.CreateAsset(newClip, clipPath);
                }
            }
        }

        // 刷新资产数据库
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}