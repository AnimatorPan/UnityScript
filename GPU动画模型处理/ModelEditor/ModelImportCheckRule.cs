using System;
using System.Collections.Generic;
using DodGame;
using UnityEditor;
using UnityEngine;
using Object = System.Object;

namespace A9Game
{

    class CheckTextureOption
    {
        public bool CheckTextureMipMap = false;
        public bool TextureMipMap = false;
        public bool CheckTextureSize = false;
        public int MaxTexutreSize = 0;
        public bool CheckTextureFormat = false;
        public TextureImporterFormat TextureFormat = TextureImporterFormat.AutomaticCompressed;
        public bool CheckReadWrite = false;
        public bool IsReadWrite = false;
        public bool CheckHasAlphaChannel = false;

        public bool NeedCheck
        {
            get { return CheckTextureMipMap || CheckTextureSize || CheckTextureFormat; }
        }
    }

    class CheckRuleOption
    {
        public bool CheckLightProbe = false;
        public bool LightProbe = false;
        public bool CheckOptmizeMesh = false;
        public bool OptmizeMesh = true;
        public bool CheckMeshIsReadable = false;
        public bool MeshIsReadable = false;
        public bool CheckOptimizeGameObjects = false;
        public bool OptimizeGameObjects = false;
        public bool CheckShadow = false;
        public bool CheckAnimatorCullingMode = false;
        public AnimatorCullingMode AnimCullMode = AnimatorCullingMode.CullUpdateTransforms;
        public bool SetAnimCullMode = true;
        public bool CheckAnimatorApplyRoot = true;
        public bool AnimatorApplyRoot = false;

        /// <summary>
        /// 是否检查动画文件是否使用了压缩提取的方式
        /// </summary>
        public bool CheckAnimatorFileUseCompress = false;

        /// <summary>
        /// 检查选择动画是否是循环的
        /// </summary>
        public bool CheckAnimatorRotLoop = false;
        
        public bool CheckLodGroup = false;
        public bool CheckMeshCollider = false;
        public bool CheckGoPoolBehaviourItem = false;
        /// <summary>
        /// 检查是否需要增加AnimatorConfig脚本
        /// </summary>
        public bool CheckAnimatorConfig = false;
        /// <summary>
        /// 检查动画controll里的参数
        /// </summary>
        public bool CheckActorAnimatorParam = false;
        /// <summary>
        /// 检查是否增加BoneConfig脚本
        /// </summary>
        public bool CheckBoneConfig = false;

        /// <summary>
        /// 检查是否有无效的脚本
        /// </summary>
        public bool CheckMissScript = true;
        public bool CheckColliderKinematic = false;

        ///检查模型中不能带有collider
        public bool CheckNoCollider = false;

        public CheckTextureOption MainTexOption = new CheckTextureOption();
        public CheckTextureOption MaskTexOption = new CheckTextureOption();
        public CheckTextureOption NormalTexOption = new CheckTextureOption();

        public bool CheckTpsConfigScript = true;
        public bool CheckRagdollConfigScript = true;
        public bool CheckShaderConfigScript = true;

        /// <summary>
        /// 检查是否需要合并Mesh
        /// </summary>
        public bool CheckNeedCombineMesh = false;

        /// <summary>
        /// 是否检查lod的参数标准化80,35, 0
        /// </summary>
        public bool CheckStandaradLodParam = false;
        /// <summary>
        /// 是否检查prefab里的坐标是否为0
        /// </summary>
        public bool CheckPositionZero = false;

        /// <summary>
        /// 是否检查root有没有collider
        /// </summary>
        public bool CheckRootCollider = false;

        /// <summary>
        /// 清理不用的骨骼点，Nub结尾的
        /// </summary>
        public bool ClearUnusedBone = false;

    }

    /// <summary>
    /// 检查音效的选项
    /// </summary>
    class CheckAudioOption
    {
        public bool CheckLoadType = false;
        public AudioClipLoadType LoadType = AudioClipLoadType.CompressedInMemory;
        public int SetComporessBitRate = 0;
        public bool Check3D = false;
        public bool Is3DType = false;
    }

    class ModelImportCheckRule
    {
        [MenuItem("ResourceRuleCheck/一键检查所有资源规则")]
        public static void CheckAllPrefab()
        {
            CheckFpsPrefab();
            CheckTpsPrefab();
            CheckDeathPrefab();
            CheckShowPrefab();
            CheckEffectPrefab();
            CheckAllAudio();
        }


        [MenuItem("ResourceRuleCheck/检查FPS Prefab规则")]
        public static void CheckFpsPrefab()
        {
            bool checkPass = true;
            int checkPrefabCnt = 0;

            var option = new CheckRuleOption();
            option.CheckLightProbe = true;
            option.LightProbe = true;
            option.CheckOptmizeMesh = true;
            option.OptmizeMesh = true;
            option.CheckMeshIsReadable = true;
            option.MeshIsReadable = false;
            option.CheckOptimizeGameObjects = true;
            option.OptimizeGameObjects = true;
            option.CheckShadow = true;
            option.CheckAnimatorCullingMode = true;
            option.AnimCullMode = AnimatorCullingMode.AlwaysAnimate;
            option.CheckAnimatorApplyRoot = true;
            option.AnimatorApplyRoot = false;
            option.CheckAnimatorFileUseCompress = true;
            option.CheckActorAnimatorParam = true;

            option.CheckLodGroup = false;
            option.CheckMeshCollider = true;
            option.CheckGoPoolBehaviourItem = true;
            option.CheckAnimatorConfig = true;
            option.CheckBoneConfig = true;
            option.CheckMissScript = true;
            option.CheckColliderKinematic = true;
            option.CheckNoCollider = true;

            option.MainTexOption.CheckTextureMipMap = true;
            option.MainTexOption.TextureMipMap = false;
            option.MainTexOption.CheckTextureSize = true;
            option.MainTexOption.MaxTexutreSize = 1024;
            option.MainTexOption.CheckTextureFormat = true;
            option.MainTexOption.TextureFormat = TextureImporterFormat.AutomaticCompressed;

            option.MaskTexOption.CheckTextureMipMap = true;
            option.MaskTexOption.TextureMipMap = false;
            option.MaskTexOption.CheckTextureSize = true;
            option.MaskTexOption.MaxTexutreSize = 512;
            option.MaskTexOption.CheckTextureFormat = true;
            option.MaskTexOption.TextureFormat = TextureImporterFormat.AutomaticCompressed;

            option.NormalTexOption.CheckTextureMipMap = true;
            option.NormalTexOption.TextureMipMap = false;
            option.NormalTexOption.CheckTextureSize = true;
            option.NormalTexOption.MaxTexutreSize = 512;
            option.NormalTexOption.CheckTextureFormat = true;  //不做检测，交给各个贴图自己定义
            option.NormalTexOption.TextureFormat = TextureImporterFormat.AutomaticTruecolor;
            
            Action<GameObject> checkModelFunc = (GameObject goPrefab) =>
            {
                checkPrefabCnt++;
                if (!CheckRuleModel(option, goPrefab))
                {
                    checkPass = false;
                }
            };

            option.CheckNeedCombineMesh = true;
            VisitAllFolderPrefab("Resources/Actor/Player/Fps", checkModelFunc);

            if (checkPass)
            {
                Debug.Log("check fps rule pass: " + checkPrefabCnt);
            }
            else
            {
                Debug.LogError("check fps rule failed: " + checkPrefabCnt);
            }
        }


        [MenuItem("ResourceRuleCheck/检查TPS Prefab规则")]
        public static void CheckTpsPrefab()
        {
            bool checkPass = true;
            int checkPrefabCnt = 0;

            var option = new CheckRuleOption();
            option.CheckLightProbe = true;
            option.LightProbe = true;
            option.CheckOptmizeMesh = true;
            option.OptmizeMesh = true;
            option.CheckMeshIsReadable = true;
            option.MeshIsReadable = false;
            option.CheckOptimizeGameObjects = true;
            option.OptimizeGameObjects = false;
            option.CheckShadow = true;
            option.CheckAnimatorCullingMode = true;
            option.AnimCullMode = AnimatorCullingMode.CullUpdateTransforms;
            option.CheckAnimatorApplyRoot = true;
            option.AnimatorApplyRoot = false;
            option.CheckAnimatorFileUseCompress = true;
            option.CheckActorAnimatorParam = true;

            option.CheckLodGroup = true;
            option.CheckMeshCollider = true;
            option.CheckGoPoolBehaviourItem = true;
            option.CheckMissScript = true;
            option.CheckColliderKinematic = true;
            option.CheckNoCollider = true;

            option.MainTexOption.CheckTextureMipMap = true;
            option.MainTexOption.TextureMipMap = true;
            option.MainTexOption.CheckTextureSize = true;
            option.MainTexOption.MaxTexutreSize = 512;
            option.MainTexOption.CheckTextureFormat = true;
            option.MainTexOption.TextureFormat = TextureImporterFormat.AutomaticCompressed;

            option.MaskTexOption.CheckTextureMipMap = true;
            option.MaskTexOption.TextureMipMap = true;
            option.MaskTexOption.CheckTextureSize = true;
            option.MaskTexOption.MaxTexutreSize = 512;
            option.MaskTexOption.CheckTextureFormat = true;
            option.MaskTexOption.TextureFormat = TextureImporterFormat.AutomaticCompressed;

            option.NormalTexOption.CheckTextureMipMap = true;
            option.NormalTexOption.TextureMipMap = false;
            option.NormalTexOption.CheckTextureSize = true;
            option.NormalTexOption.MaxTexutreSize = 512;
            option.NormalTexOption.CheckTextureFormat = true;
            option.NormalTexOption.TextureFormat = TextureImporterFormat.AutomaticCompressed;

            Action<GameObject> checkModelFunc = (GameObject goPrefab) =>
            {
                checkPrefabCnt++;
                if (!CheckRuleModel(option, goPrefab))
                {
                    checkPass = false;
                }
            };

            option.CheckNeedCombineMesh = false;
            option.CheckPositionZero = true;
            option.CheckStandaradLodParam = true;
            option.CheckAnimatorConfig = true;
            option.CheckBoneConfig = true;
            option.CheckAnimatorRotLoop = true;
            option.ClearUnusedBone = true;
            VisitAllFolderPrefab("Prefabs/Actor/Player/Tps", checkModelFunc);
            VisitAllFolderPrefab("Prefabs/Actor/Chiji/Tps", checkModelFunc);

            option.CheckNeedCombineMesh = true;
            VisitAllFolderPrefab("Resources/Actor/Player/Tps", checkModelFunc);
            VisitAllFolderPrefab("Resources/Actor/Chiji/Tps", checkModelFunc);

            ///Human的节点，也检查下是否需要combinemkesh
            VisitAllFolderPrefab("Resources/Actor/Npc/Human", checkModelFunc);

            option.CheckAnimatorRotLoop = false;
            option.CheckStandaradLodParam = false;
            option.CheckNeedCombineMesh = false;

            option.MainTexOption = new CheckTextureOption();
            option.MaskTexOption = new CheckTextureOption();
            option.NormalTexOption = new CheckTextureOption();

            option.CheckColliderKinematic = false;
            option.CheckNoCollider = false;
            option.ClearUnusedBone = true;
            VisitAllFolderPrefab("Resources/Actor/Npc/Tps", checkModelFunc);

            option.CheckActorAnimatorParam = false;
            option.ClearUnusedBone = true;
            VisitAllFolderPrefab("Resources/Actor/Npc/Guide", checkModelFunc);

            option = new CheckRuleOption();
            option.CheckMeshIsReadable = true;
            option.MeshIsReadable = false;
            option.CheckOptmizeMesh = true;
            option.OptmizeMesh = true;
            option.CheckOptimizeGameObjects = true;
            option.OptimizeGameObjects = true;
            option.CheckShadow = true;
            option.CheckAnimatorCullingMode = true;
            option.AnimCullMode = AnimatorCullingMode.CullUpdateTransforms;
            option.CheckAnimatorApplyRoot = true;
            option.AnimatorApplyRoot = false;
            option.CheckLodGroup = false;
            option.CheckMeshCollider = true;
            option.CheckGoPoolBehaviourItem = true;
            option.CheckMissScript = true;
            option.CheckColliderKinematic = true;
            option.CheckPositionZero = true;
            option.ClearUnusedBone = true;

            checkModelFunc = (GameObject goPrefab) =>
            {
                checkPrefabCnt++;
                if (!CheckRuleModel(option, goPrefab))
                {
                    checkPass = false;
                }
            };

            VisitAllFolderPrefab("Resources/Actor/Item", checkModelFunc);

            if (checkPass)
            {
                Debug.Log("check tps rule pass: " + checkPrefabCnt);
            }
            else
            {
                Debug.LogError("check tps rule failed: " + checkPrefabCnt);
            }
        }


        [MenuItem("ResourceRuleCheck/检查Death Prefab规则")]
        public static void CheckDeathPrefab()
        {
            bool checkPass = true;
            int checkPrefabCnt = 0;

            var option = new CheckRuleOption();
            option.CheckLightProbe = true;
            option.LightProbe = true;
            option.CheckOptmizeMesh = true;
            option.OptmizeMesh = true;
            option.CheckMeshIsReadable = true;
            option.MeshIsReadable = false;
            option.CheckOptimizeGameObjects = true;
            option.OptimizeGameObjects = false;
            option.CheckShadow = true;
            option.CheckAnimatorCullingMode = true;
            option.AnimCullMode = AnimatorCullingMode.CullUpdateTransforms;
            option.CheckAnimatorApplyRoot = true;
            option.AnimatorApplyRoot = false;
            option.CheckLodGroup = true;
            option.CheckMeshCollider = true;
            option.CheckGoPoolBehaviourItem = true;
            option.CheckMissScript = true;

            option.CheckStandaradLodParam = true;
            option.CheckPositionZero = true;
            option.CheckBoneConfig = true;
            option.ClearUnusedBone = true;

            Action<GameObject> checkModelFunc = (GameObject goPrefab) =>
            {
                checkPrefabCnt++;
                if (!CheckRuleModel(option, goPrefab))
                {
                    checkPass = false;
                }
            };

            option.CheckNeedCombineMesh = false;
            VisitAllFolderPrefab("Prefabs/Actor/Player/Death", checkModelFunc);
            VisitAllFolderPrefab("Prefabs/Actor/Chiji/Death", checkModelFunc);

            option.CheckNeedCombineMesh = true;
            VisitAllFolderPrefab("Resources/Actor/Player/Death", checkModelFunc);
            VisitAllFolderPrefab("Resources/Actor/Chiji/Death", checkModelFunc);

            option.CheckNeedCombineMesh = false;
            VisitAllFolderPrefab("Resources/Actor/Npc/Death", checkModelFunc);

            if (checkPass)
            {
                Debug.Log("check death rule pass: " + checkPrefabCnt);
            }
            else
            {
                Debug.LogError("check death rule failed: " + checkPrefabCnt);
            }
        }

        [MenuItem("ResourceRuleCheck/检查展示 Prefab规则")]
        public static void CheckShowPrefab()
        {
            bool checkPass = true;
            int checkPrefabCnt = 0;

            var option = new CheckRuleOption();
            option.CheckLightProbe = true;
            option.LightProbe = false;
            option.CheckOptmizeMesh = true;
            option.OptmizeMesh = true;
            option.CheckMeshIsReadable = true;
            option.MeshIsReadable = false;
            option.CheckOptimizeGameObjects = false;
            option.CheckShadow = true;
            option.CheckAnimatorCullingMode = true;
            option.AnimCullMode = AnimatorCullingMode.CullUpdateTransforms;
            option.SetAnimCullMode = false;
            option.CheckAnimatorApplyRoot = true;
            option.AnimatorApplyRoot = false;
            option.CheckAnimatorFileUseCompress = true;

            option.CheckLodGroup = false;
            option.CheckMeshCollider = true;
            option.CheckGoPoolBehaviourItem = false;
            option.CheckMissScript = true;
            option.CheckColliderKinematic = true;
            option.CheckNoCollider = true;

            option.MainTexOption.CheckTextureMipMap = true;
            option.MainTexOption.TextureMipMap = false;
            option.MainTexOption.CheckTextureSize = true;
            option.MainTexOption.MaxTexutreSize = 1024;
            option.MainTexOption.CheckTextureFormat = true;
            option.MainTexOption.TextureFormat = TextureImporterFormat.AutomaticCompressed;
            option.MainTexOption.CheckHasAlphaChannel = true;

            option.MaskTexOption.CheckTextureMipMap = true;
            option.MaskTexOption.TextureMipMap = false;
            option.MaskTexOption.CheckTextureSize = true;
            option.MaskTexOption.MaxTexutreSize = 512;
            option.MaskTexOption.CheckTextureFormat = true;
            option.MaskTexOption.TextureFormat = TextureImporterFormat.AutomaticCompressed;
            option.MaskTexOption.CheckHasAlphaChannel = true;

            option.NormalTexOption.CheckTextureMipMap = true;
            option.NormalTexOption.TextureMipMap = false;
            option.NormalTexOption.CheckTextureSize = true;
            option.NormalTexOption.MaxTexutreSize = 512;
            option.NormalTexOption.CheckTextureFormat = true;
            option.NormalTexOption.TextureFormat = TextureImporterFormat.AutomaticTruecolor;
            option.NormalTexOption.CheckHasAlphaChannel = true;

            Action<GameObject> checkModelFunc = (GameObject goPrefab) =>
            {
                checkPrefabCnt++;
                if (!CheckRuleModel(option, goPrefab))
                {
                    checkPass = false;
                }
            };

            VisitAllFolderPrefab("Resources/Actor/Player/Show", checkModelFunc);

            if (checkPass)
            {
                Debug.Log("check show rule pass: " + checkPrefabCnt);
            }
            else
            {
                Debug.LogError("check show rule failed: " + checkPrefabCnt);
            }
        }


        [MenuItem("ResourceRuleCheck/检查特效Prefab规则")]
        public static void CheckEffectPrefab()
        {
            bool checkPass = true;
            int checkPrefabCnt = 0;

            var option = new CheckRuleOption();
            //option.CheckLightProbe = true;
            //option.LightProbe = false;

            option.CheckMeshIsReadable = true;
            option.MeshIsReadable = false;
            option.CheckOptmizeMesh = true;
            option.OptmizeMesh = true;
            option.CheckOptimizeGameObjects = true;
            option.OptimizeGameObjects = true;
            option.CheckShadow = true;
            option.CheckAnimatorCullingMode = true;
            option.AnimCullMode = AnimatorCullingMode.CullUpdateTransforms;
            option.CheckAnimatorApplyRoot = true;
            option.AnimatorApplyRoot = false;
            option.CheckLodGroup = false;
            option.CheckMeshCollider = true;
            option.CheckGoPoolBehaviourItem = true;
            option.CheckMissScript = true;
            option.CheckColliderKinematic = true;
            option.CheckRootCollider = true;

            Action<GameObject> checkModelFunc = (GameObject goPrefab) =>
            {
                checkPrefabCnt++;
                if (!CheckRuleModel(option, goPrefab))
                {
                    checkPass = false;
                }
            };
            
            VisitAllFolderPrefab("Resources/Effect/Actor", checkModelFunc);
            VisitAllFolderPrefab("Resources/Effect/Bullet", checkModelFunc);
            VisitAllFolderPrefab("Resources/Effect/Item", checkModelFunc);
            VisitAllFolderPrefab("Resources/Effect/Skin display", checkModelFunc);
            VisitAllFolderPrefab("Resources/Effect/Common/fabao", checkModelFunc);
            VisitAllFolderPrefab("Resources/Effect/Common/junei", checkModelFunc);
            VisitAllFolderPrefab("Resources/Effect/skill_guai", checkModelFunc);

            option = new CheckRuleOption();
            option.CheckAnimatorCullingMode = true;
            option.AnimCullMode = AnimatorCullingMode.CullUpdateTransforms;
            option.CheckAnimatorApplyRoot = true;
            option.AnimatorApplyRoot = false;
            option.CheckMissScript = true;
            option.CheckNoCollider = true;
            option.CheckMeshCollider = true;
            option.CheckRootCollider = true;

            checkModelFunc = (GameObject goPrefab) =>
            {
                checkPrefabCnt++;
                if (!CheckRuleModel(option, goPrefab))
                {
                    checkPass = false;
                }
            };

            VisitAllFolderPrefab("Resources/Effect/Scene", checkModelFunc);

            option.CheckAnimatorCullingMode = false;
            VisitAllFolderPrefab("Resources/Effect/UI", checkModelFunc);

            option.CheckGoPoolBehaviourItem = true;
            VisitAllFolderPrefab("Resources/Effect/UI/danger", checkModelFunc);

            option.CheckGoPoolBehaviourItem = true;
            VisitAllFolderPrefab("Resources/UI/HUD", checkModelFunc);

            if (checkPass)
            {
                Debug.Log("check effect rule pass: " + checkPrefabCnt);
            }
            else
            {
                Debug.LogError("check effect rule failed: " + checkPrefabCnt);
            }
            
        }

        [MenuItem("SceneTool/临时替换SkinnedMesh")]
        public static void TestReplaceSkinnedMesh()
        {
            var allSelObj = Selection.GetFiltered(typeof(GameObject), SelectionMode.DeepAssets | SelectionMode.TopLevel);
            foreach (var selObj in allSelObj)
            {
                ReplaceSkinnedMesh(selObj as GameObject);
            }
        }

        private static void VisitAllFolderPrefab(string folderPath, Action<GameObject> procAction)
        {
            System.IO.DirectoryInfo rootDir = new System.IO.DirectoryInfo(Application.dataPath + "/" + folderPath);

            if (!rootDir.Exists)
                return;

            System.IO.FileInfo[] files = null;
            files = rootDir.GetFiles("*.prefab", System.IO.SearchOption.AllDirectories);
            int currIndex = 0;
            foreach (var fileInfo in files)
            {
                string path1 = fileInfo.FullName.Replace("\\", "/").Replace("//", "/");
                path1 = path1.Substring(path1.IndexOf("Assets"));

                if (path1.Contains("yilishabai_fps"))
                {
                    continue;
                }
                EditorUtility.DisplayProgressBar("正在检查", path1, (float)currIndex / (float)files.Length);
                currIndex++;

                var assetObject = AssetDatabase.LoadAssetAtPath(path1, typeof(GameObject)) as GameObject;
                //int index = 0;

                if (assetObject != null)
                {
                    procAction(assetObject);
                }
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }

        private static void VisitAllPrefab(Action<GameObject> procAction)
        {
            var allSelObj = Selection.GetFiltered(typeof(Object), SelectionMode.DeepAssets | SelectionMode.TopLevel);
            int currIndex = 0;

            foreach (var selObj in allSelObj)
            {
                currIndex++;

                string assetPath = AssetDatabase.GetAssetPath(selObj);
                EditorUtility.DisplayProgressBar("正在检查", assetPath, (float)currIndex / (float)allSelObj.Length);

                var goPrefab = selObj as GameObject;
                if (goPrefab == null)
                {
                    continue;
                }

                //特殊模型，跳过
                if (goPrefab.name.Contains("daqishi_fps_th"))
                {
                    continue;
                }

                procAction(goPrefab);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }

        private static bool CheckRuleModel(CheckRuleOption option, GameObject goPrefab)
        {
            ///先判断LightProbe
            string assetPath = AssetDatabase.GetAssetPath(goPrefab);
            var goInst = PrefabUtility.InstantiatePrefab(goPrefab) as GameObject;
            
            bool deleteSpringLink = false;
            bool defaultDisableChange = false;
            bool lightProbeChange = false;
            bool optmizeMeshChange = false;
            bool meshReadableChagned = false;
            bool shaderChanged = false;
            bool animCullingChanged = false;
            bool animApplyRootChange = false;
            bool animActorParamInitChange = false;
            bool poolItemScriptChange = false;
            bool texChanged = false;
            bool shaderConfigChange = false;
            bool tpsConfigChange = false;
            bool ragdollConfigChange = false;
            bool combineMeshChange = false;
            bool changeAvatarChange = false;
            bool changeLodParam = false;
            bool changedPostion = false;
            bool removeUnusedLodMesh = false;
            bool changeAnimatorConfig = false;
            bool changeBoneConfig = false;
            bool checkRotAnimLoop = false;
            int unusedBoneNum = 0;
            bool changed = false;

            bool checkPass = true;
            
            if (option.CheckNeedCombineMesh)
            {
                if (ModelMeshCombineTool.CombineMesh(goInst))
                {
                    changed = true;
                    combineMeshChange = true;
                }

                if (ModelMeshCombineTool.CheckAvatarFromFbx(goInst))
                {
                    changed = true;
                    changeAvatarChange = true;
                }
            }
            
            if (!goInst.activeInHierarchy)
            {
                defaultDisableChange = true;
                changed = true;
                goInst.SetActive(true);
            }

            Renderer[] subRenders = goInst.GetComponentsInChildren<Renderer>();
            if (subRenders != null)
            {
                for (int i = 0; i < subRenders.Length; i++)
                {
                    var render = subRenders[i];

                    ///检查lightprobe选项
                    if (option.CheckLightProbe && render.useLightProbes != option.LightProbe)
                    {
                        changed = true;
                        render.useLightProbes = option.LightProbe;
                        lightProbeChange = true;
                    }

                    ///把影子给关掉
                    if (option.CheckShadow && (render.castShadows || render.receiveShadows))
                    {
                        render.castShadows = false;
                        render.receiveShadows = false;
                        changed = true;
                        shaderChanged = true;
                    }

                    ///只有角色的，才需要处理
                    Mesh sharedMesh = null;
                    if (render is SkinnedMeshRenderer)
                    {
                        var skinnedRender = render as SkinnedMeshRenderer;
                        sharedMesh = skinnedRender.sharedMesh;
                    }
                    else if (render is MeshRenderer)
                    {
                        var meshFilter = render.gameObject.GetComponent<MeshFilter>();
                        if (meshFilter != null)
                        {
                            sharedMesh = meshFilter.sharedMesh;
                        }
                    }
                    
                    ///判断mesh的导入选项是否已经优化过了
                    /// TPS要关闭掉，方式lightprobe不生效
                    if (sharedMesh != null)
                    {
                        ///判断mesh的导入选项是否已经优化过了
                        /// TPS要关闭掉，方式lightprobe不生效
                        string meshAssetPath = AssetDatabase.GetAssetPath(sharedMesh);
                        ModelImporter importer = AssetImporter.GetAtPath(meshAssetPath) as ModelImporter;
                        if (importer != null)
                        {
                            if (option.CheckOptmizeMesh && importer.optimizeMesh != option.OptmizeMesh)
                            {
                                importer.optimizeMesh = option.OptmizeMesh;
                                optmizeMeshChange = true;
                                changed = true;

                                AssetDatabase.ImportAsset(meshAssetPath, ImportAssetOptions.ForceUpdate);
                            }

                            if (option.CheckMeshIsReadable && importer.isReadable != option.MeshIsReadable)
                            {
                                importer.isReadable = option.MeshIsReadable;
                                meshReadableChagned = true;
                                changed = true;

                                AssetDatabase.ImportAsset(meshAssetPath, ImportAssetOptions.ForceUpdate);
                            }

                            ///只有SKinnedMesh才有意义
                            if (render is SkinnedMeshRenderer &&
                                option.CheckOptimizeGameObjects && importer.optimizeGameObjects != option.OptimizeGameObjects)
                            {
                                //没有优化go
                                string errInfo = string.Format("set optmize flag in skinned mesh: {0}, cancel it",
                                                     meshAssetPath);
                                Debug.LogError(errInfo, goPrefab);

                                importer.optimizeGameObjects = option.OptimizeGameObjects;

                                EditorUtility.DisplayDialog("提示", errInfo, "ok");
                                AssetDatabase.ImportAsset(meshAssetPath, ImportAssetOptions.ForceUpdate);
                            }
                        }
                    }

                    ///检查贴图的mip
                    var sharedMats = render.sharedMaterials;
                    if (sharedMats != null && sharedMats.Length > 0 &&
                        (option.MainTexOption.NeedCheck || 
                        option.MaskTexOption.NeedCheck || 
                        option.NormalTexOption.NeedCheck))
                    {
                        List<Texture> listTex = new List<Texture>();

                        foreach (var sharedMat in sharedMats)
                        {
                            if (sharedMat.shader.name.Contains("Character/BRDF") &&
                            !sharedMat.shader.name.Contains("BRDF_Yinzi_Rim"))
                            {
                                var mainTex = sharedMat.GetTexture("_MainTex");
                                var normalTex = sharedMat.GetTexture("_BumpMap");
                                var maskTex = sharedMat.GetTexture("_MaskMap");

                                if (mainTex == null)
                                {
                                    Debug.LogError("Invalid main tex: " + sharedMat.name, sharedMat);
                                }
                                else if (CheckTextureFormat(goPrefab, mainTex, option.MainTexOption, false))
                                {
                                    changed = true;
                                    texChanged = true;
                                }

                                if (maskTex == null)
                                {
                                    Debug.LogError("Invalid mask tex: " + sharedMat.name, sharedMat);
                                }

                                if (CheckTextureFormat(goPrefab, maskTex, option.MaskTexOption, false))
                                {
                                    changed = true;
                                    texChanged = true;
                                }

                                if (normalTex == null)
                                {
                                    if (!goPrefab.name.Contains("shibing"))
                                    {
                                        Debug.LogError("Invalid normal tex: " + sharedMat.name, sharedMat);
                                    }
                                }

                                if (CheckTextureFormat(goPrefab, normalTex, option.NormalTexOption, true))
                                {
                                    changed = true;
                                    texChanged = true;
                                }
                            }
                        }
                    }
                }
            }

            ///检查根节点，不能带有collider
            if (option.CheckRootCollider)
            {
                var rootCollider = goInst.GetComponent<Collider>();
                if (rootCollider != null)
                {
                    Debug.LogError(goPrefab.name + "has root collider, check failed", goPrefab);
                    checkPass = false;
                }
            }

            ///检查collider
            if (option.CheckColliderKinematic || option.CheckNoCollider)
            {
                var colliders = goInst.GetComponentsInChildren<Collider>(true);
                if (option.CheckNoCollider)
                {
                    if (colliders != null && colliders.Length > 0)
                    {
                        EditorUtility.DisplayDialog("检查错误", "模型绑定了Collider", "ok");
                        Debug.LogError("模型包含了Collider: " + assetPath, goPrefab);
                        return false;
                    }
                }
                
                if (option.CheckColliderKinematic && colliders != null && colliders.Length > 0)
                {
                    //检查是否设置了rigidbody
                    //避免physics性能问题
                    foreach (var collider in colliders)
                    {
                        var rigidBody = collider.GetComponent<Rigidbody>();
                        if (rigidBody != null)
                        {
                            if (rigidBody.useGravity)
                            {
                                rigidBody.useGravity = false;
                                changed = true;
                                Debug.Log("去掉rigidBody的useGravity选项" + assetPath, goPrefab);
                            }
                            if (!rigidBody.isKinematic)
                            {
                                rigidBody.isKinematic = true;
                                changed = true;
                                Debug.Log("修改rigidBody 为Kinematic " + assetPath, goPrefab);
                            }
                            if (rigidBody.constraints != RigidbodyConstraints.FreezeAll)
                            {
                                rigidBody.constraints = RigidbodyConstraints.FreezeAll;
                                Debug.Log("修改rigidBody constraint为 FreezeAll " + assetPath, goPrefab);
                            }
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("检查错误", "Collider没有绑定RigidBody", "ok");
                            Debug.LogError("Collider没有绑定RigidBody: " + assetPath, goPrefab);

                            var newRigidBody = collider.gameObject.AddComponent<Rigidbody>();
                            newRigidBody.useGravity = false;
                            newRigidBody.isKinematic = true;
                            newRigidBody.constraints = RigidbodyConstraints.FreezeAll;
                            changed = true;
                        }
                    }
                }
            }
            
            
            ///todo...检查animator的Render模式
            var allAnims = goInst.GetComponentsInChildren<Animator>();
            if (allAnims != null)
            {
                foreach (var anim in allAnims)
                {
                    //TPS需要优化动画
                    if (option.CheckAnimatorCullingMode && anim.cullingMode != option.AnimCullMode)
                    {
                        if (option.SetAnimCullMode)
                        {
                            anim.cullingMode = option.AnimCullMode;
                        }
                        changed = true;
                        animCullingChanged = true;
                    }

                    if (option.CheckAnimatorApplyRoot && anim.applyRootMotion != option.AnimatorApplyRoot)
                    {
                        anim.applyRootMotion = option.AnimatorApplyRoot;
                        changed = true;
                        animApplyRootChange = true;
                    }

                    if (option.CheckActorAnimatorParam)
                    {
                        UnityEditor.Animations.AnimatorController ac = anim.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
                        if (ModelTools.CheckAndInitAnimParam(ac))
                        {
                            EditorUtility.SetDirty(ac);

                            changed = true;
                            animActorParamInitChange = true;

                            Debug.LogError(string.Format("Anmator init param changed: {0}", AssetDatabase.GetAssetPath(ac)), ac);
                        }
                    }

                    if (option.CheckAnimatorFileUseCompress ||
                        option.CheckAnimatorRotLoop)
                    {
                        UnityEditor.Animations.AnimatorController ac = anim.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;

                        var allLayers = ac.layers;
                        for (int i = 0; ac != null && i < allLayers.Length; i++)
                        {
                            var layer = allLayers[i];
                            var sm = layer.stateMachine;

                            var childStates = layer.stateMachine.states;
                            for (int k = 0; k < childStates.Length; k++)
                            {
                                var childState = childStates[k];
                                var clip = childState.state.motion;
                                if (clip != null)
                                {
                                    
                                    string clipPath = AssetDatabase.GetAssetPath(clip);

                                    if (option.CheckAnimatorFileUseCompress)
                                    {
                                        if (clipPath.EndsWith(".fbx", StringComparison.CurrentCultureIgnoreCase))
                                        {
                                            Debug.LogError("Animator controll use fbx animation: " + clipPath, clip);
                                            Debug.LogError("from animator controll " + clipPath, ac);
                                        }
                                    }

                                    if (option.CheckAnimatorRotLoop)
                                    {
                                        if (childState.state.name.Contains("turn"))
                                        {
                                            if (!clip.isLooping)
                                            {
                                                Debug.LogError("旋转动画没有配置为循环类型: " + clipPath, clip);
                                                EditorUtility.DisplayDialog("检测错误", "旋转动画没有配置为循环类型: "
                                                                                    + clipPath, "ok");

                                                checkPass = false;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            ///检查lod
            if (option.CheckLodGroup)
            {
                bool lodCheckPass = true;
                var lodGroup = goInst.GetComponent<LODGroup>();
                if (lodGroup != null)
                {
                    var lodGroupInfo = new LodGroupInfo(lodGroup);
                    int lodCount = lodGroupInfo.GetLodCount();
                    if (lodCount > 0)
                    {
                        for (int i = 0; i < lodCount-1; i++)
                        {
                            var lodItem = lodGroupInfo.GetLodItemInfo(i);
                            if (lodItem.MeshCount <= 0)
                            {
                                lodCheckPass = false;
                                break;
                            }

                            bool haveInvalidMesh = false;
                            List<Renderer> listRender = new List<Renderer>();
                            for (int k = 0; k < lodItem.MeshCount; k++)
                            {
                                var subMesh = lodItem.GetRenderer(k);
                                if (subMesh == null)
                                {
                                    lodCheckPass = false;
                                    haveInvalidMesh = true;
                                }
                                else
                                {
                                    listRender.Add(subMesh);
                                }
                            }

                            if (haveInvalidMesh)
                            {
                                changed = true;
                                removeUnusedLodMesh = true;

                                lodItem.ClearReander();
                                foreach (var subRender in listRender)
                                {
                                    lodItem.AddRender(subRender);
                                }
                            }
                            
                            if (option.CheckStandaradLodParam)
                            {
                                if (0 == i && Mathf.Abs(lodItem.ScreenPercent - 0.5f) > 0.01f)
                                {
                                    lodItem.ScreenPercent = 0.5f;
                                    changed = true;
                                    changeLodParam = true;

                                    lodGroupInfo.ApplyModifiedProperties();
                                }
                                else if (1 == i && Mathf.Abs(lodItem.ScreenPercent - 0.2f) > 0.01f)
                                {
                                    lodItem.ScreenPercent = 0.2f;
                                    changed = true;
                                    changeLodParam = true;

                                    lodGroupInfo.ApplyModifiedProperties();
                                }
                            }
                        }
                    }
                }

                if (!lodCheckPass)
                {
                    EditorUtility.DisplayDialog("检查错误", goPrefab.name + "模型的LOD没有配置Mesh", "ok");
                    Debug.LogError(string.Format("lod check failed, lod is not bind mesh: {0} ", assetPath), goPrefab);
                    checkPass = false;
                }
            }

            if (option.CheckAnimatorConfig)
            {
                var animatorConfig = goInst.GetComponent<AnimatorConfig>();
                if (animatorConfig == null)
                {
                    animatorConfig = goInst.AddComponent<AnimatorConfig>();
                    changed = true;
                    changeAnimatorConfig = true;
                }

                if (animatorConfig)
                {
                    if (AnimatorConfigInspector.EditorCollect(animatorConfig))
                    {
                        changed = true;
                        changeAnimatorConfig = true;
                    }
                }
            }
            
            if (option.CheckBoneConfig)
            {
                ///检查boneconfig，这个一般和AnimConfig肯定是一起的
                var boneConfig = goInst.GetComponent<BoneConfig>();
                if (boneConfig == null)
                {
                    boneConfig = goInst.AddComponent<BoneConfig>();
                    changed = true;
                    changeBoneConfig = true;
                }
            }

            if (option.ClearUnusedBone)
            {
                var boneRoot = goInst.transform.Find("Bip001");
                if (boneRoot != null)
                {
                    var allChild = boneRoot.GetComponentsInChildren<Transform>();
                    if (allChild != null)
                    {
                        List<Transform> listBone = new List<Transform>();
                        listBone.AddRange(allChild);

                        foreach (var childBone in listBone)
                        {
                            if (childBone.name.EndsWith("Nub"))
                            {
                                if (!IsBoneUsed(goInst, childBone))
                                {
                                    if (childBone.childCount > 0)
                                    {
                                        EditorUtility.DisplayDialog("清理骨骼失败", goPrefab.name + " invalid null bone: " + childBone.name, "ok");
                                        checkPass = false;

                                        Debug.LogError(goPrefab.name + " invalid null bone: " + childBone.name);
                                    }
                                    else
                                    {
                                        if (childBone.GetComponent<Rigidbody>() != null)
                                        {
                                            EditorUtility.DisplayDialog("清理骨骼失败", goPrefab.name + "nul bone has other component: " + childBone.name, "ok");

                                            Debug.LogError(goPrefab.name + "nul bone has other component: " + childBone.name);
                                            checkPass = false;
                                        }
                                        else
                                        {
                                            Debug.Log(goPrefab.name + " clear unused bone: " + childBone.gameObject.name);

                                            GameObject.DestroyImmediate(childBone.gameObject);
                                            changed = true;
                                            unusedBoneNum++;
                                        }
                                    }
                                }
                                else
                                {
                                    Debug.LogError(goPrefab.name + " bone is used:  " + childBone.name);
                                }
                            }
                        }
                    }
                }
            }


            ///检查是否有内存池支持
            bool isIgnorBehaviourItem = false;
            if (goPrefab.name.Contains("Empty.prefab") ||
                goPrefab.name.Contains("empty_target.prefab") ||
                goPrefab.name.Contains("EmptyHUD.prefab"))
            {
                isIgnorBehaviourItem = true;
            }


            if (option.CheckMissScript)
            { 
                ///检查是否有丢失的脚本
                int missNum = FindMissingScriptsRecursively.FindScriptMissGo(goInst, false);
                if (missNum > 0)
                {
                    Debug.LogError("Find Missing script: " + assetPath, goPrefab);
                }
            }
            
            if (option.CheckTpsConfigScript)
            {
                var script = goInst.GetComponent<TpsConfig>();
                if (script != null)
                {
                    if (script.CollectCachePos())
                    {
                        tpsConfigChange = true;
                        changed = true;
                    }
                }
            }

            if (option.CheckRagdollConfigScript)
            {
                var script = goInst.GetComponent<RagdollController>();
                if (script != null)
                {
                    if (script.CollectRagdollNode())
                    {
                        ragdollConfigChange = true;
                        changed = true;
                    }
                }
            }

            if (option.CheckPositionZero)
            {
                if (goInst.transform.position != Vector3.zero)
                {
                    goInst.transform.position = Vector3.zero;
                    changed = true;
                    changedPostion = true;
                }
            }
            
            if (changed)
            {
                if (deleteSpringLink)
                {
                    Debug.Log("delete discard sprintlinek: " + assetPath, goPrefab);
                }

                if (defaultDisableChange)
                {
                    Debug.Log("prefab defaulat is inactive, change it: " + assetPath, goPrefab);
                }

                if (combineMeshChange)
                {
                    Debug.Log("combine mesh: " + assetPath, goPrefab);
                }

                if (changeAvatarChange)
                {
                    Debug.Log("change avtar to separate file: " + assetPath, goPrefab);
                }

                if (lightProbeChange)
                {
                    Debug.Log("model fixed lightprobe: " + assetPath, goPrefab);
                }
                if (shaderChanged)
                {
                    Debug.Log("model fixed shadow flag: " + assetPath, goPrefab);
                }
                if (animCullingChanged)
                {
                    Debug.Log("model fixed animaotr culling flag: " + assetPath, goPrefab);
                }
                if (animApplyRootChange)
                {
                    Debug.Log("model fixed animaotr apply root flag: " + assetPath, goPrefab);
                }
                if (optmizeMeshChange)
                {
                    Debug.Log("model fixed optimize mesh flag: " + assetPath, goPrefab);
                }
                if (meshReadableChagned)
                {
                    Debug.Log("model fixed  mesh readable flag: " + assetPath, goPrefab);
                }

                if (poolItemScriptChange)
                {
                    Debug.Log("修改对象池的GoPoolBehaviourItem数据 " + assetPath, goPrefab);
                }

                if (shaderConfigChange)
                {
                    Debug.Log("修改对象池shaderConfig数据 " + assetPath, goPrefab);
                }
                if (tpsConfigChange)
                {
                    Debug.Log("修改对象池tpsConfig数据 " + assetPath, goPrefab);
                }
                if (ragdollConfigChange)
                {
                    Debug.Log("修改对象池RagdollConfig数据 " + assetPath, goPrefab);
                }

                if (texChanged)
                {
                    Debug.Log("修改贴图格式数据 " + assetPath, goPrefab);
                }

                if (changeLodParam)
                {
                    Debug.Log("修改lod参数" + assetPath, goPrefab);
                }

                if (changedPostion)
                {
                    Debug.Log("修改默认坐标" + assetPath, goPrefab);
                }

                if (removeUnusedLodMesh)
                {
                    Debug.LogError("移除lod中无效的mesh", goPrefab);
                }

                if (changeAnimatorConfig)
                {
                    Debug.LogError("修改AnimatorConfig脚本");
                }

                if (changeBoneConfig)
                {
                    Debug.LogError("修改BoneConfig脚本");
                }

                if (animActorParamInitChange)
                {
                    Debug.LogError("修改角色AnimatorControll的初始参数");
                }

                if (unusedBoneNum > 0)
                {
                    Debug.LogError("清理无用的骨骼数: " + unusedBoneNum);
                }
                
                PrefabUtility.ReplacePrefab(goInst, goPrefab);
                EditorUtility.SetDirty(goPrefab);
            }

            GameObject.DestroyImmediate(goInst);
            return checkPass;
        }


        private static void ReplaceSkinnedMesh(GameObject goPrefab)
        {
            var allSkinnedMesh = goPrefab.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (allSkinnedMesh != null)
            {
                foreach (var skinnedMesh in allSkinnedMesh)
                {
                    var meshFilter = XGUtil.AddMonoBehaviour<MeshFilter>(skinnedMesh.gameObject);
                    var meshRender = XGUtil.AddMonoBehaviour<MeshRenderer>(skinnedMesh.gameObject);
                    meshFilter.sharedMesh = skinnedMesh.sharedMesh;
                    meshRender.sharedMaterials = skinnedMesh.sharedMaterials;
                    meshRender.useLightProbes = skinnedMesh.useLightProbes;
                    meshRender.castShadows = false;
                    meshRender.receiveShadows = false;

                    UnityEngine.Object.DestroyImmediate(skinnedMesh);
                }
            }
        }

        private static bool CheckTextureFormat(GameObject prefab, Texture tex,CheckTextureOption option, bool isNormal)
        {
            if (tex == null)
            {
                if (!prefab.name.Contains("shibing"))
                {
                    ///Debug.LogError("texuture is null: " + prefab.name, prefab);
                }

                return false;
            }

            var texPath = AssetDatabase.GetAssetPath(tex);
            TextureImporter import = AssetImporter.GetAtPath(texPath) as TextureImporter;

            bool changed = false;
            if (option.CheckTextureSize && import.maxTextureSize > option.MaxTexutreSize)
            {
                import.maxTextureSize = option.MaxTexutreSize;
                changed = true;

                Debug.LogError("modify texture size to 512: " + texPath, tex);
            }

            if (option.CheckTextureFormat && import.textureFormat != option.TextureFormat)//TextureImporterFormat.AutomaticTruecolor)
            {
                import.textureFormat = option.TextureFormat;
                Debug.LogError("modify texture " + texPath + " to format: " + option.TextureFormat, tex);
                changed = true;
            }

            if (option.CheckTextureMipMap && import.mipmapEnabled != option.TextureMipMap)
            {
                import.mipmapEnabled = option.TextureMipMap;
                changed = true;

                if (isNormal)
                {
                    import.textureType = TextureImporterType.Default;
                    import.normalmap = true;
                }

                Debug.LogError("modify texture " + texPath + " mipmap: " + option.TextureMipMap, tex);
            }

            if (option.CheckHasAlphaChannel && import.DoesSourceTextureHaveAlpha())
            {
                EditorUtility.DisplayDialog("贴图格式检查失败","Texture has alpha channel: " + texPath, "ok");
                Debug.LogError("Texture has alpha channel: " + texPath, tex);
            }

            if (changed)
            {
                AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceUpdate);
                return true;
            }

            return false;
        }

        private static bool IsBoneUsed(GameObject go, Transform boneTrans)
        {
            var gpuRender = go.GetComponent<GPUSkinRenderer>();
            if (gpuRender != null)
            {
                if (gpuRender.bones != null)
                {
                    foreach (var usedBone in gpuRender.bones)
                    {
                        if (usedBone == boneTrans)
                        {
                            return true;
                        }
                    }
                }
            }

            ///判断skinnedmesh
            var allSkinnedMesh = go.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (allSkinnedMesh == null)
            {
                Debug.LogError("there is no skinned mesh: " + go.name);
                return true;
            }

            foreach (var skinnedMesh in allSkinnedMesh)
            {
                if (skinnedMesh.bones != null)
                {
                    foreach (var usedBone in skinnedMesh.bones)
                    {
                        if (usedBone == boneTrans)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        #region Audio

        private static void VisitAllFolderAudio(string folderPath, Action<AudioClip> procAction)
        {
            System.IO.DirectoryInfo rootDir = new System.IO.DirectoryInfo(Application.dataPath + "/" + folderPath);
            System.IO.FileInfo[] files = null;
            files = rootDir.GetFiles("*.*", System.IO.SearchOption.AllDirectories);
            int currIndex = 0;
            foreach (var fileInfo in files)
            {
                string path1 = fileInfo.FullName.Replace("\\", "/").Replace("//", "/");
                path1 = path1.Substring(path1.IndexOf("Assets"));

                EditorUtility.DisplayProgressBar("正在检查", path1, (float)currIndex / (float)files.Length);
                currIndex++;

                var assetObject = AssetDatabase.LoadAssetAtPath(path1, typeof(AudioClip)) as AudioClip;
                int index = 0;

                if (assetObject != null)
                {
                    procAction(assetObject);
                }
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }

        [MenuItem("ResourceRuleCheck/检查Audio规则")]
        private static void CheckAllAudio()
        {
            bool checkPass = true;
            int checkAudioCnt = 0;
            CheckAudioOption option = new CheckAudioOption();
            Action<AudioClip> checkAudioFunc = (audio) =>
            {
                checkAudioCnt++;
                if (!CheckAudioRule(option, audio))
                {
                    checkPass = false;
                }
            };

            option.Check3D = false;
            option.CheckLoadType = true;
            option.LoadType = AudioClipLoadType.CompressedInMemory;
            VisitAllFolderAudio("Resources/Audio/Bgm", checkAudioFunc);
            VisitAllFolderAudio("Resources/Audio/Guide", checkAudioFunc);

            VisitAllFolderAudio("Resources/Audio/MapBarrage", checkAudioFunc);
            VisitAllFolderAudio("Resources/Audio/Voice", checkAudioFunc);

            option.LoadType = AudioClipLoadType.DecompressOnLoad;
            VisitAllFolderAudio("Resources/Audio/Kill", checkAudioFunc);

            option.Check3D = true;
            option.Is3DType = true;
            VisitAllFolderAudio("Resources/Audio/Move", checkAudioFunc);

            option.Check3D = false;
            VisitAllFolderAudio("Resources/Audio/Skill", checkAudioFunc);
            VisitAllFolderAudio("Resources/Audio/BossShow", checkAudioFunc);

            option.Check3D = false;
            option.CheckLoadType = false;
            VisitAllFolderAudio("Resources/Audio/Base", checkAudioFunc);
            VisitAllFolderAudio("Resources/Audio/UI", checkAudioFunc);
        }

        private static bool CheckAudioRule(CheckAudioOption option, AudioClip audio)
        {
            ///先判断LightProbe
            string assetPath = AssetDatabase.GetAssetPath(audio);
            var importor = AssetImporter.GetAtPath(assetPath) as AudioImporter;
            if (importor == null)
            {
                Debug.LogError("Invalid audio path: " + assetPath, audio);
                return false;
            }

            bool comppressBitRateChanged = false;
            bool loadTypeChanged = false;
            bool threeDChanged = false;
            bool changed = false;
            if (option.CheckLoadType)
            {
                var defaultSetting = importor.defaultSampleSettings;
                if (defaultSetting.loadType != option.LoadType)
                {
                    loadTypeChanged = true;
                    changed = true;

                    defaultSetting.loadType = option.LoadType;
                    importor.defaultSampleSettings = defaultSetting;
                }
            }

            /*
            if (option.SetComporessBitRate > 0)
            {
                var defaultSetting = importor.defaultSampleSettings;
                if (defaultSetting.sampleRateSetting != AudioSampleRateSetting.OverrideSampleRate ||
                    defaultSetting.sampleRateOverride != option.SetComporessBitRate)
                {
                    comppressBitRateChanged = true;
                    changed = true;
                    defaultSetting.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate;
                    defaultSetting.sampleRateOverride = (uint)option.SetComporessBitRate;
                    importor.defaultSampleSettings = defaultSetting;
                }
            }*/

            if (option.Check3D)
            {
                if (importor.threeD != option.Is3DType)
                {
                    importor.threeD = option.Is3DType;
                    changed = true;
                    threeDChanged = true;
                }
            }

            if (changed)
            {
                if (loadTypeChanged)
                {
                    BLogger.Error("audio load type changed: " + assetPath, audio);
                }
                if (comppressBitRateChanged)
                {
                    BLogger.Error("audio bitrate changed: " + assetPath, audio);
                }
                if (threeDChanged)
                {
                    BLogger.Error("audio 3D type changed: " + assetPath, audio);
                }

                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            }

            return true;
        }

        #endregion


        #region SceneTexture
        private static void VisitAllFolderTexture(string folderPath, Action<Texture2D> procAction)
        {
            System.IO.DirectoryInfo rootDir = new System.IO.DirectoryInfo(Application.dataPath + "/" + folderPath);
            System.IO.FileInfo[] files = null;
            files = rootDir.GetFiles("*.*", System.IO.SearchOption.AllDirectories);
            int currIndex = 0;
            foreach (var fileInfo in files)
            {
                string path1 = fileInfo.FullName.Replace("\\", "/").Replace("//", "/");
                path1 = path1.Substring(path1.IndexOf("Assets"));

                EditorUtility.DisplayProgressBar("正在检查", path1, (float)currIndex / (float)files.Length);
                currIndex++;

                var assetObject = AssetDatabase.LoadAssetAtPath(path1, typeof(Texture2D)) as Texture2D;
                int index = 0;

                if (assetObject != null)
                {
                    procAction(assetObject);
                }
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }

        private static bool CheckSceneTexture(CheckTextureOption option, Texture2D tex)
        {
            var texPath = AssetDatabase.GetAssetPath(tex);
            TextureImporter import = AssetImporter.GetAtPath(texPath) as TextureImporter;

            bool changed = false;
            if (option.CheckTextureSize && import.maxTextureSize > option.MaxTexutreSize)
            {
                import.maxTextureSize = option.MaxTexutreSize;
                changed = true;

                Debug.LogError("modify texture size to 512: " + texPath, tex);
            }

            if (option.CheckTextureFormat && import.textureFormat != option.TextureFormat)//TextureImporterFormat.AutomaticTruecolor)
            {
                import.textureFormat = option.TextureFormat;
                Debug.LogError("modify texture " + texPath + " to format: " + option.TextureFormat, tex);
                changed = true;
            }

            if (option.CheckTextureMipMap && import.mipmapEnabled != option.TextureMipMap)
            {
                import.mipmapEnabled = option.TextureMipMap;
                changed = true;

                Debug.LogError("modify texture " + texPath + " mipmap: " + option.TextureMipMap, tex);
            }

            if (option.CheckReadWrite && import.isReadable != option.IsReadWrite)
            {
                import.isReadable = option.IsReadWrite;
                changed = true;
            }

            if (changed)
            {
                AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceUpdate);
            }

            return true;
        }

        [MenuItem("ResourceRuleCheck/检查场景贴图规则")]
        private static void CheckAllSceneTexture()
        {
            bool checkPass = true;
            int checkAudioCnt = 0;
            var option = new CheckTextureOption();
            Action<Texture2D> checkTexFunc = (texture) =>
            {
                checkAudioCnt++;
                if (!CheckSceneTexture(option, texture))
                {
                    checkPass = false;
                }
            };

            option.CheckTextureFormat = true;
            option.TextureFormat = TextureImporterFormat.ARGB16;
            option.CheckReadWrite = true;
            option.IsReadWrite = false;
            VisitAllFolderTexture("T4MOBJ/Terrains/Texture", checkTexFunc);

            if (checkPass)
            {
                Debug.LogError("check scene texture finished");
            }
        }
        
        #endregion

    }
}

