using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

/// <summary>
/// 把场景中的带通道的贴图给拆分出来
/// </summary>
public class ProcessSceneAlphaTexture : ScriptableObject
{
    private static Dictionary<string, string> ChangeShaderMap = new Dictionary<string, string>();

    private static List<string> ProcessModelPath = new List<string>(); 

    private static List<string> FilterShaderName = new List<string>();
    private static List<string> FilterFbxPath = new List<string>(); 

    private static void Init()
    {
        ChangeShaderMap.Clear();
        ChangeShaderMap["Transparent/Cutout/Diffuse"] = "Dodjoy/Transparent/MaskCutDiffuse";
        ChangeShaderMap["Unlit/Transparent"] = "Dodjoy/Transparent/Unlit";

        ProcessModelPath.Clear();

        FilterShaderName.Add("BRDF");
        FilterShaderName.Add("dod_hero_light");
        FilterShaderName.Add("falloff");
        FilterShaderName.Add("Character");

        FilterFbxPath.Add("GameModel");
    }

    [MenuItem("DodTools/场景工具/场景优化")]
    public static void DoIt()
    {
        Init();
        ProcessAlphaTexture();
        ProcessModelNormal();

        EditorUtility.DisplayDialog("确定", "优化完成", "OK", "");
    }

    static void ProcessAlphaTexture()
    {
        //遍历所有的材质
        Renderer[] renderers = (Renderer[])EditorWindow.FindObjectsOfType(typeof(Renderer));
        foreach (Renderer renderer in renderers)
        {
            foreach (Material material in renderer.sharedMaterials)
            {
                if (material == null || material.shader == null)
                {
                    Debug.LogError("no material ", renderer.gameObject);
                    continue;
                }
                string newShaderName = GetChangeShaderName(material.shader.name);
                if (string.IsNullOrEmpty(newShaderName))
                {
                    continue;
                }

                Texture2D mainTexture = material.mainTexture as Texture2D;
                if (mainTexture == null)
                {
                    EditorUtility.DisplayDialog("Error", "material no texture", "Close");
                    continue;
                }

                //提取贴图的alpha
                string mainTexturePath = AssetDatabase.GetAssetPath(mainTexture);
                if (string.IsNullOrEmpty(mainTexturePath))
                {
                    EditorUtility.DisplayDialog("Error", "can not find texture in project", "Close");
                    continue;
                }

                TextureImporter mainTextureImporter = AssetImporter.GetAtPath(mainTexturePath) as TextureImporter;
                int oldSize = mainTextureImporter.maxTextureSize;

                mainTextureImporter.isReadable = true;
                mainTextureImporter.SetPlatformTextureSettings("iPhone", 2048, TextureImporterFormat.RGBA32, 100, false);
                mainTextureImporter.SetPlatformTextureSettings("Android", 2048, TextureImporterFormat.RGBA32, 100, false);

                AssetDatabase.ImportAsset(mainTexturePath, ImportAssetOptions.ForceUpdate);
                mainTexture = AssetDatabase.LoadMainAssetAtPath(mainTexturePath) as Texture2D;

                string fileName = Path.GetFileNameWithoutExtension(mainTexturePath);
                string directoryName = Path.GetDirectoryName(mainTexturePath);

                string maskTexturePath = Path.Combine(directoryName, string.Format("{0}_alpha.png", fileName));

                Color[] mainTextureColors = mainTexture.GetPixels();

                Texture2D maskTexture = new Texture2D(mainTexture.width, mainTexture.height, TextureFormat.RGB24,
                    false);
                Color[] maskTextureColors = maskTexture.GetPixels();

                for (int i = 0; i < maskTextureColors.Length; ++i)
                {
                    maskTextureColors[i].r =
                        maskTextureColors[i].g = maskTextureColors[i].b = mainTextureColors[i].a;
                }

                maskTexture.SetPixels(maskTextureColors);
                maskTexture.Apply();

                // 保存alpha到png
                using (FileStream stream = new FileStream(maskTexturePath, FileMode.OpenOrCreate, FileAccess.Write))
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(maskTexture.EncodeToPNG());
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                //修改贴图的导入选项
                mainTextureImporter = AssetImporter.GetAtPath(mainTexturePath) as TextureImporter;
                mainTextureImporter.textureType = TextureImporterType.Default;
                mainTextureImporter.isReadable = false;
                mainTextureImporter.anisoLevel = 0;
                mainTextureImporter.npotScale = TextureImporterNPOTScale.ToNearest;
                mainTextureImporter.wrapMode = TextureWrapMode.Clamp;
                mainTextureImporter.filterMode = FilterMode.Bilinear;
                mainTextureImporter.mipmapEnabled = true;
                mainTextureImporter.maxTextureSize = oldSize;
                mainTextureImporter.SetPlatformTextureSettings("iPhone", oldSize, TextureImporterFormat.PVRTC_RGB4, 100, false);
                mainTextureImporter.SetPlatformTextureSettings("Android", oldSize, TextureImporterFormat.ETC_RGB4, 100, false);
                AssetDatabase.ImportAsset(mainTexturePath, ImportAssetOptions.ForceUpdate);

                TextureImporter maskTextureImporter = AssetImporter.GetAtPath(maskTexturePath) as TextureImporter;
                maskTextureImporter.textureType = TextureImporterType.Default;
                maskTextureImporter.isReadable = false;
                maskTextureImporter.anisoLevel = 0;
                maskTextureImporter.npotScale = TextureImporterNPOTScale.ToNearest;
                maskTextureImporter.wrapMode = TextureWrapMode.Clamp;
                maskTextureImporter.filterMode = FilterMode.Bilinear;
                maskTextureImporter.mipmapEnabled = true;
                maskTextureImporter.maxTextureSize = oldSize;
                maskTextureImporter.SetPlatformTextureSettings("iPhone", oldSize, TextureImporterFormat.PVRTC_RGB4, 100, false);
                maskTextureImporter.SetPlatformTextureSettings("Android", oldSize, TextureImporterFormat.ETC_RGB4, 100, false);
                AssetDatabase.ImportAsset(maskTexturePath, ImportAssetOptions.ForceUpdate);

                Shader newShader = Shader.Find(newShaderName);
                if (newShader != null)
                {
                    material.shader = newShader;
                }
                else
                {
                    Debug.LogError("can not find new shader:" + newShaderName);
                }

                maskTexture = AssetDatabase.LoadMainAssetAtPath(maskTexturePath) as Texture2D;
                if (maskTexture != null)
                {
                    material.SetTexture("_MaskTex", maskTexture);
                }
                AssetDatabase.SaveAssets();
            }
        }
    }

    /// <summary>
    /// 去除模型的normal数据
    /// </summary>
    static void ProcessModelNormal()
    {
        MeshFilter[] meshes = (MeshFilter[]) EditorWindow.FindObjectsOfType(typeof (MeshFilter));
        foreach (MeshFilter mesh in meshes)
        {
            bool needFilter = false;

            Renderer[] renderers = mesh.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (NeedFilterShaderName(material.shader.name))
                    {
                        needFilter = true;
                        break;
                    }
                }
                if (needFilter)
                {
                    break;
                }
            }
            if (!needFilter)
            {
                string assetPath = AssetDatabase.GetAssetPath(mesh.sharedMesh);
                if (!ProcessModelPath.Contains(assetPath))
                {
                    ModelImporter modelImporter = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                    if (modelImporter != null && modelImporter.normalImportMode != ModelImporterTangentSpaceMode.None)
                    {
                        modelImporter.normalImportMode = ModelImporterTangentSpaceMode.None;
                        modelImporter.tangentImportMode = ModelImporterTangentSpaceMode.None;
                        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                        ProcessModelPath.Add(assetPath);
                    }
                }
            }
        }

        AssetDatabase.SaveAssets();
    }

    private static bool NeedFilterShaderName(string name)
    {
        foreach (var filter in FilterShaderName)
        {
            if (name.ToLower().Contains(filter))
            {
                return true;
            }
        }
        return false;
    }

    private static bool NeedFilterAssetPath(string assetPath)
    {
        foreach (var filterPath in FilterFbxPath)
        {
            if (assetPath.Contains(filterPath))
            {
                return true;
            }
        }

        return false;
    }

    private static string GetChangeShaderName(string preShaderName)
    {
        if (!ChangeShaderMap.ContainsKey(preShaderName))
        {
            return "";
        }
        return ChangeShaderMap[preShaderName];
    }


    [MenuItem("DodTools/特效工具/去除特效应用模型的Normal")]
    private static void ProcessEffectModelNormal()
    {
        List<string> processed = new List<string>();
        Process("Assets/Resources/Effect", processed);
    }

    private static void Process(string directory, List<string> processed)
    {
        string[] paths = Directory.GetFiles(directory);
        for (int i = 0; i < paths.Length; ++i)
        {
            if (paths[i].EndsWith("prefab", System.StringComparison.OrdinalIgnoreCase))
            {
                string[] dependencies = AssetDatabase.GetDependencies(new[] {paths[i]});

                bool filter = false;

                foreach (var s in dependencies)
                {
                    if (s.ToLower().EndsWith("shader"))
                    {
                        Shader shader = (Shader) AssetDatabase.LoadMainAssetAtPath(s);
                        if (NeedFilterShaderName(shader.name))
                        {
                            filter = true;
                            break;
                        }
                    }
                }
                if (!filter)
                {
                    foreach (var s in dependencies)
                    {
                        if (!NeedFilterAssetPath(s) && s.ToLower().EndsWith("fbx"))
                        {
                            if (!processed.Contains(s))
                            {
                                ModelImporter modelImporter = AssetImporter.GetAtPath(s) as ModelImporter;
                                if (modelImporter != null &&
                                    modelImporter.normalImportMode != ModelImporterTangentSpaceMode.None)
                                {
                                    modelImporter.normalImportMode = ModelImporterTangentSpaceMode.None;
                                    modelImporter.tangentImportMode = ModelImporterTangentSpaceMode.None;
                                    AssetDatabase.ImportAsset(s, ImportAssetOptions.ForceUpdate);
                                    ProcessModelPath.Add(s);
                                }
                                processed.Add(s);
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogError("skip error" + paths[i]);
                }
            }
        }

        paths = Directory.GetDirectories(directory);
        for (int i = 0; i < paths.Length; ++i)
        {
            if (paths[i].Contains(".svn"))
            {
                continue;
            }

            Process(paths[i], processed);
        }
    }
}