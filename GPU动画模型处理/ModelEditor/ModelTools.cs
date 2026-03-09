using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using DodGame;
using A9Game;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;
using AnimatorControllerParameterType = UnityEngine.AnimatorControllerParameterType;
using Object = UnityEngine.Object;
public class DumplicateAnimationWizard : ScriptableWizard
{
    public GameObject m_prefab;
    public UnityEngine.Object[] m_selectedObjects;
    private void OnWizardCreate()
    {
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();
        var prefabPath = AssetDatabase.GetAssetPath(m_prefab);
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
    }

}


public class ModelTools : EditorWindow
{
    public static Dictionary<string, List<string>> m_monsterAnimState = new Dictionary<string, List<string>>()
    { 
        {"birth", new List<string> { "birth"}},
        {"run_fast", new List<string> { "run_fast", "run1"}},
        {"run", new List<string> { "run", "run2","run02","default_run_fast","default_run"}},
        {"jump_start", new List<string> { "jump_up", "jump_start"}},
        {"jump_loop", new List<string> { "jump_loop"}},
        {"jump_land", new List<string> { "jump_down", "jump_land"}},
        {"idle", new List<string> { "idle","default_idle"}},
        {"trip", new List<string> { "trip"}},
        {"sprint", new List<string> { "sprint", "chongci"}},
        {"attack", new List<string> { "attack", "attack1","attack01","default_attack01"}},
        {"fire1_start", new List<string> { "attack", "attack1","attack01","default_attack01"}},
        {"wound 0", new List<string> { "wound01", "wound_mid"}},
        {"wound", new List<string> { "wound"}},
        {"die", new List<string> { "die", "death","default_die"}},
        {"flyDeath", new List<string> { "die1", "flydie"}},
        {"flyDeath2", new List<string> { ""}},
        {"attack_play", new List<string> { ""}},
        {"walk", new List<string> { "walk", }},
        {"walk_b", new List<string> { "walk_b"}},
        {"walk_f", new List<string> { "walk_f"}},
        {"walk_l", new List<string> { "walk_l"}},
        {"walk_r", new List<string> { "walk_r"}},
        {"Open", new List<string> { "open_door"}},
        {"Collect", new List<string> { "common_collect"}},
        {"giddy", new List<string> { "giddy","default_giddy"}},
        {"jump", new List<string> { "default_jump", "default_jump_down", "default_jump_loop", "default_jump_up"}},
        {"switch", new List<string> { "switch","default_switch"}},
        {"social", new List<string> { "social_hi"}},
        {"find", new List<string> { "find"}},
        {"ground", new List<string> { "idle","default_idle"}},
        {"death", new List<string> { "death" }},
        {"SwitchFirearms", new List<string> { "switch","default_switch"}},
    };

    [MenuItem("Effect Editor/Select AnimationController")]
    public static void ShowWindow()
    {
        //EditorApplication.OpenScene("Assets/Scenes/SkillEditor.unity");
        //EditorApplication.isPlaying = true;
        GetWindow(typeof(ModelTools));
    }

    private static UnityEditor.Animations.AnimatorController selectAnimatorController = null;

    public void OnGUI()
    {

        selectAnimatorController = (UnityEditor.Animations.AnimatorController)EditorGUI.ObjectField(new Rect(0, 0, 300, 20), selectAnimatorController, typeof(UnityEditor.Animations.AnimatorController));
        GUILayout.Space(40);
        if (GUILayout.Button("Add state"))
        {
            AddState();
        }
    }

    //[MenuItem("Assets/模型处理功能/设置单个动作文件的属性信息")]
    private static void SetSelectFBXSettings()
    {
        for (int i = 0; i < Selection.objects.Length; i++)
        {
            string path = AssetDatabase.GetAssetPath(Selection.objects[i]);
            ModelImporter modelObject = AssetImporter.GetAtPath(path) as ModelImporter;
            if (modelObject != null)
            {
                //设置模型文件的属性
                //设置model
                modelObject.materialImportMode = ModelImporterMaterialImportMode.None;
                modelObject.isReadable = false;
                modelObject.globalScale = 0.01f;
                modelObject.useFileScale = false;
                modelObject.importTangents = ModelImporterTangents.CalculateLegacyWithSplitTangents;
                modelObject.importBlendShapes = false;
                modelObject.importVisibility = false;
                modelObject.importCameras = false;
                modelObject.importLights = false;
                modelObject.indexFormat = ModelImporterIndexFormat.UInt16;
                modelObject.importNormals = ModelImporterNormals.Import;
                modelObject.normalCalculationMode = ModelImporterNormalCalculationMode.Unweighted_Legacy;

                //设置RIG
                modelObject.animationType = ModelImporterAnimationType.Generic;
                modelObject.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                modelObject.generateAnimations = ModelImporterGenerateAnimations.InOriginalRoots;
                modelObject.animationPositionError = 0.1f;
                modelObject.animationRotationError = 0.1f;
                modelObject.animationScaleError = 0.1f;

                PropertyInfo prop = typeof(ModelImporter).GetProperty("importedTakeInfos", BindingFlags.NonPublic | BindingFlags.Instance);
                if (prop != null)
                {
                    TakeInfo[] takeinfo = (TakeInfo[])prop.GetValue(modelObject, null);
                    if (takeinfo.Length > 0)
                    {
                        ModelImporterClipAnimation[] clips = AnimImportor.SetupDefaultClips(modelObject, takeinfo);
                        modelObject.clipAnimations = clips;
                    }
                }
                AssetDatabase.ImportAsset(modelObject.assetPath, ImportAssetOptions.ForceUpdate);
            }
        }
    }

    private static List<string> m_needLoopAnim = new List<string>(){
        "idle",
        "run01",
        "run1",
        "run",
        "run2",
        "run02",
        "tiaoyue_loop",
        "sprint",
        "chongci",
    };

   public static AnimatorController InitAnimationControllers(Object go, string tempPath, bool force, string filenameSuffix = "")
    {
        controllers = new List<AnimatorController>();
        string path = AssetDatabase.GetAssetPath(go);
        string[] paths = path.Split('/');
        var pathController = path.Remove(path.IndexOf(paths[paths.Length - 1]));
        System.IO.DirectoryInfo rootDir = new System.IO.DirectoryInfo(pathController);
        System.IO.FileInfo[] files = null;
        files = rootDir.GetFiles("*.controller", System.IO.SearchOption.AllDirectories);
        if (force || files.Length <= 0)
        {
            if (string.IsNullOrEmpty(tempPath))
            {
                Debug.LogError("找不到animationController");
                return null;
            }
            string comControl = tempPath;
            string newpath = pathController + "/"  + filenameSuffix + "_tps_animctrl.controller";
            if (File.Exists(newpath))
            {
                selectAnimatorController =AssetDatabase.LoadAssetAtPath<AnimatorController>(newpath);
                return selectAnimatorController;
            }
            AssetDatabase.CopyAsset(comControl, newpath);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            selectAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(newpath);
            return selectAnimatorController;
        }
        files = rootDir.GetFiles("*.controller", System.IO.SearchOption.AllDirectories);
        for (int i = 0; i < files.Length; i++)
        {
            //提取动作
            string thePath = files[i].FullName.Replace("\\", "/");
            thePath = thePath.Substring(thePath.IndexOf("Assets"));
            Object[] objectsLoad = AssetDatabase.LoadAllAssetsAtPath(thePath);
            for (int j = 0; j < objectsLoad.Length; j++)
            {
                if (objectsLoad[j] is UnityEditor.Animations.AnimatorController)
                {
                    controllers.Add(objectsLoad[j] as UnityEditor.Animations.AnimatorController);
                }
            }
        }

        if (controllers != null)
            selectAnimatorController = controllers[0];
        return selectAnimatorController;
    }

    [MenuItem("Assets/生成主角动画状态机")]
    private static void GenPlayerAnimatorController()
    {
        var selection = Selection.activeObject;
        if(selection != null)
        {
            var assetPath = AssetDatabase.GetAssetPath(selection);
            var dirRoot = Path.GetDirectoryName(assetPath);
            dirRoot = dirRoot.Replace('\\', '/');
            if (Directory.Exists(dirRoot))
            {
                var dirName = Path.GetFileName(dirRoot);
                string playerName = string.Empty;
                var playerDir = "Assets/GameModel/Player/";
                var index = dirRoot.IndexOf('/', playerDir.Length);
                if(index > 0)
                {
                    playerName = dirRoot.Substring(playerDir.Length, index - playerDir.Length);
                }
                
                var templateAnimCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"Assets/GameModel/Player/{playerName}/Animation/player_animctrl_template.controller");
                var destOverrideAnimCtrl = "Assets/Resources/Animator/Player/player_" + playerName + "_tps_" + dirName + ".overrideController";
                AnimatorOverrideController overrideCtrl = null;
                if (File.Exists(destOverrideAnimCtrl))
                    overrideCtrl = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(destOverrideAnimCtrl);
                else
                {
                    overrideCtrl = new AnimatorOverrideController();
                    AssetDatabase.CreateAsset(overrideCtrl, destOverrideAnimCtrl);
                }
                    
                overrideCtrl.runtimeAnimatorController = templateAnimCtrl;
                var animFiles = Directory.GetFiles(dirRoot, "*.anim");
                foreach(var file in animFiles)
                {
                    foreach(var clip in overrideCtrl.clips)
                    {
                        var clipName = clip.originalClip.name;
                        clipName = clipName.Replace("Rifle_", "");
                        if (clip.originalClip != null && file.IndexOf(dirName + "_" + clipName, StringComparison.CurrentCultureIgnoreCase) >= 0)
                        {
                            overrideCtrl[clip.originalClip] = AssetDatabase.LoadAssetAtPath<AnimationClip>(file);
                            break;
                        }
                    }
                }

                AssetDatabase.SaveAssets();
            }
        }
    }

    private static List<UnityEditor.Animations.AnimatorController> controllers;
    [MenuItem("Assets/模型处理功能/复制选择的文件的动画出来")]
    private static void DumplicateAnimationSingle()
    {
        Object[] objects = Selection.objects;
        string pathController = null;
        controllers = new List<UnityEditor.Animations.AnimatorController>();
        if (objects != null && objects.Length > 0)
        {
            string path = AssetDatabase.GetAssetPath(objects[0]);
            string[] paths = path.Split('/');
            pathController = path.Remove(path.IndexOf(paths[paths.Length - 1]));
            System.IO.DirectoryInfo rootDir = new System.IO.DirectoryInfo(pathController);
            System.IO.FileInfo[] files = null;
            files = rootDir.GetFiles("*.controller", System.IO.SearchOption.AllDirectories);
            if (files.Length <= 0 && !paths.Contains("Boss"))
            {
                string comControl = "Assets/GameModel/Template/A_CommonControl/common_tps_animctrl.controller";
                string newpath = pathController + "/" + paths[paths.Length - 2] + "_tps_animctrl.controller";
                AssetDatabase.CopyAsset(comControl, newpath);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            }
            files = rootDir.GetFiles("*.controller", System.IO.SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                //提取动作
                string thePath = files[i].FullName.Replace("\\", "/");
                thePath = thePath.Substring(thePath.IndexOf("Assets"));
                Object[] objectsLoad = AssetDatabase.LoadAllAssetsAtPath(thePath);
                for (int j = 0; j < objectsLoad.Length; j++)
                {
                    if (objectsLoad[j] is UnityEditor.Animations.AnimatorController)
                    {
                        controllers.Add(objectsLoad[j] as UnityEditor.Animations.AnimatorController);
                    }
                }
            }
            var dirName = Path.GetDirectoryName(path);
            var fbxs = AssetDatabase.FindAssets("t:GameObject", new string[] { dirName.Replace('\\', '/') });
            string prefabPath = string.Empty;
            GameObject prefab = null;
            if (fbxs.Length > 0)
            {
                foreach (var fbx in fbxs)
                {
                    var fbxPath = AssetDatabase.GUIDToAssetPath(fbx);
                    if (fbxPath.IndexOf("@") == -1 && fbxPath.EndsWith(".fbx", StringComparison.CurrentCultureIgnoreCase))
                    {
                        prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
                        prefabPath = fbxPath;
                        break;
                    }
                }
            }
            if (prefab == null)
            {
                var wizard = ScriptableWizard.DisplayWizard<DumplicateAnimationWizard>("选择模型文件");

                wizard.m_selectedObjects = objects;
            }
            else
            {
                foreach (var item0 in objects)
                {
                    string assetPath = AssetDatabase.GetAssetPath(item0);
                    Object[] assetObjects = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                    foreach (var item in assetObjects)
                    {
                        ModelTools.ProcessAnimClip(item, prefabPath, path);
                    }
                }
            }
        }
        //foreach (var itemO in objects)
        //{
            
        //}
    }

    //[MenuItem("Assets/模型处理功能/生成怪物模型预制体")]
    private static void CreateMonsterPrefab()
    {
        Object[] objects = Selection.objects;
        if (objects != null && objects.Length > 0)
        {
            CreateMonsterPrefab(objects[0] as GameObject);
        }
    }

    public static string CreateMonsterPrefab(GameObject prefab, string prefabName = "")
    {
        GameObject cameraGo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Camera/MainCamera_SkyBox.prefab");
        var camera = cameraGo.GetComponent<Camera>();
        string path = AssetDatabase.GetAssetPath(prefab);
        string assetPath = Path.GetDirectoryName(path);
        string[] paths = path.Split('/');
        var fileName = paths[paths.Length - 1];
        var modelName = prefabName == string.Empty ? prefab.name : prefabName; //paths[paths.Length - 2];
        var pathController = path.Remove(path.IndexOf(paths[paths.Length - 1]));
        System.IO.DirectoryInfo rootDir = new System.IO.DirectoryInfo(pathController);
        if (!fileName.Contains("@") || fileName.Contains("skin"))
        {
            var go = GameObject.Instantiate(prefab);

            var animator = go.GetComponent<Animator>();
            if(animator == null)
            {
                animator = go.AddComponent<Animator>();
            }
            if (animator != null)
            {
                var files = rootDir.GetFiles("*.controller", System.IO.SearchOption.AllDirectories);
                if (files.Length > 0)
                {
                    string thePath = files[0].FullName.Replace("\\", "/");
                    thePath = thePath.Substring(thePath.IndexOf("Assets"));
                    Object[] objectsLoad = AssetDatabase.LoadAllAssetsAtPath(thePath);
                    for (int j = 0; j < objectsLoad.Length; j++)
                    {
                        if (objectsLoad[j] is UnityEditor.Animations.AnimatorController)
                        {
                            animator.runtimeAnimatorController = objectsLoad[j] as UnityEditor.Animations.AnimatorController;
                            break;
                        }
                    }
                }
            }


            var materialDir = rootDir + "Materials";
            if (!Directory.Exists(materialDir))
            {
                Directory.CreateDirectory(materialDir);
            }
            Material useMaterial = null;
            System.IO.DirectoryInfo materidDirectoryInfo = new System.IO.DirectoryInfo(materialDir);
            var allAssets = materidDirectoryInfo.GetFiles("*.mat", System.IO.SearchOption.AllDirectories);
            for (int i = 0; i < allAssets.Length; i++)
            {
                var file = allAssets[i];
                if (!file.Name.Contains("_gpu"))
                {
                    string thePath = file.FullName.Replace("\\", "/");
                    thePath = thePath.Substring(thePath.IndexOf("Assets"));
                    useMaterial = AssetDatabase.LoadAssetAtPath<Material>(thePath);
                    if (useMaterial != null)
                    {
                        break;
                    }
                }
            }

            if (useMaterial == null)
            {
                string commonMat = "Assets/GameModel/Rogue/A_CommonControl/common_mat.mat";
                string commonGpuMat = "Assets/GameModel/Rogue/A_CommonControl/common_mat_gpu_anim.mat";
                string matpath = materialDir + "/" + modelName + "_mat1.mat";
                string matGpuPath;
                var files = rootDir.GetFiles("*.tga", System.IO.SearchOption.AllDirectories);
                for (int i = 0; i < files.Length; i++)
                {
                    var file = files[i];
                    if (file.Name.Contains("_d"))
                    {
                        string thePath = file.FullName.Replace("\\", "/");
                        thePath = thePath.Substring(thePath.IndexOf("Assets"));
                        Texture texture = AssetDatabase.LoadAssetAtPath<Texture>(thePath);
                        matpath = materialDir + "/" + texture.name + ".mat";
                        matGpuPath = materialDir + "/" + texture.name + "_gpu_anim.mat";
                        AssetDatabase.CopyAsset(commonMat, matpath);
                        AssetDatabase.CopyAsset(commonGpuMat, matGpuPath);
                        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                        var mat = AssetDatabase.LoadAssetAtPath<Material>(matpath);
                        var matGpu = AssetDatabase.LoadAssetAtPath<Material>(matGpuPath);
                        mat.SetTexture("_MainTex", texture);
                        matGpu.SetTexture("_MainTex", texture);
                        if (useMaterial == null)
                        {
                            useMaterial = mat;
                        }
                    }
                }

            }

            bool hasLod = false;
            int lodCount = 0;
            SkinnedMeshRenderer shadowRenderer = null;
            SkinnedMeshRenderer[] smrs = go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var smrArray = new SkinnedMeshRenderer[3];
            for(int i = 0; i < smrs.Length; i++)
            {
                if(smrs[i].name.EndsWith("high", StringComparison.CurrentCultureIgnoreCase))
                {
                    smrArray[0] = smrs[i];
                    hasLod = true;
                    lodCount++;
                }
                else if (smrs[i].name.EndsWith("mid", StringComparison.CurrentCultureIgnoreCase))
                {
                    smrArray[1] = smrs[i];
                    hasLod = true;
                    lodCount++;
                }
                else if(smrs[i].name.EndsWith("low", StringComparison.CurrentCultureIgnoreCase))
                {
                    smrArray[2] = smrs[i];
                    hasLod = true;
                    lodCount++;
                }
                else if(smrs[i].name.EndsWith("shadow", StringComparison.CurrentCultureIgnoreCase))
                {
                    smrs[i].shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                    shadowRenderer = smrs[i];
                }
                smrs[i].material = useMaterial;
            }
            if(shadowRenderer != null)
            {
                foreach(var smr in smrs)
                {
                    if(smr != shadowRenderer)
                    {
                        smr.shadowCastingMode = ShadowCastingMode.Off;
                    }
                }
            }

            if (hasLod)
            {
                var lodGroup = go.GetComponent<LODGroup>();
                if (lodGroup == null)
                {
                    lodGroup = go.AddComponent<LODGroup>();
                }
                
                var lods = new LOD[lodCount];
                int smrStart = 0;
                for(int i = 0; i < lodCount; i++)
                {
                    Renderer render = null;
                    for(int j = smrStart; j < smrArray.Length; j++)
                    {
                        if(smrArray[j] != null)
                        {
                            render = smrArray[j];
                            smrStart = j + 1;
                            break;
                        }
                    }
                    
                    lods[i] = new LOD()
                    {
                        renderers = new Renderer[] { render },
                        screenRelativeTransitionHeight = 1.0f - (float)i / (float)lodCount
                    };
                }
                lodGroup.SetLODs(lods);
                lodGroup.RecalculateBounds();

                lods[0].screenRelativeTransitionHeight = DistanceToRelativeHeight(camera, 4.33f, lodGroup.size);
                if(lods.Length > 1)
                    lods[1].screenRelativeTransitionHeight = DistanceToRelativeHeight(camera, 13.0f, lodGroup.size);
                if (lods.Length > 2)
                    lods[2].screenRelativeTransitionHeight = DistanceToRelativeHeight(camera, 100.0f, lodGroup.size);
                lodGroup.SetLODs(lods);
            }

            // var poolItem = go.AddComponent<GoPoolBehaviourItem>();
            // poolItem.EditorRefresh();
            var newpath = assetPath + "/" + modelName + "_tps.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, newpath, out bool success);
            GameObject.DestroyImmediate(go);
            return newpath;
        }
        return string.Empty;
    }

    public static float DistanceToRelativeHeight(Camera camera, float distance, float size)
    {
        if (camera.orthographic)
            return size * 0.5F / camera.orthographicSize;

        var halfAngle = Mathf.Tan(Mathf.Deg2Rad * camera.fieldOfView * 0.5F);
        var relativeHeight = size * 0.5F / (distance * halfAngle);
        return relativeHeight;
    }

    //[MenuItem("Assets/模型处理功能/给现有状态机添加参数")]
    private static void AddState()
    {
        selectAnimatorController = Selection.activeObject as AnimatorController;

        if (selectAnimatorController != null)
        {
            var lastSelectFile = PlayerPrefs.GetString("LastTemplateAnimatorController");
            var file = "Assets" + EditorUtility.OpenFilePanel("选择动画状态机", lastSelectFile, "controller").Substring(Application.dataPath.Length);

            AnimatorController template = AssetDatabase.LoadAssetAtPath<AnimatorController>(file);
            if (template != null)
            {
                PlayerPrefs.SetString("LastTemplateAnimatorController", file);
                var currentParams = selectAnimatorController.parameters;
                for (int i = 0; i < template.parameters.Length; i++)
                {
                    var p1 = template.parameters[i];
                    bool isExist = false;
                    for (int j = 0; j < currentParams.Length; j++)
                    {
                        var p2 = currentParams[j];
                        if (p1.name == p2.name && p1.type == p2.type)
                        {
                            isExist = true;
                            break;
                        }
                    }
                    if (!isExist)
                    {
                        selectAnimatorController.AddParameter(p1.name, p1.type);
                    }
                }
            }

            //var run1 = GetAnState(selectAnimatorController, "run1");
            //var run = GetAnState(selectAnimatorController, "run");
            //var trip = GetAnState(selectAnimatorController, "trip");
            //var idle = GetAnState(selectAnimatorController, "idle");
            //var birth = GetAnState(selectAnimatorController, "birth");


            //var firstLayersMachine = selectAnimatorController.layers[0].stateMachine;
            //var sprint = AddAnState(firstLayersMachine, "sprint", null, new Vector3(50, 50, 0));
            //if (run != null)
            //{
            //    SetACondition(firstLayersMachine, run, sprint, "run", AnimatorConditionMode.Equals, 4);
            //    SetACondition(firstLayersMachine, sprint, run, "run", AnimatorConditionMode.Equals, 1);
            //}

            //if (run1 != null)
            //{
            //    SetACondition(firstLayersMachine, run1, sprint, "run", AnimatorConditionMode.Equals, 4);
            //    SetACondition(firstLayersMachine, sprint, run1, "run", AnimatorConditionMode.Equals, 2);
            //}

            //if (idle != null)
            //{
            //    SetACondition(firstLayersMachine, idle, sprint, "run", AnimatorConditionMode.Equals, 4);
            //    SetACondition(firstLayersMachine, sprint, idle, "run", AnimatorConditionMode.Equals, 0);
            //}

            //if (sprint != null && birth != null)
            //{
            //    SetACondition(firstLayersMachine, birth, sprint, "run", AnimatorConditionMode.Equals, 4);
            //    SetACondition(firstLayersMachine, sprint, trip, "Trip", AnimatorConditionMode.If, 1);
            //}
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    //[MenuItem("Assets/模型处理功能/克隆预制体且替换材质")]
    private static void CloneModelAndReplaceMat()
    {
        Object[] objects = Selection.objects;

        foreach (var itemO in objects)
        {
            string path = AssetDatabase.GetAssetPath(itemO);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var gpuAnimRenderer = prefab.GetComponentInChildren<GpuAnimRenderer>();
            string matPath;
            string newAssetPath;
            int cnt = 1;

            if (gpuAnimRenderer != null)
            {
                matPath = AssetDatabase.GetAssetPath(gpuAnimRenderer.m_mat);
                var dir = Path.GetDirectoryName(matPath);
                dir = dir.Replace("\\", "/");
                System.IO.DirectoryInfo rootDir = new System.IO.DirectoryInfo(dir);
                var files = rootDir.GetFiles("*.mat", System.IO.SearchOption.AllDirectories);
                for (int i = 0; i < files.Length; i++)
                {
                    var file = files[i];
                    if (!file.Name.Contains("_gpu"))
                    {
                        string thePath = file.FullName.Replace("\\", "/");
                        thePath = thePath.Substring(thePath.IndexOf("Assets"));
                        Material mat = AssetDatabase.LoadAssetAtPath<Material>(thePath);
                        Material GpuMat = CreateGPUInstaceMat(mat);

                        var sourceGo = GameObject.Instantiate(prefab);

                        newAssetPath = Regex.Replace(path, @"\d", cnt.ToString());
                        //newAssetPath = path.Replace(".prefab", cnt + ".prefab");
                        var allAnimRenderer = sourceGo.GetComponentInChildren<GpuAnimRenderer>();
                        allAnimRenderer.m_mat = GpuMat;
                        PrefabUtility.SaveAsPrefabAsset(sourceGo, newAssetPath, out bool success);
                        GameObject.DestroyImmediate(sourceGo);
                        cnt++;
                    }
                }
                continue;
            }

            var allSkinMesh = prefab.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (allSkinMesh.Length > 0)
            {
                matPath = AssetDatabase.GetAssetPath(allSkinMesh[0].sharedMaterial);
                var dir = Path.GetDirectoryName(matPath);
                dir = dir.Replace("\\", "/");
                System.IO.DirectoryInfo rootDir = new System.IO.DirectoryInfo(dir);
                var files = rootDir.GetFiles("*.mat", System.IO.SearchOption.AllDirectories);
                for (int i = 0; i < files.Length; i++)
                {
                    var file = files[i];
                    if (!file.Name.Contains("_gpu"))
                    {
                        string thePath = file.FullName.Replace("\\", "/");
                        thePath = thePath.Substring(thePath.IndexOf("Assets"));
                        Material mat = AssetDatabase.LoadAssetAtPath<Material>(thePath);
                        var sourceGo = GameObject.Instantiate(prefab);
                        newAssetPath = Regex.Replace(path, @"\d", cnt.ToString());
                        var allskinMesh = sourceGo.GetComponentsInChildren<SkinnedMeshRenderer>();
                        for (int j = 0; j < allskinMesh.Length; j++)
                        {
                            allskinMesh[j].material = mat;
                        }
                        PrefabUtility.SaveAsPrefabAsset(sourceGo, newAssetPath, out bool success);
                        GameObject.DestroyImmediate(sourceGo);
                        cnt++;
                    }
                }

            }

        }

    }


    private static Material CreateGPUInstaceMat(Material material)
    {
        string assetPath = AssetDatabase.GetAssetPath(material);
        string fileName = Path.GetFileNameWithoutExtension(assetPath);
        string dir = Path.GetDirectoryName(assetPath);
        fileName = fileName + "_gpu_anim";
        string path = Path.Combine(dir, fileName + ".mat");
        if (File.Exists(path))
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            return mat;
        }
        Shader shader = Shader.Find("Dodjoy/Character/LightProbe/Normal/BRDF_MATCAP_GPUInst");
        Material retMat = new Material(material)
        {
            shader = shader
        };
        retMat.enableInstancing = true;
        retMat.name = fileName;
        DodEditorUtil.SaveAsset(retMat, path);
        retMat = AssetDatabase.LoadMainAssetAtPath(path) as Material;
        return retMat;
    }

    //[MenuItem("Assets/模型处理功能/一键生成gpu Skin材质")]
    private static void CreateGpuMat()
    {
        Object[] objects = Selection.objects;
        foreach (var itemO in objects)
        {
            string path = AssetDatabase.GetAssetPath(itemO);
            var dir = Path.GetDirectoryName(path);
            dir = dir.Replace("\\", "/");
            System.IO.DirectoryInfo rootDir = new System.IO.DirectoryInfo(dir);
            var files = rootDir.GetFiles("*.mat", System.IO.SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];
                if (!file.Name.Contains("_gpu"))
                {
                    string thePath = file.FullName.Replace("\\", "/");
                    thePath = thePath.Substring(thePath.IndexOf("Assets"));
                    Material mat = AssetDatabase.LoadAssetAtPath<Material>(thePath);
                    Material GpuMat = CreateGPUSkinMat(mat);
                }
            }
        }

    }

    private static Material CreateGPUSkinMat(Material material)
    {
        string assetPath = AssetDatabase.GetAssetPath(material);
        string fileName = Path.GetFileNameWithoutExtension(assetPath);
        string dir = Path.GetDirectoryName(assetPath);
        fileName = fileName + "_gpu";
        string path = Path.Combine(dir, fileName + ".mat");
        if (File.Exists(path))
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            return mat;
        }
        Shader shader = Shader.Find(material.shader.name + " GPU Skin");
        Material retMat = new Material(material)
        {
            shader = shader
        };
        retMat.name = fileName;
        DodEditorUtil.SaveAsset(retMat, path);
        retMat = AssetDatabase.LoadMainAssetAtPath(path) as Material;
        return retMat;
    }

    //[MenuItem("Assets/模型处理功能/复制hero Gpu Skin Model")]
    private static void CloneHeroModelAndReplaceMat()
    {
        Object[] objects = Selection.objects;

        foreach (var itemO in objects)
        {
            string path = AssetDatabase.GetAssetPath(itemO);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            string matPath;
            int cnt = 1;

            var allSkinMesh = prefab.GetComponentsInChildren<MeshRenderer>();
        
            for (int j = 2; j < 17; j++)
            {
                string index = "";
                if (j<10)
                {
                    index = "_00" + j ;
                }
                else
                {
                    index = "_0" + j;
                }
                var CopyGo = GameObject.Instantiate(prefab);
                var copyMesh = CopyGo.GetComponentsInChildren<MeshRenderer>();
                var name = prefab.name;
                name = Regex.Replace(name, @"_\d\d\d", index);
                CopyGo.name = name;
                bool haveChange = false;
                for (int i = 0; i < allSkinMesh.Length; i++)
                {
                    var curMesh = allSkinMesh[i];
                    var curMat = curMesh.sharedMaterial;
                    matPath = AssetDatabase.GetAssetPath(curMat);
                    matPath = Regex.Replace(matPath, @"_\d\d\d", index);
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if (mat!=null)
                    {
                        copyMesh[i].sharedMaterial = mat;
                        haveChange = true;
                    }
                }

                if (!haveChange)
                {
                    GameObject.DestroyImmediate(CopyGo);
                }
            }
        }

    }

    public static void ProcessAnimClip(Object item, string modelPath, string path)
    {
        string[] pathItems = path.Split('/');
        if (pathItems.Length <= 0)
        {
            Debug.LogError("丢你螺母1 pathItems.Length:" + pathItems.Length);
            return;
        }

        if (item is AnimationClip && !item.name.Contains("__preview__"))
        {
            string modelName = modelPath;
            string[] paths = path.Split('/');
            string pathMy = path.Remove(path.IndexOf(paths[paths.Length - 1])) + item.name + ".anim";

            string tmpDir = "Assets/GameModel/Tmp";
            string thePath = Path.GetDirectoryName(modelName).Replace("\\", "/");
            string tmpName = modelName.Replace(thePath, tmpDir);
            if (!Directory.Exists(tmpDir))
            {
                Directory.CreateDirectory(tmpDir);
            }
            if (!File.Exists(tmpName) && File.Exists(modelName))
            {
                AssetDatabase.CopyAsset(modelName, tmpName);
                ModelImporter modelImporter = AssetImporter.GetAtPath(tmpName) as ModelImporter;
                modelImporter.optimizeGameObjects = false;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            }
            Transform transform = (Transform)AssetDatabase.LoadAssetAtPath(tmpName, typeof(Transform));
            if (transform == null)
            {
                Debug.LogError("load model path is:" + path + " failed:" + modelName + "   temppath: " + tmpName);
            }

            AnimationClip clip = new AnimationClip();
            AnimationClip anim = item as AnimationClip;
            EditorUtility.CopySerialized(item, clip);
            if (m_needLoopAnim.Contains(item.name))
            {
                var animSetting = AnimationUtility.GetAnimationClipSettings(anim);
                animSetting.loopTime = true;
                AnimationUtility.SetAnimationClipSettings(clip, animSetting);
            }
            AssetDatabase.SaveAssets();
            //if (controllers != null)
            //{
            //    for (int i = 0; i < controllers.Count; i++)
            //    {
            //        ProcessAnimControl(controllers[i], clip);
            //    }
            //}
            if (selectAnimatorController != null)
            {
                ProcessAnimControl(selectAnimatorController, clip);
            }
            AssetDatabase.CreateAsset(clip, pathMy);
            AnimCompressor.RemoveAnimScaleCurves(clip, pathMy, transform);

            clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(pathMy);
            if (controllers != null)
            {
                for (int i = 0; i < controllers.Count; i++)
                {
                    var controler = controllers[i];
                    ProcessAnimControl(controler, clip);
                }
            }
        }

    }

    private static void ProcessBlendTree(BlendTree tree, AnimationClip anim)
    {
        var children = tree.children;
        for (int k = 0; k < children.Length; k++)
        {
            var child = children[k];
            if (child.motion is BlendTree childTree)
            {
                ProcessBlendTree(childTree, anim);
            }
            else
            {
                if(child.motion is AnimationClip && anim.name.IndexOf(child.motion.name) >0 )
                {
                    child.motion = anim;
                }
                children[k] = child;
            }
        }
        tree.children = children;
    }
    
    static void ProcessAnimControl(AnimatorStateMachine stateMachine, AnimationClip anim, ref bool changed)
    {
        var allStates = stateMachine.states;
        for (int j = 0; j < allStates.Length; j++)
        {
            var theState = allStates[j];

            Motion theMotion = theState.state.motion;
            if (theMotion is BlendTree btree)
            {
                ProcessBlendTree(btree, anim);
                continue;
            }

            if (theState.state.name != "null")
            {
                List<string> m_listMotionName;
                if (m_monsterAnimState.TryGetValue(theState.state.name, out m_listMotionName))
                {
                    var animName = anim.name;
                    if (m_listMotionName.Contains(anim.name))
                    {
                        theState.state.motion = anim;
                        changed = true;
                    }
                    else
                    {
                        var index = animName.IndexOf('_');
                        if (index >= 0)
                        {
                            var name = animName.Substring(index + 1);
                            if (m_listMotionName.Contains(name))
                            {
                                theState.state.motion = anim;
                                changed = true;
                            }
                        }
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(theState.state.name) && theState.state.name != "Null")
                    {
                        Debug.LogError("记录状态与动画关系的字典需完善" + theState.state.name);
                    }
                }

            }

        }

        stateMachine.states = allStates;
        foreach (var sm in stateMachine.stateMachines)
        {
            ProcessAnimControl(sm.stateMachine, anim, ref changed);
        }
    }

    private static void ProcessAnimControl(UnityEditor.Animations.AnimatorController theController, AnimationClip anim)
    {
        UnityEditor.Animations.AnimatorStateMachine stateMachine = null;
        if (theController != null)
        {
            var layers = theController.layers;
            var changed = false;
            for (int i = 0; i < layers.Length; i++)
            {
                stateMachine = layers[i].stateMachine;
                ProcessAnimControl(stateMachine, anim, ref changed);
            }

            if (!changed)
            {
                Debug.LogError(anim.name + "动画没有匹配的State!!");
            }
        }
    }



    ///补全所有的动画变量
    //[MenuItem("Assets/模型处理功能/初始化动画状态机参数")]
    private static void PrepareAnimParam()
    {
        var animCtrlList = Selection.GetFiltered(typeof(UnityEditor.Animations.AnimatorController), SelectionMode.Assets | SelectionMode.DeepAssets);
        for (int i = 0; i < animCtrlList.Length; i++)
        {
            var animCtrl = animCtrlList[i] as UnityEditor.Animations.AnimatorController;
            string assetPath = AssetDatabase.GetAssetPath(animCtrl);
            bool modify = CheckAndInitAnimParam(animCtrl);
            if (!modify)
            {
                Debug.Log(string.Format("animation {0} pass", assetPath), animCtrl);
            }
            else
            {
                EditorUtility.SetDirty(animCtrl);
            }
        }

        AssetDatabase.Refresh(ImportAssetOptions.Default);
        AssetDatabase.SaveAssets();
    }

    ///补全所有的动画变量
    //[MenuItem("Assets/模型处理功能/检查collider的参数")]
    private static void PrepareColliderParam()
    {
        int colliderCnt = 0;

        var colliderList = Selection.GetFiltered(typeof(Object), SelectionMode.TopLevel | SelectionMode.DeepAssets);
        for (int i = 0; i < colliderList.Length; i++)
        {
            var go = colliderList[i] as GameObject;
            if (go == null)
            {
                continue;
            }

            var prefabType = PrefabUtility.GetPrefabType(go);
            if (prefabType != PrefabType.Prefab)
            {
                continue;
            }

            var goInst = PrefabUtility.InstantiatePrefab(go) as GameObject;
            var colliderCmptList = goInst.GetComponentsInChildren<Collider>();
            bool changed = false;
            if (colliderCmptList != null)
            {
                for (int j = 0; j < colliderCmptList.Length; j++)
                {
                    var collider = colliderCmptList[j];
                    if (CheckColliderParam(collider as Collider))
                    {
                        changed = true;
                    }

                    colliderCnt++;
                }
            }

            string assetPath = AssetDatabase.GetAssetPath(go);
            if (changed)
            {
                Debug.LogError(assetPath + " changed", go);

                PrefabUtility.ReplacePrefab(goInst, go);
                EditorUtility.SetDirty(go);
            }
            else
            {
                Debug.Log(assetPath + " passed", go);
            }

            DestroyImmediate(goInst);
        }

        AssetDatabase.Refresh(ImportAssetOptions.Default);
        AssetDatabase.SaveAssets();
        Debug.LogError("check finished, check gameobject count: " + colliderCnt);
    }

    private static bool CheckColliderParam(Collider collider)
    {
        bool changed = false;
        var rigidBody = collider.gameObject.GetComponent<Rigidbody>();
        if (rigidBody == null)
        {
            rigidBody = collider.gameObject.AddComponent<Rigidbody>();
            rigidBody.isKinematic = true;
            rigidBody.constraints = RigidbodyConstraints.FreezeAll;
            changed = true;
        }
        else if (!rigidBody.isKinematic || rigidBody.constraints != RigidbodyConstraints.FreezeAll)
        {
            rigidBody.isKinematic = true;
            rigidBody.constraints = RigidbodyConstraints.FreezeAll;
            changed = true;
        }

        return changed;
    }

    //[MenuItem("Assets/模型处理功能/GPU怪物移除Rigidbody")]
    private static void RemoveRigidbody()
    {
        bool changed = false;
        var assetPaths = DodEditorTools.GetSelectedAssetPaths("*.prefab");
        foreach (var assetPath in assetPaths)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            var cmptList = prefab.GetComponentsInChildren<Rigidbody>(true);
            if (cmptList.Length > 0)
            {
                foreach (var rigidbody in cmptList)
                {
                    DestroyImmediate(rigidbody, true);
                }
                PrefabUtility.SavePrefabAsset(prefab);
                EditorUtility.SetDirty(prefab);
                changed = true;
                Debug.Log("移除Rigidbody：" + assetPath);
            }
        }
        if (changed)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    //[MenuItem("Assets/模型处理功能/GPU怪物移除骨骼")]
    public static void RemoveBone()
    {
        bool changed = false;
        var assetPaths = DodEditorTools.GetSelectedAssetPaths("*.prefab");
        foreach (var assetPath in assetPaths)
        {
            RemoveBone(assetPath);
        }
        if (changed)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    public static bool RemoveBone(string assetPath)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        var gpuAnimRenderer = prefab.GetComponentInChildren<GpuAnimRenderer>();
        if (gpuAnimRenderer == null)
        {
            return false;
        }
        bool deletBone = false;
        var tpsConfig = prefab.GetComponent<TpsConfig>();
        if (tpsConfig != null)
        {
            DestroyImmediate(tpsConfig, true);
        }
        List<Transform> toFindList = new List<Transform>();
        List<string> listDmPoint = new List<string>();
        for (int i = 0; i < prefab.transform.childCount; i++)
        {
            var childTrans = prefab.transform.GetChild(i);
            toFindList.Add(childTrans);
        }

        for (int index = 0; index < toFindList.Count; index++)
        {
            var currTrans = toFindList[index];
            if (currTrans.name.Contains("DM"))
            {
                listDmPoint.Add(currTrans.name);
            }
            for (int k = 0; k < currTrans.childCount; k++)
            {
                toFindList.Add(currTrans.GetChild(k));
            }
        }

        for (int i = 0; i < prefab.transform.childCount; i++)
        {
            var child = prefab.transform.GetChild(i);
            var renderer = child.GetComponent<Renderer>();
            if (renderer == null)
            {
                deletBone = true;
                DestroyImmediate(child.gameObject, true);
                i--;
            }
        }
        if (deletBone)
        {
            var sourceGo = GameObject.Instantiate(prefab);
            for (int i = 0; i < listDmPoint.Count; i++)
            {
                var name = listDmPoint[i];
                GameObject go = new GameObject(name);
                go.transform.SetParent(sourceGo.transform);
                go.transform.localPosition = Vector3.zero;
            }
            PrefabUtility.SaveAsPrefabAsset(sourceGo, assetPath, out bool success);
            GameObject.DestroyImmediate(sourceGo);
        }
        return true;
    }

    [MenuItem("Assets/模型处理功能/关闭阴影接收")]
    private static void CloseReceiveShadow()
    {
        bool changed = true;
        var assetPaths = DodEditorTools.GetSelectedAssetPaths("*.prefab");
        foreach (var assetPath in assetPaths)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            var allMesh = prefab.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < allMesh.Length; i++)
            {
                var mesh = allMesh[i];
                mesh.receiveShadows = false;
            }
        }
        if (changed)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    //[MenuItem("Assets/模型处理功能/Lod参数设置")]
    //private static void SetLodParam()
    //{
    //    bool changed = false;
    //    var assetPaths = DodEditorTools.GetSelectedAssetPaths("*.prefab");
    //    foreach (var assetPath in assetPaths)
    //    {
    //        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
    //        var lodGroup = prefab.GetComponentInChildren<LODGroup>();
    //        if (lodGroup == null)
    //        {
    //            continue;
    //        }

    //        var lods = lodGroup.GetLODs();
    //        if (lodGroup.lodCount==3)
    //        {
    //            lods[0].screenRelativeTransitionHeight = 0.53f;
    //            lods[1].screenRelativeTransitionHeight = 0.2f;
    //            lods[2].screenRelativeTransitionHeight = 0.03f;
    //            changed = true;
    //        }

    //        if (lodGroup.lodCount == 2)
    //        {
    //            lods[0].screenRelativeTransitionHeight = 0.53f;
    //            lods[1].screenRelativeTransitionHeight = 0.03f;
    //            changed = true;
    //        }

    //        if (changed)
    //        {
    //            lodGroup.SetLODs(lods);
    //        }
    //    }
    //    if (changed)
    //    {
    //        AssetDatabase.SaveAssets();
    //        AssetDatabase.Refresh();
    //    }

    //}


    private struct AnimParamConfig
    {
        public string m_name;
        public AnimatorControllerParameterType m_type;

        public AnimParamConfig(string name, AnimatorControllerParameterType type)
        {
            m_name = name;
            m_type = type;
        }
    }

    private static List<AnimParamConfig> GetAnimParamConfigList()
    {
        List<AnimParamConfig> list = new List<AnimParamConfig>();
        list.Add(new AnimParamConfig("shoot", AnimatorControllerParameterType.Int));
        list.Add(new AnimParamConfig("sametrigger", AnimatorControllerParameterType.Trigger));
        list.Add(new AnimParamConfig("ground", AnimatorControllerParameterType.Bool));
        list.Add(new AnimParamConfig("dir_forward", AnimatorControllerParameterType.Float));
        list.Add(new AnimParamConfig("dir_left", AnimatorControllerParameterType.Float));
        list.Add(new AnimParamConfig("dir_up", AnimatorControllerParameterType.Float));
        list.Add(new AnimParamConfig("fly", AnimatorControllerParameterType.Bool));
        list.Add(new AnimParamConfig("turn_dir", AnimatorControllerParameterType.Int));
        list.Add(new AnimParamConfig("run", AnimatorControllerParameterType.Int));
        list.Add(new AnimParamConfig("reload", AnimatorControllerParameterType.Int));
        list.Add(new AnimParamConfig("ModeID", AnimatorControllerParameterType.Int));
        list.Add(new AnimParamConfig("SkillFlying", AnimatorControllerParameterType.Bool));
        list.Add(new AnimParamConfig("SetMode", AnimatorControllerParameterType.Int));
        list.Add(new AnimParamConfig("Capcitying", AnimatorControllerParameterType.Bool));
        list.Add(new AnimParamConfig("Hurt", AnimatorControllerParameterType.Trigger));
        list.Add(new AnimParamConfig("SmallStun", AnimatorControllerParameterType.Bool));
        list.Add(new AnimParamConfig("BeCatch", AnimatorControllerParameterType.Bool));
        list.Add(new AnimParamConfig("Relaxing", AnimatorControllerParameterType.Bool));
        list.Add(new AnimParamConfig("IsWalk", AnimatorControllerParameterType.Float));
        list.Add(new AnimParamConfig("death", AnimatorControllerParameterType.Bool));
        list.Add(new AnimParamConfig("open", AnimatorControllerParameterType.Int));
        list.Add(new AnimParamConfig("walk", AnimatorControllerParameterType.Int));
        list.Add(new AnimParamConfig("social", AnimatorControllerParameterType.Int));
        list.Add(new AnimParamConfig("giddy", AnimatorControllerParameterType.Int));
        list.Add(new AnimParamConfig("switch", AnimatorControllerParameterType.Int));
        list.Add(new AnimParamConfig("jump", AnimatorControllerParameterType.Int));
        list.Add(new AnimParamConfig("collect", AnimatorControllerParameterType.Int));
        list.Add(new AnimParamConfig("open", AnimatorControllerParameterType.Int));

        ///这个只有吃鸡出生角色才需要，其他不需要
        ///list.Add(new AnimParamConfig("JumpRot", AnimatorControllerParameterType.Float));

        return list;
    }

    public static bool CheckAndInitAnimParam(UnityEditor.Animations.AnimatorController ctrl)
    {
        bool modify = false;
        string assetPath = AssetDatabase.GetAssetPath(ctrl);
        List<AnimParamConfig> listConfig = GetAnimParamConfigList();
        for (int i = 0; i < listConfig.Count; i++)
        {
            var item = listConfig[i];
            if (IsExistAnimParam(ctrl, item))
            {
                continue;
            }

            ctrl.AddParameter(item.m_name, item.m_type);
            Debug.LogError(string.Format("[{0} ]Add animation param: {1}, type[{2}]", assetPath, item.m_name, item.m_type), ctrl);
            modify = true;
        }

        return modify;
    }

    private static bool IsExistAnimParam(UnityEditor.Animations.AnimatorController animCtr, AnimParamConfig config)
    {
        var allParams = animCtr.parameters;
        for (int i = 0; i < allParams.Length; i++)
        {
            var animParma = allParams[i];
            if (animParma.name.Equals(config.m_name))
            {
                if (animParma.type != config.m_type)
                {
                    throw new Exception(
                        string.Format("anmator parma type not match: {0}, {1}-{2}",
                        config.m_name, animParma.type, config.m_type));
                }

                return true;
            }
        }

        return false;
    }

    ///补全所有的动画变量
    //[MenuItem("Assets/模型处理功能/Copy TPS贴图")]
    private static void ProcessTpsTexture()
    {
        var texList = Selection.GetFiltered(typeof(Texture2D), SelectionMode.Assets);
        for (int i = 0; i < texList.Length; i++)
        {
            var tex = texList[i] as Texture2D;
            if (tex == null)
            {
                continue;
            }

            string assetPath = AssetDatabase.GetAssetPath(tex);
            Debug.LogError(assetPath, tex);

            CopyTpsTexture(tex);
        }

        AssetDatabase.Refresh();
    }

    private static void CopyTpsTexture(Texture2D tex)
    {
        string assetPath = AssetDatabase.GetAssetPath(tex);
        string newPath = Path.GetDirectoryName(assetPath) + "/" + Path.GetFileNameWithoutExtension(assetPath) + "_tps" +
                         Path.GetExtension(assetPath);

        int minTexSize = 256;
        if (newPath.Contains("diffuse"))
        {
            minTexSize = 512;
        }

        Debug.LogError("dest path: " + newPath + string.Format("width: {0}, height:{1}", tex.width, tex.height));

        var srcImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;

        ///关闭src的mipmap
        if (srcImporter.mipmapEnabled)
        {
            srcImporter.mipmapEnabled = false;
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }

        if (AssetDatabase.LoadAssetAtPath(newPath, typeof(Texture2D)) != null)
        {
            string rootDir = Application.dataPath + "/../";

            File.Copy(rootDir + assetPath, rootDir + newPath, true);
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(newPath, ImportAssetOptions.ForceUpdate);
        }
        else
        {
            AssetDatabase.CopyAsset(assetPath, newPath);
        }

        var dstImport = AssetImporter.GetAtPath(newPath) as TextureImporter;
        dstImport.mipmapEnabled = true;
        int newSize = srcImporter.maxTextureSize / 2;
        if (newSize >= minTexSize)
        {
            dstImport.maxTextureSize = newSize;
        }

        dstImport.textureFormat = srcImporter.textureFormat;
        AssetDatabase.Refresh();
        AssetDatabase.ImportAsset(newPath, ImportAssetOptions.ForceUpdate);
    }


    [MenuItem("SceneTool/录像工具/转换人物模型")]
    public static void PrepareVideoModel()
    {
        string[] dirs = new[] { "/Resources/Actor/Player/Tps", "/Resources/Actor/Player/Death" };
        DoProcessVideoModel(dirs);
    }

    private static void DoProcessVideoModel(string[] targetDirs)
    {
        Shader videoTpsShader = Shader.Find("Dodjoy/Character/BRDF_LightProbe");
        string showPrefabPath = "/Resources/Actor/Player/Show";
        var dirs = new[]
        {
            "/Prefabs/Actor/Player/Tps",
            "/Prefabs/Actor/Player/Death"
        };

        var fileList = new List<string>();
        for (int i = 0; i < dirs.Length; ++i)
        {
            string filePath = Application.dataPath + dirs[i];
            fileList =
                Directory.GetFiles(filePath, "*.*", SearchOption.AllDirectories)
                    .Where(file => file.ToLower().EndsWith("prefab"))
                    .ToList();
            int count = fileList.Count;
            if (count == 0)
            {
                Debug.LogError(dirs[i] + "读取失败");
                continue;
            }
            bool isDeath = filePath.Contains("Death");
            for (int j = 0; j < count; ++j)
            {
                EditorUtility.DisplayProgressBar("处理模型中", string.Format("进度{0}/{1}", j + 1, count),
                    j / (float)count);

                //if (!CheckFilter(fileList[j]))
                //{
                //    continue;
                //}
                filePath = fileList[j].Substring(fileList[j].IndexOf("Assets", StringComparison.Ordinal));
                string targetPath = filePath.Replace(".prefab", "_gpu.prefab");
                targetPath = targetPath.Replace(dirs[i], targetDirs[i]);

                Object goTargetObj = AssetDatabase.LoadMainAssetAtPath(targetPath);
                if (goTargetObj == null)
                {
                    Debug.LogError(targetPath + "读取失败");
                    continue;
                }

                string showPath = filePath.Replace(dirs[i], showPrefabPath);
                if (isDeath)
                {
                    showPath = showPath.Substring(0, showPath.IndexOf("_death", StringComparison.Ordinal));
                }
                else if (filePath.Contains("_fps"))
                {
                    //特殊处理大骑士的情况
                    showPath = showPath.Substring(0, showPath.IndexOf("_fps", StringComparison.Ordinal));
                }
                else
                {
                    showPath = showPath.Substring(0, showPath.IndexOf("_tps", StringComparison.Ordinal));
                }
                showPath += "_tps_s.prefab";
                Object goShowObj = AssetDatabase.LoadMainAssetAtPath(showPath);
                if (goShowObj == null)
                {
                    Debug.LogError(showPath + "读取失败");
                    continue;
                }

                GameObject goVideoPrefab = (GameObject)PrefabUtility.InstantiatePrefab(goShowObj);
                GameObject targetGo = (GameObject)goTargetObj;
                Material targetMaterial = GetGPUMeshMaterial(targetGo);
                if (targetMaterial == null)
                {
                    Debug.LogError(targetPath + "材质读取失败");
                    continue;
                }

                var useMaterials = GetUseMaterials(goVideoPrefab);
                foreach (var kv in useMaterials)
                {
                    var material = kv.Key;
                    string materialPath = AssetDatabase.GetAssetPath(material);
                    string videoMaterialPath;
                    if (!materialPath.EndsWith("_video.mat"))
                    {
                        videoMaterialPath = materialPath.Replace(".mat", "_video.mat");
                    }
                    else
                    {
                        videoMaterialPath = materialPath;
                    }
                    Material videoMaterial = null;
                    if (!File.Exists(videoMaterialPath))
                    {
                        videoMaterial = new Material(material) { shader = videoTpsShader };
                        videoMaterial.SetTexture("_BRDFTex", targetMaterial.GetTexture("_BRDFTex"));
                        videoMaterial.SetFloat("_SpecScale", targetMaterial.GetFloat("_SpecScale"));
                        videoMaterial.SetFloat("_EmissionScale", targetMaterial.GetFloat("_EmissionScale"));
                        videoMaterial.SetFloat("_LightProbeScale", targetMaterial.GetFloat("_LightProbeScale"));
                        AssetDatabase.CreateAsset(videoMaterial, videoMaterialPath);
                    }
                    else
                    {
                        videoMaterial = (Material)AssetDatabase.LoadMainAssetAtPath(videoMaterialPath);
                    }

                    if (videoMaterial == null)
                    {
                        Debug.LogError(string.Format("load video mat:{0} failed!!!", videoMaterialPath));
                        continue;
                    }

                    var renderers = kv.Value;
                    foreach (var renderer in renderers)
                    {
                        renderer.sharedMaterial = videoMaterial;
                        renderer.useLightProbes = true;
                        renderer.receiveShadows = false;
                        renderer.castShadows = true;
                    }
                }

                Animator animator = goVideoPrefab.GetComponent<Animator>();
                if (isDeath)
                {
                    GameObject.DestroyImmediate(animator);
                }
                else
                {
                    Animator targetAnimator = targetGo.GetComponent<Animator>();
                    animator.runtimeAnimatorController = targetAnimator.runtimeAnimatorController;
                    animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                }

                ShaderShowConfig showConfig = goVideoPrefab.GetComponent<ShaderShowConfig>();
                if (showConfig != null)
                {
                    GameObject.DestroyImmediate(showConfig);
                }
                //移除动态骨骼
                DynamicBone[] bones = goVideoPrefab.GetComponents<DynamicBone>();
                foreach (var bone in bones)
                {
                    GameObject.DestroyImmediate(bone);
                }

                TpsActorCloner.CloneObject(targetGo, goVideoPrefab, false);
                //移除lod
                LODGroup lod = goVideoPrefab.GetComponent<LODGroup>();
                if (lod)
                {
                    GameObject.DestroyImmediate(lod);
                }

                PrefabUtility.ReplacePrefab(goVideoPrefab, goTargetObj);
                GameObject.DestroyImmediate(goVideoPrefab);
                AssetDatabase.SaveAssets();
            }
        }
        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
    }

    private static bool CheckFilter(string name)
    {
        var filter = new[]
        {
            "aganboshi",
            "qianshou",
            "shibing9527",
            "duwangshidifen",
            "shashoumofei"
        };
        if (filter.Length == 0)
        {
            return true;
        }
        for (int i = 0; i < filter.Length; ++i)
        {
            if (name.Contains(filter[i]))
            {
                return true;
            }
        }
        return false;
    }

    private static Dictionary<Material, List<Renderer>> GetUseMaterials(GameObject gameObject)
    {
        Dictionary<Material, List<Renderer>> ret = new Dictionary<Material, List<Renderer>>();

        if (gameObject != null)
        {
            Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                Material sharedMaterial = renderer.sharedMaterial;
                if (!sharedMaterial.shader.name.Contains("BRDF"))
                {
                    continue;
                }
                List<Renderer> listRenderers;
                if (!ret.TryGetValue(sharedMaterial, out listRenderers))
                {
                    listRenderers = new List<Renderer>();
                    ret.Add(sharedMaterial, listRenderers);
                }
                listRenderers.Add(renderer);
            }
        }
        return ret;
    }

    private static Material GetGPUMeshMaterial(GameObject gameObject)
    {
        Material sharedMaterial = null;
        if (gameObject != null)
        {
            var renders = gameObject.GetComponentsInChildren<MeshRenderer>(true);
            if (renders.Length != 0)
            {
                foreach (var render in renders)
                {
                    if (render.sharedMaterial == null)
                    {
                        continue;
                    }
                    var material = render.sharedMaterial;
                    if (!material.shader.name.Contains("BRDF"))
                    {
                        continue;
                    }
                    sharedMaterial = material;
                    break;
                }
            }
            else
            {
                var skinRenders = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                foreach (var render in skinRenders)
                {
                    if (render.sharedMaterial == null)
                    {
                        continue;
                    }
                    var material = render.sharedMaterial;
                    if (!material.shader.name.Contains("BRDF"))
                    {
                        continue;
                    }
                    sharedMaterial = material;
                    break;
                }
            }
        }
        return sharedMaterial;
    }

    #region 状态机状态添加相关接口

    static List<AnimatorState> NotChangeStates = new List<AnimatorState>();
    private static bool _isAddBlend;

    //设置动作控制器状态转换的变量类型
    private static void AddParameter(AnimatorController ac, string paraName,
        AnimatorControllerParameterType acParameterType)
    {
        //判断是否已存在，已存在则跳过，不存在则创建
        for (int i = 0; i < ac.parameters.Length; i++)
        {
            var para = ac.parameters[i];
            if (para.name == paraName)
            {
                return;
            }
        }

        ac.AddParameter(paraName, acParameterType);
    }

    //通过片段名获取片段
    private static AnimationClip GetAnimClipByName(Dictionary<string, AnimationClip> animationClips, string clipName)
    {
        AnimationClip ac = null;
        animationClips.TryGetValue(clipName, out ac);

        return ac;
    }

    /// 添加一个状态
    /// </summary>
    /// <param name="stateMachine">状态机对象</param>
    /// <param name="stateName">状态名字</param>
    /// <param name="theClip">动画文件</param>
    static AnimatorState AddAnState(
        AnimatorStateMachine stateMachine,
        string stateName,
        AnimationClip theClip,
        Vector3 pos,
        AnimationClip defaultClip = null,
        AnimationClip defaultClip2 = null
    )
    {
        //判断是否已存在
        AnimatorState theState = null;
        for (int i = 0; i < stateMachine.states.Length; i++)
        {
            AnimatorState state = stateMachine.states[i].state;
            if (state.name == stateName)
            {
                //判断是否是可覆盖状态
                if (IsOverwriteState(stateName))
                {
                    //已存在则清空Transition
                    theState = state;
                    ClearState(stateMachine, theState);
                }
                else
                {
                    stateMachine.states[i].position = pos;
                    //已存在则使用
                    theState = state;
                    //NotChangeStates.Add(state);
                }
                break;
            }
        }

        if (theState == null)
        {
            //不存在则创建
            theState = stateMachine.AddState(stateName);
        }

        if (theClip != null)
        {
            theState.motion = theClip;
        }
        else
        {
            if (defaultClip != null)
            {
                theState.motion = defaultClip;
            }
            else
            {
                theState.motion = defaultClip2;
            }
        }
        return theState;
    }

    static AnimatorState GetAnState(
        AnimatorController animatorController,
        string stateName
    )
    {
        //判断是否已存在
        AnimatorState theState = null;
        for (int i = 0; i < animatorController.layers.Length; i++)
        {
            var stateMachine = animatorController.layers[i].stateMachine;
            for (int j = 0; j < stateMachine.states.Length; j++)
            {
                AnimatorState state = stateMachine.states[j].state;
                if (state.name == stateName)
                {
                    //已存在则使用
                    theState = state;
                    break;
                }
            }
        }

        return theState;
    }

    //判断是否是可覆盖状态
    private static bool IsOverwriteState(string stateName)
    {
        //bool isOverwrite;
        //switch (stateName)
        //{
        //    case "Idle":
        //    case "Move":
        //    case "Impact_HitRecovery":
        //    case "DeathNormal":
        //        isOverwrite = true;
        //        break;
        //    default:
        //        isOverwrite = false;
        //        break;
        //}

        return false;
    }

    /// <summary>
    /// 清空一个状态作为From的所有Transition
    /// </summary>
    /// <param name="stateMachine"></param>
    /// <param name="state"></param>
    /// <returns></returns>
    private static void ClearState(AnimatorStateMachine stateMachine, AnimatorState state)
    {
        AnimatorStateTransition[] fromTranss = state.transitions;
        foreach (var trans in fromTranss)
        {
            state.RemoveTransition(trans);
            DestroyImmediate(trans);
        }
    }

    static AnimatorStateTransition SetACondition(AnimatorStateMachine stateMachine,
        AnimatorState from,
        AnimatorState to,
        string conditionFeal,
        AnimatorConditionMode mode,
        int threshole = 0
    )
    {
        if (NotChangeStates.Contains(from))
        {
            return null;
        }

        var transition = from.AddTransition(to);

        var len = SetAddBlend(from, to);

        SetDefaultTransition(transition, len);
        transition.AddCondition(mode, threshole, conditionFeal);
        return transition;
    }

    //设置添加融合
    private static float SetAddBlend(AnimatorState from, AnimatorState to)
    {
        if (from == null || to == null || from.motion == null || to.motion == null)
        {
            return 0f;
        }

        var fromName = from.motion.name;
        var toName = to.motion.name;
        var len = 0f;

        if (fromName == "run" && toName == "idle")
        {
            len = 0.2f;
            _isAddBlend = true;
        }
        if (fromName == "caiji" && toName == "idle")
        {
            len = 0.1f;
            _isAddBlend = true;
        }
        return len;
    }

    //设置默认的转换状态
    private static void SetDefaultTransition(AnimatorStateTransition transition, float len)
    {
        transition.duration = 0f;
        transition.offset = 0f;
        if (_isAddBlend)
        {
            transition.duration = len;
            _isAddBlend = false;
        }
    }

    /// <summary>
    /// 增加一个状态转换条件
    /// </summary>
    /// <param name="stateMachine">状态机对象</param>
    /// <param name="from">源头状态</param>
    /// <param name="to">目标状态</param>
    /// <param name="conditionFeal">条件字段</param>
    /// <param name="mode">条件转换的判断方式</param>
    /// <param name="threshole">条件字段</param>
    /// <param name="exitTime">动画转换时间</param>
    static void AddAConditionInExitTransition(AnimatorStateMachine stateMachine,
        AnimatorState from,
        AnimatorState to,
        string conditionFeal,
        AnimatorConditionMode mode,
        int threshole = 0
    )
    {
        if (NotChangeStates.Contains(from)) return;

        AnimatorStateTransition[] fromTras = from.transitions;
        for (int i = 0; i < fromTras.Length; i++)
        {
            AnimatorStateTransition trans = fromTras[i];
            if (trans.destinationState == to)
            {
                //判断condition是否存在，存在则跳过
                for (int p = 0; p < trans.conditions.Length; p++)
                {
                    var cond = trans.conditions[p];
                    if (cond.parameter == conditionFeal && cond.mode == mode &&
                        cond.threshold == threshole)
                    {
                        return;
                    }
                }

                fromTras[i].AddCondition(mode, threshole, conditionFeal);
                break;
            }
        }
    }


    #endregion

}
