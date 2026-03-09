using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using DodGame;
using UnityEditor.Animations;
using Object = UnityEngine.Object;

public enum ModelType
{
    [LabelText("主角")]
    Hero,
    [LabelText("怪物")]
    Monster,
    [LabelText("精英&Boss")]
    Boss,
    [LabelText("主角gpu动画")]
    HeroGPU,
    [LabelText("Npc")]
    NPC,
    [LabelText("坐骑")]
    Vehicles,
}
public class ModelEditor : OdinEditorWindow
{
    [SerializeField]
    [LabelText("角色模型FBX")]
    [AssetsOnly]
    [OnValueChanged("OnModelFBXChanged")]
    protected GameObject m_prefab;

    [Sirenix.OdinInspector.FilePath(ParentFolder = "Assets/GameModel/AnimatorControllerTemplate", Extensions = ".controller")]
    [LabelText("选择动画状态机模板")]
    public string m_tmpCtrlPath = "Monster_Template.controller";
    [FolderPath]
    [AssetsOnly]
    [OnValueChanged("OnAnimationFolderChanged")]
    [LabelText("动画文件夹")]
    public string AnimationFolder;
    
    [LabelText("状态机存放目录")]
    [ShowIf("m_modelType", ModelType.Hero)]
    public string AnimatorControllerSaveDir = "Assets/Resources/Animator/Player/";
    //[SerializeField]
    //[ReadOnly]
    protected UnityEngine.Object[] m_selectedObjects;
    
    [LabelText("模型类型")]
    [OnValueChanged("OnModeTypeChanged")]
    public ModelType m_modelType;

    [FolderPath]
    [LabelText("gpu预制体输出目录")]
    [OnValueChanged("OnDestSaveDirChanged")]
    public string m_prefabSaveDir;
    public GameObject m_destPrefab;
    [LabelText("保存预制体文件名前缀")]
    [OnValueChanged("OnModelFBXChanged")]
    [ShowIf("m_modelType", ModelType.Hero | ModelType.HeroGPU)]
    //[ShowIf("m_modelType")]
    public string m_prefix;
    private string m_modelName;
    private string m_destPath;
    private bool m_inited = false;
    // [LabelText("当前状态机")]
    // public AnimatorController m_selectedCtrl; 
    
    private ModelEditorSettings m_modelEditorSettings;

    void RefreshAnimations()
    {
        if(string.IsNullOrEmpty(AnimationFolder))
            return;
        
        var assets = AssetDatabase.FindAssets("t:GameObject", new string[] { AnimationFolder.Replace('\\', '/') });
        List<Object> objects = new List<Object>();
        foreach (var asset in assets)
        {
            var path = AssetDatabase.GUIDToAssetPath(asset);
            if (path.EndsWith(".fbx", System.StringComparison.CurrentCultureIgnoreCase) && path.IndexOf("@") >= 0)
            {
                objects.Add(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            }
        }
        m_selectedObjects = objects.ToArray();

        if (m_prefab != null)
        {
            var assetPath = AssetDatabase.GetAssetPath(m_prefab);
            if(!string.IsNullOrEmpty(assetPath))
                ActorAnimationConfig.Instance.Set(assetPath, AnimationFolder, m_prefabSaveDir);    
        }
    }
    
    public void OnAnimationFolderChanged()
    {
        RefreshAnimations();

        m_prefix = Path.GetFileNameWithoutExtension(AnimationFolder);
        RefreshSavePath();
    }
    public void Init(GameObject prefab)
    {
        m_prefab = prefab;
        AnimationFolder = string.Empty;
        m_modelName = string.Empty;
        m_destPath = string.Empty;
        m_inited = false;
        m_prefix = string.Empty;
        m_prefabSaveDir = string.Empty;
        m_destPrefab = null;
        OnModelFBXChanged();
    }
    [Button("创建")]
    private void OnWizardCreate()
    {
        if(!m_inited)
        {
            OnModelFBXChanged();
        }
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();
        var prefabPath = AssetDatabase.GetAssetPath(m_prefab);
        var controller = ModelTools.InitAnimationControllers(m_prefab, "Assets/GameModel/AnimatorControllerTemplate/" + m_tmpCtrlPath, 
            m_modelType == ModelType.HeroGPU | m_modelType == ModelType.Hero,//主角的gpu动画需要选择模板
            m_modelType == ModelType.HeroGPU ? m_prefix + "_GPU"  : m_modelName);
        foreach (var item0 in m_selectedObjects)
        {
            string path = AssetDatabase.GetAssetPath(item0);
            Object[] assetObjects = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var item in assetObjects)
            {
                ModelTools.ProcessAnimClip(item, prefabPath, path);
            }
        }

        //AnimationTool.OnDumplicateAnimations(m_selectedObjects, m_prefab, cfgDir, allReload: true);
        sw.Stop();
        Debug.Log("total cost " + sw.ElapsedMilliseconds.ToString());

        var prefab = GenPrefab();
        if (prefab)
        {
            var animator = prefab.GetComponent<Animator>();
            if (animator)
            {
                animator.runtimeAnimatorController = controller;
            }
            
            if (m_modelType == ModelType.Monster ||
                m_modelType == ModelType.NPC || m_modelType == ModelType.HeroGPU ||
                m_modelType == ModelType.Vehicles)
            {
                GpuAnimEditor editor = GpuAnimEditor.GetWindow<GpuAnimEditor>();
                var gpuAnimSaveDir = Path.GetDirectoryName(prefabPath);
                var gpuPrefabSavePath = Path.Combine(gpuAnimSaveDir, m_modelName + "_tps_gpu_anim.prefab");
                var param = new GPUAnimParam()
                {
                    m_destPath =  gpuPrefabSavePath,
                    m_shader = (m_modelType == ModelType.Hero) ? Shader.Find("Dodjoy/Actor/ActorToonGpuSkin") : Shader.Find("Dodjoy/Actor/ActorToonGpuSkin"),
                    m_materialNameSuffix = (m_modelType == ModelType.HeroGPU) ? "_gpu_0_anim.mat" : "_gpu_anim.mat"
                };
                string[] extrBones = null;
                if (m_modelType == ModelType.Hero || m_modelType == ModelType.HeroGPU)
                {
                    extrBones = new string[]
                    {
                        "DM_ChEST",
                        "DM_HEAD",
                        "DM_L_WEAPON",
                        "DM_R_WEAPON",
                        "DM_UI"
                    };
                }
                editor.Init(prefab, param, () =>
                {
                    //PrefabUtility.SaveAsPrefabAsset(prefab, m_destPath);
                    var destDir = Path.GetDirectoryName(m_destPath);
                    if (!Directory.Exists(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }
                    AssetDatabase.CopyAsset(gpuPrefabSavePath, m_destPath);
                            
                    ModelTools.RemoveBone(m_destPath);
                    GenGpuMesh(m_destPath);
                    m_destPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_destPath);
                    return true;
                }, additionBones: extrBones);
                editor.Show();
            }
            else 
            {
                GameObject go = Object.Instantiate(prefab);
                ConvertGPUSkinMesh.ConvertGpuSkinRender(go);
                var dir = Path.GetDirectoryName(m_destPath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                // GoPoolBehaviourItem poolBehaviourItem = go.GetComponent<GoPoolBehaviourItem>();
                // if (poolBehaviourItem == null)
                // {
                //     poolBehaviourItem = go.AddComponent<GoPoolBehaviourItem>();
                // }
                // poolBehaviourItem.EditorRefresh();

                PrefabUtility.SaveAsPrefabAsset(go, m_destPath);
                
                // 添加 IK 组件到处理后的预制体
                var savedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_destPath);
                if (savedPrefab != null)
                {
                    // 重新实例化保存的预制体来添加IK组件
                    GameObject destGo = Object.Instantiate(savedPrefab);
                    
                    // 1. 添加 Aim IK 组件
                    var aimIK = destGo.GetComponent<RootMotion.FinalIK.AimIK>();
                    if (aimIK == null)
                    {
                        aimIK = destGo.AddComponent<RootMotion.FinalIK.AimIK>();
                        // 查找 Bip001 Spine_IK
                        Transform spineIK = destGo.transform.Find("Bip001 Spine_IK");
                        if (spineIK != null)
                        {
                            aimIK.solver.transform = spineIK;
                            // 添加一个 Bone
                            var bone = new RootMotion.FinalIK.IKSolver.Bone();
                            bone.transform = spineIK;
                            aimIK.solver.bones = new RootMotion.FinalIK.IKSolver.Bone[] { bone };
                            Debug.Log($"Aim IK 添加成功，Bones 设置为: {spineIK.name}");
                        }
                        else
                        {
                            Debug.LogWarning("未找到 Bip001 Spine_IK");
                        }
                    }
                    
                    // 2. 添加 Left Hand IK 组件（不需要手动添加 LimbIK，LeftHandIK 会自己管理）
                    var leftHandIK = destGo.GetComponent<LeftHandIK>();
                    if (leftHandIK == null)
                    {
                        leftHandIK = destGo.AddComponent<LeftHandIK>();
                        Debug.Log("Left Hand IK 添加成功");
                    }
                    
                    // 3. 添加 Human IK 组件
                    var humanIK = destGo.GetComponent<HumanIK>();
                    if (humanIK == null)
                    {
                        humanIK = destGo.AddComponent<HumanIK>();
                        // 设置 Human IK 的基本参数
                        humanIK.m_bodyYawSpeed = 180f;
                        humanIK.m_bodyYawStandSpeed = 30f;
                        humanIK.m_enableIK = true;
                        Debug.Log("Human IK 添加成功");
                    }
                    
                    // 检查目标预制体是否有 IK 组件
                    var destAimIK = destGo.GetComponent<RootMotion.FinalIK.AimIK>();
                    var destHumanIK = destGo.GetComponent<HumanIK>();
                    var destLimbIKs = destGo.GetComponents<RootMotion.FinalIK.LimbIK>();
                    Debug.Log($"目标预制体 {destGo.name} 的 IK 组件: AimIK={(destAimIK != null)}, HumanIK={(destHumanIK != null)}, LimbIKs={destLimbIKs.Length}");
                    
                    // 标记所有组件为 dirty
                    if (destAimIK != null) EditorUtility.SetDirty(destAimIK);
                    if (destHumanIK != null) EditorUtility.SetDirty(destHumanIK);
                    foreach (var limbIK in destLimbIKs) EditorUtility.SetDirty(limbIK);
                    var destLeftHandIK = destGo.GetComponent<LeftHandIK>();
                    if (destLeftHandIK != null) EditorUtility.SetDirty(destLeftHandIK);
                    EditorUtility.SetDirty(destGo);
                    
                    // 保存修改后的预制体
                    PrefabUtility.SaveAsPrefabAsset(destGo, m_destPath);
                    Object.DestroyImmediate(destGo);
                }
                
                m_destPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_destPath);
                if (m_modelType == ModelType.Hero)
                {
                    CopyHeroAnimatorControllerToResources();
                }
            }
        }
        
    }
    
    void CopyHeroAnimatorControllerToResources()
    {
        var tempCtrlPath = "Assets/GameModel/AnimatorControllerTemplate/" + m_tmpCtrlPath;
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(tempCtrlPath);
        var assets = Directory.GetFiles(AnimationFolder, "*.anim", SearchOption.AllDirectories);
        List<AnimationClip> newClips = new List<AnimationClip>();
        foreach (var asset in assets)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(asset);
            if (clip)
            {
                newClips.Add(clip);
            }
        }
        Debug.LogError("assets count:" + assets.Length);
        var animDirName = Path.GetFileName(AnimationFolder).Replace("@", "_");
        var destControllerPath = Path.Combine (AnimatorControllerSaveDir, animDirName+ ".controller");
        if (File.Exists(destControllerPath))
        {
            var overrideController = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(destControllerPath);
            if (overrideController != null)
            {
                foreach (var clip in newClips)
                {
                    var layerCount = controller.layers.Length;
                    for (int i = 0; i < layerCount; i++)
                    {
                        var layer = controller.layers[i];
                        var stateMachine = layer.stateMachine;
                        string clipName = GetOverrideClipName(stateMachine, clip.name);
                        if (!string.IsNullOrEmpty(clipName))
                        {
                            overrideController[clipName] = clip;
                            break;
                        }
                    }
                }
                
            }
            EditorUtility.SetDirty(overrideController);
        }
        else
        {
            AnimatorOverrideController overrideController = new AnimatorOverrideController(controller);
            var clips = overrideController.animationClips;
            foreach (var clip in newClips)
            {
                var layerCount = controller.layers.Length;
                for (int i = 0; i < layerCount; i++)
                {
                    var layer = controller.layers[i];
                    var stateMachine = layer.stateMachine;
                    string clipName = GetOverrideClipName(stateMachine, clip.name);
                    if (!string.IsNullOrEmpty(clipName))
                    {
                        overrideController[clipName] = clip;
                        break;
                    }
                }
            }

            AssetDatabase.CreateAsset(overrideController, destControllerPath);
        }
    }
    
    private static string ProcessBlendTree(BlendTree tree, string clipName)
    {
        var children = tree.children;
        for (int k = 0; k < children.Length; k++)
        {
            var child = children[k];
            if(child.motion == null)
                continue;
            
            if (child.motion is BlendTree childTree)
            {
                string result = ProcessBlendTree(childTree, clipName);
                if (!string.IsNullOrEmpty(result))
                {
                    return result;
                }
            }
            else
            {
                if(child.motion is AnimationClip && clipName.EndsWith(child.motion.name) )
                {
                    return child.motion.name;
                }
            }
        }

        return string.Empty;
    }

    string  GetOverrideClipName(AnimatorStateMachine stateMachine, string clipName)
    {
        var monsterAnimState = ModelTools.m_monsterAnimState;
        var allStates = stateMachine.states;
        for (int j = 0; j < allStates.Length; j++)
        {
            var theState = allStates[j];

            Motion theMotion = theState.state.motion;
            if(theMotion == null)
                continue;
            if (theMotion is BlendTree btree)
            {
                var result= ProcessBlendTree(btree, clipName);
                if (!string.IsNullOrEmpty(result))
                    return result;
                continue;
            }

            if (theState.state.name != "null")
            {
                List<string> m_listMotionName;
                if (monsterAnimState.TryGetValue(theState.state.name, out m_listMotionName))
                {
                    var animName = clipName;
                    if (m_listMotionName.Contains(animName))
                    {
                        return theState.state.motion.name;
                    }
                    else
                    {
                        var index = animName.IndexOf('_');
                        if (index >= 0)
                        {
                            var name = animName.Substring(index + 1);
                            if (m_listMotionName.Contains(name))
                            {
                                return theState.state.motion.name;
                            }
                        }
                    }
                }
            }
        }
        foreach (var sm in stateMachine.stateMachines)
        {
            var name = GetOverrideClipName(sm.stateMachine, clipName);
            if (!string.IsNullOrEmpty(name))
                return name;
        }
        return string.Empty;
    }

    public static void GenGpuMesh(string path) 
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            return;
        }

        var gpuAnimRenderer = prefab.GetComponent<GpuAnimRenderer>();
        if (gpuAnimRenderer == null)
        {
            return;
        }

        var meshfilters = prefab.GetComponentsInChildren<MeshFilter>();
        if (meshfilters != null)
        {
            for(int i = 0; i < meshfilters.Length; i++)
            {
                var meshfilter = meshfilters[i];
                var meshpath = AssetDatabase.GetAssetPath(meshfilter.sharedMesh);
                //Debug.LogError(meshpath);
                if (meshpath.IndexOf("Assets/ActorModel", StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                    meshpath.EndsWith(".fbx", StringComparison.CurrentCultureIgnoreCase))
                {
                    CreateMesh(gpuAnimRenderer, meshfilter, i);
                    EditorUtility.SetDirty(prefab);
                    AssetDatabase.SaveAssets();
                }
            }
        }
    }

    public static void CreateMesh(GpuAnimRenderer gpuAnimRenderer, MeshFilter meshfilter, int index)
    {
        var scriptObj = AssetDatabase.LoadAssetAtPath<GpuAnimTexScriptObject>(
            string.Format("Assets/Resources/GpuInst/anim/{0}.asset", gpuAnimRenderer.m_animTexName));
        if (scriptObj)
        {
            var gpuMesh = Object.Instantiate(meshfilter.sharedMesh);
            if (scriptObj.m_data == null)
                return;
            
            GpuAnimMeshData meshData = null;
            for (int i = 0; i < scriptObj.m_data.m_metaData.m_arrMeshData.Length; i++)
            {
                var item = scriptObj.m_data.m_metaData.m_arrMeshData[i];
                if (item.meshBoneIndex.Length == gpuMesh.vertices.Length)
                {
                    meshData = item;
                    break;
                }
            }
            
            Color[] colors = new Color[meshData.meshBoneWeight.Length];
            for (int i = 0; i != colors.Length; ++i)
            {
                colors[i].r = meshData.meshBoneWeight[i].x;
                colors[i].g = meshData.meshBoneWeight[i].y;
                colors[i].b = meshData.meshBoneWeight[i].z;
                colors[i].a = meshData.meshBoneWeight[i].w;
            }

            gpuMesh.colors = colors;
            gpuMesh.SetUVs(2, meshData.meshBoneIndex);
            gpuMesh.UploadMeshData(true);
            string destPath = "Assets/GpuMesh/" + meshfilter.sharedMesh.name + ".asset";
            if (File.Exists(destPath))
            {
                AssetDatabase.DeleteAsset(destPath);
            }
            AssetDatabase.CreateAsset(gpuMesh, destPath);
            meshfilter.sharedMesh = gpuMesh;
            //Debug.LogError(path);
            //break;
        }
    }

    [MenuItem("Assets/模型处理功能/打开模型导出工具", false, 101)]
    static void OpenModelEditor()
    {
        var selectedPrefab = Selection.activeObject as GameObject;
        string assetPath = AssetDatabase.GetAssetPath(selectedPrefab);
        if(assetPath.EndsWith(".fbx", System.StringComparison.CurrentCultureIgnoreCase) )
        {
            var window = ModelEditor.GetWindow<ModelEditor>();
            
            window.Init(selectedPrefab);
            window.Show();
        }
        else
        {
            Debug.LogError("请打开模型fbx文件");
        }
    }

    void RefreshSavePath()
    {
        var assetPath = AssetDatabase.GetAssetPath(m_prefab);
        if (string.IsNullOrEmpty(assetPath))
            return;

        var splits = assetPath.Split('/');
        if (splits.Length == 0)
            return;

        if (string.IsNullOrEmpty(m_prefix))
        {
            m_prefix = splits[splits.Length - 2];
        }
        
        m_destPath = string.Empty;
        
        
        if (assetPath.IndexOf("Player") >= 0)
        {
            if (m_modelType == ModelType.HeroGPU)
            {
                 m_prefabSaveDir = "Assets/Resources/Actor/Player/GPU";
                m_modelName = m_prefix + "_" + m_prefab.name + "_tps_gpu_anim";
                m_destPath =Path.Combine(m_prefabSaveDir , m_modelName+ ".prefab") ;
            }
            else
            {
                // m_prefabSaveDir = "Assets/Resources/Actor/Player/";
                m_modelName = m_prefix + "_" + m_prefab.name + "_tps_gpu_skin";
                m_destPath = m_prefabSaveDir + m_modelName + ".prefab";
                m_modelType = ModelType.Hero;
            }
        }
        else if (assetPath.IndexOf("Monster/Enemy") >= 0)
        {
            m_modelType = ModelType.Monster;
            // m_prefabSaveDir = "Assets/Resources/Actor/Monster/Enemy/";
            m_modelName = m_prefab.name;
            m_destPath = m_prefabSaveDir + m_modelName + "_tps_gpu_anim.prefab";
        }
        else if (assetPath.IndexOf("NPC") >= 0)
        {
            m_modelType = ModelType.NPC;
            // m_prefabSaveDir = "Assets/Resources/Actor/Monster/Enemy/";
            m_modelName = m_prefab.name;
            m_destPath = m_prefabSaveDir + m_modelName + "_tps_gpu_anim.prefab";
        }
        else if (assetPath.IndexOf("Vehicles") >= 0)
        {
            m_modelType = ModelType.Vehicles;
            // m_prefabSaveDir = "Assets/Resources/Actor/Monster/Enemy/";
            m_modelName = m_prefab.name;
            m_destPath = m_prefabSaveDir + m_modelName + "_tps_gpu_anim.prefab";
        }
        else if (assetPath.IndexOf("Monster/Boss") >= 0)
        {
            m_modelType = ModelType.Boss;
            // m_prefabSaveDir = "Assets/Resources/Actor/Boss/";
            m_modelName = m_prefab.name;
            m_destPath = m_prefabSaveDir + m_modelName + "_tps_gpu_anim.prefab";
        }

        if (m_modelType != ModelType.HeroGPU && GetModelSettingsConfig(out var modelSaveData))
        {
            m_prefabSaveDir = modelSaveData.PrefabSaveDir;
            if (!string.IsNullOrEmpty(assetPath))
            {
                m_modelName = m_prefab.name;
                m_destPath = Path.Combine(m_prefabSaveDir, m_modelName + "_tps_gpu_anim.prefab") ;
            }
        }
        
        
        m_destPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_destPath);
    }

    bool GetModelSettingsConfig(out ModelSaveData modelSaveData)
    {
        if (!ModelEditorSettings.Instance.GetModelSaveDataByModelType(m_modelType, out modelSaveData))
        {
            Debug.LogError("找不到该模型类型的配置！");
            return false;
        }

        return true;
    }

    void OnModelFBXChanged()
    {
        var assetPath = AssetDatabase.GetAssetPath(m_prefab);
        if (string.IsNullOrEmpty(assetPath))
            return;
        
        if(string.IsNullOrEmpty(AnimationFolder))
        {
            AnimationFolder = Path.GetDirectoryName(assetPath);
        }
        OnAnimationFolderChanged();
        

        if (ActorAnimationConfig.Instance.TryGetDir(assetPath, out var config))
        {
            AnimationFolder = config.animDir;
            if (!string.IsNullOrEmpty(config.saveDir))
            {
                m_prefabSaveDir = config.saveDir;
            }
            OnDestSaveDirChanged();
        }
        
        m_inited = true;
    }

    void OnDestSaveDirChanged()
    {
        m_destPath = Path.Combine(m_prefabSaveDir, m_modelName + "_tps_gpu_anim.prefab") ;
    }

    GameObject GenPrefab()
    {
        var path = ModelTools.CreateMonsterPrefab(m_prefab, m_modelName);
        if (!string.IsNullOrEmpty(path))
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        return null;
    }

    
    void OnModeTypeChanged()
    {
        if (m_modelType == ModelType.HeroGPU)
        {
            m_tmpCtrlPath = "HeroGPUCtrl.controller";
        }
        else
        {
            var selectedFile = EditorUtility.OpenFilePanel("选择状态机模板", "Assets/ActorModel/AnimatorControllerTemplate/", "controller");
            m_tmpCtrlPath = Path.GetFileName(selectedFile);
        }
        OnModelFBXChanged();
    }
    
    /// <summary>
    /// 递归查找 Transform
    /// </summary>
    Transform FindTransformRecursive(Transform parent, string name)
    {
        Transform result = parent.Find(name);
        if (result != null)
            return result;
        
        foreach (Transform child in parent)
        {
            result = FindTransformRecursive(child, name);
            if (result != null)
                return result;
        }
        return null;
    }
}
