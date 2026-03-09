using System.Collections.Generic;
using System.IO;
using UnityEditor;
using System;
using System.Linq;
using Unity.EditorCoroutines.Editor;
using UnityEngine;
using DodGame;
public class ConvertGPUSkinMesh : ScriptableObject
{
    public static long GetLocalDateTimeUtc()
    {
        DateTime dtUtcStartTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
        DateTime nowTime = DateTime.UtcNow;
        long time = nowTime.Ticks - dtUtcStartTime.Ticks;
        time = time / 10;
        long second = time / 1000000;
        return second;
    }
    
    public static void SaveAsset(UnityEngine.Object obj, string assetPath)
    {
        var dir = System.IO.Path.GetDirectoryName(assetPath);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        
        // 如果已经存在了，直接返回，不重复创建
        var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
        if (asset != null)
        {
            Debug.Log($"资源已存在，跳过保存: {assetPath}");
            return;
        }
        
        AssetDatabase.CreateAsset(obj, assetPath);
        Debug.Log($"资源创建成功: {assetPath}");
    }

    public static void ConvertGpuSkinRender(GameObject go)
    {
            //go.name = o.name + "_gpu";
            var renderers = go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var startTime = GetLocalDateTimeUtc();
            //EditorCoroutineUtility.StartCoroutine()
            
            {
                List<Transform> bonesList = new List<Transform>();
                List<Matrix4x4> bindPoseList = new List<Matrix4x4>();

                foreach (var renderer in renderers)
                {
                    Transform[] bones = renderer.bones;
                    Mesh mesh = renderer.sharedMesh;
                    BoneWeight[] boneWeights = mesh.boneWeights;
                    Matrix4x4[] bindposes = mesh.bindposes;
                    foreach (BoneWeight boneWeight in boneWeights)
                    {
                        var bone = bones[boneWeight.boneIndex0];
                        if (bone.gameObject.name.EndsWith("Nub"))
                        {
                            Debug.LogError("Invalid bone used: " + bone.gameObject.name);
                        }

                        if (!bonesList.Contains(bones[boneWeight.boneIndex0]))
                        {
                            bonesList.Add(bones[boneWeight.boneIndex0]);
                            bindPoseList.Add(bindposes[boneWeight.boneIndex0]);
                        }

                        if (!bonesList.Contains(bones[boneWeight.boneIndex1]))
                        {
                            bonesList.Add(bones[boneWeight.boneIndex1]);
                            bindPoseList.Add(bindposes[boneWeight.boneIndex1]);
                        }
                    }
                }

                if (bonesList.Count <= GPUSkinRenderer.MaxBoneNum)
                {
                    //修改材质
                    List<MeshRenderer> renders = new List<MeshRenderer>();
                    GPUSkinRenderer gpuSkinRenderer = go.AddComponent<GPUSkinRenderer>();
                    gpuSkinRenderer.bones = bonesList.ToArray();
                    gpuSkinRenderer.bindPose = bindPoseList.ToArray();

                    int canBatchNum = 0;

                    Debug.Log($"ConvertGpuSkinRender: 找到 {renderers.Length} 个 SkinnedMeshRenderer");
                    
                    foreach (var skinnedMeshRenderer in renderers)
                    {
                        Debug.Log($"处理 SkinnedMeshRenderer: {skinnedMeshRenderer.gameObject.name}, 材质: {(skinnedMeshRenderer.sharedMaterial != null ? skinnedMeshRenderer.sharedMaterial.name : "null")}");
                        
                        string assetPath = AssetDatabase.GetAssetPath(skinnedMeshRenderer.sharedMesh);
                        string name = Path.GetFileNameWithoutExtension(assetPath) + "_" +
                                      skinnedMeshRenderer.sharedMesh.name;
                        string path = string.Format("{0}{1}.asset", MeshDir, name);

                        Vector3[] vertices = skinnedMeshRenderer.sharedMesh.vertices;

                        Mesh mesh;
                        //death 不需要更新mesh（会把bound box 改错了）
                        //if (!File.Exists(path))
                        {
                            mesh = (Mesh)Instantiate(skinnedMeshRenderer.sharedMesh);
                            Mesh boundMesh = new Mesh();
                            skinnedMeshRenderer.BakeMesh(boundMesh);
                            boundMesh.RecalculateBounds();

                            Color[] colorArray = new Color[vertices.Length];
                            BoneWeight[] boneWeights = skinnedMeshRenderer.sharedMesh.boneWeights;

                            for (int num = 0; num < vertices.Length; num++)
                            {
                                Transform trans = skinnedMeshRenderer.bones[boneWeights[num].boneIndex0];
                                Transform trans1 = skinnedMeshRenderer.bones[boneWeights[num].boneIndex1];
                                int index = bonesList.IndexOf(trans);
                                int index1 = bonesList.IndexOf(trans1);

                                colorArray[num] = new Color(1 - boneWeights[num].weight0, ((index1 * 2) + 1) / 255f,
                                    boneWeights[num].weight0, ((index * 2) + 1) / 255f);
                            }

                            mesh.vertices = vertices;
                            mesh.colors = colorArray;
                            mesh.bounds = boundMesh.bounds;

                            mesh.bindposes = null;
                            mesh.boneWeights = null;
                            mesh.UploadMeshData(true);
                            
                            SaveAsset(mesh, path);
                            Debug.Log($"Mesh 保存到: {path}");
                        }
                        mesh = AssetDatabase.LoadMainAssetAtPath(path) as Mesh;
                        if (mesh == null)
                        {
                            Debug.LogError($"加载 Mesh 失败: {path}");
                        }
                        else
                        {
                            Debug.Log($"加载 Mesh 成功: {mesh.name} 从 {path}");
                        }

                        var mat = CreateGPUSkinMat(skinnedMeshRenderer.sharedMaterial);
                        Debug.Log($"创建/获取 GPU 材质: {(mat != null ? mat.name : "null")} 用于 {skinnedMeshRenderer.gameObject.name}");

                        GameObject goRenderer = skinnedMeshRenderer.gameObject;
                        var meshFilter = goRenderer.AddComponent<MeshFilter>();
                        meshFilter.sharedMesh = mesh;
                        Debug.Log($"设置 MeshFilter.sharedMesh: {(mesh != null ? mesh.name : "null")} 到 {goRenderer.name}");
                        var meshRenderer = goRenderer.AddComponent<MeshRenderer>();

                        if (vertices.Length < 300)
                        {
                            meshRenderer.transform.localScale = new Vector3(1 - canBatchNum*0.001f,
                                1 - canBatchNum*0.001f,
                                1 - canBatchNum*0.001f);
                            canBatchNum++;
                        }

                        meshRenderer.sharedMaterial = mat;
                        Debug.Log($"赋予材质给 MeshRenderer: {meshRenderer.gameObject.name}, 材质: {(mat != null ? mat.name : "null")}");
                        
                        meshRenderer.useLightProbes = skinnedMeshRenderer.useLightProbes;
                        meshRenderer.castShadows = skinnedMeshRenderer.castShadows;
                        meshRenderer.receiveShadows = skinnedMeshRenderer.receiveShadows;
                        

                        GameObject.DestroyImmediate(skinnedMeshRenderer);
                        renders.Add(meshRenderer);
                    }

                    // GoPoolBehaviourItem poolBehaviourItem = go.GetComponent<GoPoolBehaviourItem>();
                    // if (poolBehaviourItem == null)
                    // {
                    //     poolBehaviourItem = go.AddComponent<GoPoolBehaviourItem>();
                    // }
                    // poolBehaviourItem.EditorRefresh();
                    
                    gpuSkinRenderer.m_renderers = renders.ToArray();
                    var eventHandler = go.GetComponent<AnimatorEventHandler>();
                    if (eventHandler == null)
                    {
                        go.AddComponent<AnimatorEventHandler>();
                    }
                }
                else
                {
                    Debug.LogError(string.Format("{0}的骨骼数量{1}超过最大上限{2}", go.name, bonesList.Count,
                        GPUSkinRenderer.MaxBoneNum));
                }

                //EditorApplication.update = null;
            };
    }
    
    private const string MeshDir = "Assets/MeshData/";

    [MenuItem("Assets/模型处理功能/生成GPU Skin",  false, 102)]
    private static void Convert()
    {
        var objs = Selection.gameObjects;
        foreach (var o in objs)
        {
            GameObject go = (GameObject) Instantiate(o);
            ConvertGpuSkinRender(go);
        }
    }

    private static Material CreateGPUSkinMat(Material material)
    {
        if (material.shader.name.EndsWith(" GPU Skin"))
        {
            return material;
        }

        string assetPath = AssetDatabase.GetAssetPath(material);
        string fileName = Path.GetFileNameWithoutExtension(assetPath);
        string dir = Path.GetDirectoryName(assetPath);
        fileName = fileName + "_gpu";
        string path = Path.Combine(dir, fileName + ".mat");
        
        // 检查材质是否已存在，如果存在直接返回
        Material existingMat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existingMat != null)
        {
            Debug.Log($"使用已存在的 GPU 材质: {path}");
            return existingMat;
        }

        Shader shader = Shader.Find(material.shader.name + " GPU Skin");
        if (shader==null)
        {
            shader= Shader.Find("Dodjoy/Actor/ActorToonGpuSkinOnly");
        }

        Material retMat = new Material(material)
        {
            shader = shader
        };
        retMat.name = fileName;

        SaveAsset(retMat, path);
        Debug.Log($"创建新的 GPU 材质: {path}");

        retMat = AssetDatabase.LoadMainAssetAtPath(path) as Material;
        return retMat;
    }

    //[MenuItem("Assets/模型处理功能/处理IOS的GPU")]
    public static void PrepareForIOS()
    {
        DirectoryInfo directory = new DirectoryInfo("Assets/Resources/Actor");
        foreach (var fileInfo in directory.GetFiles("*_gpu.prefab", SearchOption.AllDirectories))
        {
            if (fileInfo.Extension.ToLower() == ".prefab")
            {
                string orginPath = fileInfo.FullName.Replace("Resources", "Prefabs").Replace("_gpu", "");
                if (File.Exists(orginPath))
                {
                    File.Copy(orginPath, fileInfo.FullName, true);
                }
            }
        }

        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
    }
}
