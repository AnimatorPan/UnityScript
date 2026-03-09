using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;

namespace DodGame
{
    class GpuAnimClipTask
    {
        public AnimationClip m_clip;
        public string m_animStateName;
        public float m_animStateSpeed;
        public GpuAnimClipData m_gpuData = new GpuAnimClipData();
        public GpuAnimBoneInfoData m_boneInfoData;
        public GpuAnimSourceAssetConfig m_srcConfig;
        public Material m_gpuMaterial;
        public string m_clipPath;
        public Color[] m_animFrameData;

        public GpuAnimClipTask(GpuAnimSourceAssetConfig srcConfig, AnimationClip clip, string animStateName, float animStateSpeed)
        {
            m_clip = clip;
            m_srcConfig = srcConfig;
            m_clipPath = AssetDatabase.GetAssetPath(clip);
            m_animStateName = animStateName;
            m_animStateSpeed = animStateSpeed;
            

        }

        public void StartBak()
        {
            m_gpuData = new GpuAnimClipData();
            m_gpuData.m_frameCnt = (int)(m_srcConfig.m_fps * m_clip.length + 0.5f) + 1;
            m_gpuData.m_assetPath = m_clipPath;
            m_gpuData.m_animName = Path.GetFileNameWithoutExtension(m_clipPath);
            m_gpuData.m_animStateName = m_animStateName;
            m_gpuData.m_speed = m_animStateSpeed;

            m_boneInfoData = new GpuAnimBoneInfoData();
            m_boneInfoData.m_animName= Path.GetFileNameWithoutExtension(m_clipPath);

            BakeAnimator();
        }

        void BakeEvent()
        {
            var events = AnimationUtility.GetAnimationEvents(m_clip);
            m_boneInfoData.m_eventData.Clear();
            var frameRate = 1 / 30.0f;
            foreach (var evt in events)
            {
                var data = new AnimationClipEventData()
                {
                    m_frameCount = Mathf.RoundToInt(evt.time / frameRate),
                    m_FunctionName = evt.functionName,
                    m_FloatParameter = evt.floatParameter,
                    m_IntParameter = evt.intParameter,
                    m_StringParameter = evt.stringParameter
                };
                m_boneInfoData.m_eventData.Add(data);
            }
        }
        
        
        void BakeAnimator()
        {
            BakeEvent();
            ///计算骨骼和每一帧占用的数据
            var meshData = GpuSkinnedMeshCache.Instance.GetMeshData(m_srcConfig.m_srcAssetPath);
            if (meshData == null)
            {
                BLogger.Error("load mesh failed:{0}", m_srcConfig.m_srcAssetPath);
                return;
            }

            ///骨骼数
            int boneCount = meshData.m_bones.Length;
            ///每一帧占用的像素数
            int framePixelCount = boneCount * 4;
            int totalPixelCount = framePixelCount * m_gpuData.m_frameCnt;
            m_animFrameData = new Color[totalPixelCount];

            m_gpuData.m_framePixelCount = framePixelCount;
            m_gpuData.m_boneCount = boneCount;
            m_gpuData.m_loop = m_clip.isLooping;
            m_gpuData.m_length = m_clip.length;

            ///循环每一帧，开始烘培对象
            float stepTime = m_gpuData.m_frameCnt > 1 ? (m_clip.length / (m_gpuData.m_frameCnt - 1)) : 1;

            ///初始化动画状态
            meshData.BeginPlayAnimation(m_clip);
            GpuBakerMeshDebug.Instance.BegDebugMesh(meshData, m_srcConfig.m_animTexName, m_clip.name, m_srcConfig.m_fps);

            var localVerts = meshData.m_renderers[0].sharedMesh.vertices;
            meshData.StepAnimator(0f);
            Dictionary<string, Transform> m_dummyName2Trans = new Dictionary<string, Transform>();
            
            var boneNames = m_boneInfoData.m_boneNames;
            boneNames.Clear();
            // var cnt = 0;
            // cnt += AvatarDefine.m_needRecordBoneInfo.Count;
            foreach(var info in AvatarDefine.m_needRecordBoneNames)
            {
                if (meshData.HasDummy(info) && !boneNames.Contains(info))
                {
                    boneNames.Add(info);
                }
            }
            for (int i = 0; i < m_srcConfig.m_etrBoneNum; i++)
            {
                var extrBoneName = m_srcConfig.m_etrBoneName[i];
                
                if (!string.IsNullOrEmpty(extrBoneName) && meshData.HasDummy(extrBoneName)&& !boneNames.Contains(extrBoneName))
                {
                    boneNames.Add(extrBoneName);   
                }
            }

            if (m_srcConfig.m_additionBOnes != null)
            {
                for (int i = 0; i < m_srcConfig.m_additionBOnes.Length; i++)
                {
                    var additionBone = m_srcConfig.m_additionBOnes[i];
                    if (!string.IsNullOrEmpty(additionBone) && meshData.HasDummy(additionBone)&& !boneNames.Contains(additionBone))
                    {
                        boneNames.Add(additionBone);   
                    }
                }
            }
            ///每个骨骼占用4个像素
            int framePixelIndex = 0;
            for (int i = 0; i < m_gpuData.m_frameCnt; i++)
            {
                /**
                 * 调试mesh纯烘培的效果
                 */
                var mesh = new Mesh();
                mesh.name = string.Format("{0}_{1}", m_clip.name, i); 
                meshData.m_renderers[0].BakeMesh(mesh);

                var perFrameBoneInfo = new PerFrameBoneInfo();
                perFrameBoneInfo.m_frame = i;

                perFrameBoneInfo.m_boneOffset = GpuBoneInfoUtil.CalculateBoneInfo(meshData, boneNames, meshData.m_bones);
                m_boneInfoData.m_boneInfo.Add(perFrameBoneInfo);
                ///开始计算骨骼的烘培数据
                var boneMatrix = GpuSkinnedMeshUtil.CalculateSkinMatrix(meshData.m_bones, meshData.m_bindPose, Matrix4x4.identity, false);

                var skinVerts = CalSkinnedMeshVertDebug(meshData, meshData.m_renderers[0].sharedMesh, boneMatrix, localVerts);
                GpuBakerMeshDebug.Instance.AddDebugMesh(meshData, m_srcConfig.m_animTexName, mesh, skinVerts, m_clip.name);

                GpuSkinnedMeshUtil.Convert2Color(m_animFrameData, ref framePixelIndex, boneMatrix, meshData.m_bones.Length);

                meshData.StepAnimator(stepTime);
            }

            BLogger.Assert(framePixelIndex == m_animFrameData.Length);
        }

        Vector3[] CalSkinnedMeshVertDebug(GpuSkinnedMeshData meshData, Mesh sharedSkinMesh, Matrix4x4[] boneMatrix, Vector3[] localVerts)
        {
            Transform[] bonePose = meshData.m_bones;
            Matrix4x4[] bindPoses = meshData.m_bindPose;

            var allBoneWeight = sharedSkinMesh.boneWeights;
            var skinnedVerts = new Vector3[localVerts.Length];
            for (int i = 0; i < localVerts.Length; i++)
            {
                var boneWeight = allBoneWeight[i];
                var boneIndex = boneWeight.boneIndex0;

#if false
                var boneTrans = bonePose[boneIndex];
                var bindPose = bindPoses[boneIndex];

                var matrix = boneTrans.localToWorldMatrix* bindPose;
#else
                var matrix = boneMatrix[boneIndex];
#endif  

                skinnedVerts[i] = matrix.MultiplyPoint(localVerts[i]);
            }

            return skinnedVerts;
        }
    }



    class GpuAnimBakeTask
    {
        private GpuAnimSourceAssetConfig m_assetConfig;
        private GPUAnimParam m_param;
        private List<GpuAnimClipTask> m_listAnimClip = new List<GpuAnimClipTask>();
        //private BoneConfig m_boneConfig;
        private int m_currClipIndex = 0;

        private GpuAnimClipTask CurrClipTask
        {
            get
            {
                if (m_currClipIndex >= 0 && m_currClipIndex < m_listAnimClip.Count)
                {
                    return m_listAnimClip[m_currClipIndex];
                }

                return null;
            }
        }

        public float Progress
        {
            get { return m_listAnimClip.Count > 0 ? (float)m_currClipIndex / m_listAnimClip.Count : 0; }
        }

        public GpuAnimBakeTask(GpuAnimSourceAssetConfig assetConfig, GPUAnimParam param)
        {
            m_assetConfig = assetConfig;
            SetParam(param);
            CollectAnimationClip();
        }

        public void SetParam(GPUAnimParam param)
        {
            m_param = param;
        }

        public void Step()
        {
            if (IsFinish())
            {
                return;
            }

            var currClip = CurrClipTask;
            BLogger.Assert(currClip != null);

            currClip.StartBak();
            m_currClipIndex++;

            if (IsFinish())
            {
                SaveGpuData();
                SaveBoneInfo();
                //SaveSourcePrefab();
                SaveGpuAnimPrefab();
                //SaveAnimShapePrefab();
                GpuSkinnedMeshCache.Instance.Reset();
            }
        }

        private void SaveGpuData()
        {
            string path = "Assets/Resources/GpuInst/anim/" + m_assetConfig.m_animTexName + ".asset";
            var scriptGo = AssetDatabase.LoadAssetAtPath<GpuAnimTexScriptObject>(path);
            if (scriptGo == null)
            {
                var dir = System.IO.Path.GetDirectoryName(path);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                scriptGo = ScriptableObject.CreateInstance<GpuAnimTexScriptObject>();
                AssetDatabase.CreateAsset(scriptGo, path);
            }

            scriptGo.m_data = ComputeAnimTexData();

            ///绑定动画贴图到调试模块里
            GpuBakerMeshDebug.Instance.BindAnimTexData(m_assetConfig.m_animTexName, scriptGo.m_data);

            ///存档
            EditorUtility.SetDirty(scriptGo);
            AssetDatabase.SaveAssets();
        }

        private void SaveBoneInfo()
        {
            var dir = "Assets/Resources/GpuInst/boneInfo/";
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            string path = dir + m_assetConfig.m_boneInfoTexName + ".asset";
            var scriptGo = AssetDatabase.LoadAssetAtPath<GpuBoneInfoScriptObject>(path);
            if (scriptGo == null)
            {
                scriptGo = ScriptableObject.CreateInstance<GpuBoneInfoScriptObject>();
                AssetDatabase.CreateAsset(scriptGo, path);
            }

            scriptGo.m_allBoneInfo = ComputeBoneInfoTexData();
            scriptGo.m_allBoneInfo.AnimatorCtrlData = m_BakedAnimatorData;

            ///绑定动画贴图到调试模块里
            GpuBakerMeshDebug.Instance.BindBoneInfoTexData(m_assetConfig.m_animTexName, scriptGo.m_allBoneInfo);

            ///存档
            EditorUtility.SetDirty(scriptGo);
            AssetDatabase.SaveAssets();
        }

        private GpuAnimBoneInfoTexData ComputeBoneInfoTexData()
        {
            GpuAnimBoneInfoTexData  data=new GpuAnimBoneInfoTexData();
            foreach (var animClipTask in m_listAnimClip)
            {
                data.m_animBoneInfo.Add(animClipTask.m_boneInfoData);
            }
            return data;
        }

        private GameObject GetSourcePrefab()
        {
            var mainAssetConfig = m_assetConfig.m_srcAssetPath;
            var goPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_assetConfig.m_srcAssetPath);
            return goPrefab;
        }

        private string GetSourceResPath()
        {
            return CommHelper.FormatResourcePath(m_assetConfig.m_srcAssetPath);
        }

#if false
        private void SaveSourcePrefab()
        {
            var goPrefab = GetSourcePrefab();
            var goInst = PrefabUtility.InstantiatePrefab(goPrefab) as GameObject;

            var sourceScript = goInst.GetComponent<AnimSourceConfig>();
            if (sourceScript == null)
            {
                sourceScript = goInst.AddComponent<AnimSourceConfig>();
            }

            sourceScript.m_listAnim.Clear();
            foreach (var animClipTask in m_listAnimClip)
            {
                var clipData = new AnimSourceClipData();
                clipData.m_animName = animClipTask.m_clip.name;
                clipData.m_animStateName = animClipTask.m_animStateName;
                clipData.m_fps = animClipTask.m_srcConfig.m_fps;
                clipData.m_frameCount = animClipTask.m_gpuData.m_frameCnt;
                clipData.m_loop = animClipTask.m_gpuData.m_loop;

                sourceScript.m_listAnim.Add(clipData);
            }

            var allRendedrs = goInst.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (allRendedrs == null || allRendedrs.Length <= 0)
            {
                BLogger.Error("Invalid skinned mesh render: {0}", AssetDatabase.GetAssetPath(goPrefab));
                Abortion();
                return;
            }

            var listRender = new List<SkinnedMeshRenderer>();
            listRender.AddRange(allRendedrs);

            for (int i = 0; i < listRender.Count; i++)
            {
                var render = listRender[i];
                render.gameObject.SetActive(false);
            }

            listRender.Sort((t1, t2) => { return t1.sharedMesh.vertexCount - t2.sharedMesh.vertexCount; });
            int index = 0;
            sourceScript.m_bakMeshMesh = listRender[index];
            if (index + 1 < listRender.Count)
            {
                index++;
            }

            sourceScript.m_bakGpuMesh = listRender[index];

            /////填充贴图
            //int texCnt = 2;
            //index = 0;
            //sourceScript.m_sharedTexture = new Texture2D[texCnt];
            //// 己方阵营，蓝色
            //var sourceTex = listRender[index].sharedMaterial.mainTexture;
            //if (!sourceTex.name.Contains("_b_"))
            //{
            //    BLogger.Error("texture name format[xx_b_xx] error: {0}", sourceTex.name);
            //    Abortion();
            //    return;
            //}

            //sourceScript.m_sharedTexture[index++] = sourceTex as Texture2D;
            //// 敌对阵营
            //var nextTexPath = AssetDatabase.GetAssetPath(sourceTex).Replace("_b_", "_r_");
            //var nextTex = AssetDatabase.LoadAssetAtPath<Texture2D>(nextTexPath);
            //if (nextTex != null)
            //{
            //    sourceScript.m_sharedTexture[index] = nextTex;
            //}
            //else
            //{
            //    BLogger.Error("cannot find texture path: {0}", nextTexPath);
            //    sourceScript.m_sharedTexture[index] = sourceTex as Texture2D;
            //}

            PrefabUtility.ReplacePrefab(goInst, goPrefab, ReplacePrefabOptions.ConnectToPrefab);
            GameObject.DestroyImmediate(goInst);
        }
#else

        private void SaveSourcePrefab()
        {
            var destPath = GetSourcePathWithoutExtension() + "_source.prefab";

            var goPrefab = GetSourcePrefab();
            var goInst = GameObject.Instantiate<GameObject>(goPrefab as GameObject);
            var sourceScript = goInst.GetComponent<AnimSourceConfig>();
            if (sourceScript == null)
            {
                sourceScript = goInst.AddComponent<AnimSourceConfig>();
            }

            sourceScript.m_listAnim.Clear();
            foreach (var animClipTask in m_listAnimClip)
            {
                var clipData = new AnimSourceClipData();
                clipData.m_animName = animClipTask.m_clip.name;
                clipData.m_animStateName = animClipTask.m_animStateName;
                clipData.m_fps = animClipTask.m_srcConfig.m_fps;
                clipData.m_frameCount = animClipTask.m_gpuData.m_frameCnt;
                clipData.m_loop = animClipTask.m_gpuData.m_loop;

                sourceScript.m_listAnim.Add(clipData);
            }

            var allRendedrs = goInst.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (allRendedrs == null || allRendedrs.Length <= 0)
            {
                BLogger.Error("Invalid skinned mesh render: {0}", AssetDatabase.GetAssetPath(goPrefab));
                Abortion();
                return;
            }

            var listRender = new List<SkinnedMeshRenderer>();
            listRender.AddRange(allRendedrs);

            for (int i = 0; i < listRender.Count; i++)
            {
                var render = listRender[i];
                render.gameObject.SetActive(false);
            }

            listRender.Sort(GpuAnimMgr.SortSkinnedMeshRenderer);
            int index = 0;
            sourceScript.m_bakMeshMesh = listRender[index];
            if (index + 1 < listRender.Count)
            {
                index++;
            }

            sourceScript.m_bakGpuMesh = listRender[index];
            sourceScript.m_arrRenderer = listRender.ToArray();


            goPrefab = PrefabUtility.SaveAsPrefabAssetAndConnect(goInst, destPath, InteractionMode.AutomatedAction);
            EditorUtility.SetDirty(goPrefab);
            AssetDatabase.SaveAssets();

            GameObject.DestroyImmediate(goInst);
        }
#endif

        private string GetFullPathWithoutExtension(string path)
        {
            var dir = Path.GetDirectoryName(path);
            var fileName = Path.GetFileNameWithoutExtension(path);
            return dir + "/" + fileName;
        }

        private string GetSourcePathWithoutExtension()
        {
            return GetFullPathWithoutExtension(m_assetConfig.m_srcAssetPath);
        }

        public static string[] colorProperties = new[]
        {
            "_ShadowColor",
            "_ShadowColor2",
            "_SpecColor",
            "_OutLineColor"
        };

        public static  string[] floatProperties = new string[]
        {
            "_ShadowSoftRange",
            "_Cutoff",
            "_Smoothness",
            "_OcclusionStrength",
            "_Metallic",
            "_GIStrength",
            "_BumpScale",
            "_Outline_Width",
            "_Offset_Z"
        };

        public static  string[] tex2DProperties = new[]
        {
            "_BaseMap",
            "_MetallicGlossMap",
            "_BumpMap",
            "_MatCapTex"
        };

        private Material LoadOrCreateGpuAnimMat()
        {
            if(m_assetConfig.gpuAnimMaterial != null)
            {
                return m_assetConfig.gpuAnimMaterial;
            }
            var sourcePrefab = GetSourcePrefab();
            var sourceGo = GameObject.Instantiate(sourcePrefab);
            
            // 获取所有Renderer，找到第一个有材质的
            var allRenderers = sourceGo.GetComponentsInChildren<Renderer>(true);
            Material sourceMat = null;
            foreach (var renderer in allRenderers)
            {
                if (renderer.sharedMaterial != null)
                {
                    sourceMat = renderer.sharedMaterial;
                    Debug.Log($"找到原始材质: {sourceMat.name} 来自 {renderer.gameObject.name}");
                    break;
                }
            }
            
            if (sourceMat == null)
            {
                Debug.LogError("没有找到任何带有材质的Renderer");
            }

            GameObject.DestroyImmediate(sourceGo);
            Material newMat = null;
            string newMatPath = string.Empty;
            if (sourceMat != null)
            {
                var sourceMatPath = AssetDatabase.GetAssetPath(sourceMat);
                newMatPath = GetFullPathWithoutExtension(sourceMatPath) + (m_param!= null ? m_param.m_materialNameSuffix : "_gpu_anim.mat");
                newMat = AssetDatabase.LoadAssetAtPath<Material>(newMatPath);
            }
            else
            {
                string fileName = Path.GetFileNameWithoutExtension(m_assetConfig.m_srcAssetPath);
                var dir = Path.GetDirectoryName(m_assetConfig.m_srcAssetPath);
                newMatPath = $"{dir}/Material/{fileName}_gpu_anim.mat";
            }
            
            if (newMat == null)
            {
                var shader = m_param.m_shader != null ? m_param.m_shader : Shader.Find("Dodjoy/Actor/ActorToonGpuMatCap");
                if (sourceMat != null)
                {
                    newMat = new Material(sourceMat);
                    newMat.shader = shader;
                    foreach (var tex in tex2DProperties)
                    {
                        if (sourceMat.HasProperty(tex))
                        {
                            newMat.SetTexture(tex, sourceMat.GetTexture(tex));
                        }
                    }

                    foreach (var propertyName in colorProperties)
                    {
                        if (sourceMat.HasProperty(propertyName))
                        {
                            newMat.SetColor(propertyName, sourceMat.GetColor(propertyName));
                        }
                    }

                    foreach (var propertyName in floatProperties)
                    {
                        if (sourceMat.HasProperty(propertyName))
                        {
                            newMat.SetFloat(propertyName, sourceMat.GetFloat(propertyName));
                        }
                    }
                    
                }
                else
                {
                    newMat = new Material(shader);
                }
                newMat.enableInstancing = false;

                var dir = Path.GetDirectoryName(newMatPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                AssetDatabase.CreateAsset(newMat, newMatPath);
                newMat = AssetDatabase.LoadAssetAtPath<Material>(newMatPath);
            }

            return newMat;
        }

        private void SaveGpuAnimPrefab()
        {
            var destPath = m_param != null ? m_param.m_destPath : string.Empty;
            if (string.IsNullOrEmpty(destPath))
            {
                destPath = GetSourcePathWithoutExtension() + "_gpu_anim.prefab";
            }

            var destDir = Path.GetDirectoryName(destPath);
            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }
            UnityEngine.GameObject goPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(destPath);

            goPrefab = GetSourcePrefab();
            GameObject goInst = GameObject.Instantiate<GameObject>(goPrefab as GameObject);
            goInst.name = Path.GetFileNameWithoutExtension(destPath);

            //删除Animator Component
            var animator = goInst.GetComponent<Animator>();
            GameObject.DestroyImmediate(animator);

            //删除所有SkinnedMeshRenderer
            SkinnedMeshRenderer[] smrs = goInst.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var listSmr = new List<SkinnedMeshRenderer>(smrs);
            //按顶点数排序
            listSmr.Sort(GpuAnimMgr.SortSkinnedMeshRenderer);
            var listRenderer = new List<MeshRenderer>();
            var listFilter = new List<MeshFilter>();
            var listIndex = new List<int>();

            MeshFilter shadowFilter = null;
            MeshRenderer shadowRenderer = null;
            for (int index = 0; index < listSmr.Count; index++)
            {
                var smr = listSmr[index];
                var go = smr.gameObject;
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;

                var meshFilter = go.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = smr.sharedMesh;
                var meshRenderer = go.AddComponent<MeshRenderer>();
                meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                //meshRenderer.receiveShadows = true;

                GameObject.DestroyImmediate(smr);

                if (go.name.ToLower().EndsWith("_shadow"))
                {
                    shadowFilter = meshFilter;
                    shadowRenderer = meshRenderer;
                    if (index != 0)
                    {
                        EditorUtility.DisplayDialog("提示", "生成失败！投影Mesh不是顶点数最少的Mesh", "确定");
                        Debug.LogError("生成失败！投影Mesh不是顶点数最少的Mesh");
                        return;
                    }
                }
                else
                {
                    listRenderer.Add(meshRenderer);
                    listFilter.Add(meshFilter);
                    listIndex.Add(index);
                }
            }

            if (listRenderer.Count > 1)
            {
                var lodGroup = goInst.GetComponent<LODGroup>();
                if (lodGroup == null)
                {
                    lodGroup = goInst.AddComponent<LODGroup>();
                }

                if (listRenderer.Count == 3)
                {
                    lodGroup.SetLODs(new LOD[]
                    {
                            new LOD(0.53f, new Renderer[]{ listRenderer[2] }),
                            new LOD(0.2f, new Renderer[]{ listRenderer[1] }),
                            new LOD(0.03f, new Renderer[]{ listRenderer[0] }),
                    });
                }
                else if (listRenderer.Count == 2)
                {
                    lodGroup.SetLODs(new LOD[]
                    {
                            new LOD(0.53f, new Renderer[]{ listRenderer[1] }),
                            new LOD(0.01f, new Renderer[]{ listRenderer[0] }),
                    });
                }

                foreach (var meshRenderer in listRenderer)
                {
                    meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                }

                if (shadowRenderer != null)
                {
                    shadowRenderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                }
                else
                {
                    //没找到投影Renderer，创建一个单独的Renderer用于投影
                    var shadowGo = new GameObject("Shadow");
                    shadowGo.transform.parent = goInst.transform;
                    shadowGo.transform.localPosition = Vector3.zero;
                    shadowGo.transform.localRotation = Quaternion.identity;
                    shadowGo.transform.localScale = Vector3.one;
                    shadowFilter = shadowGo.AddComponent<MeshFilter>();
                    shadowRenderer = shadowGo.AddComponent<MeshRenderer>();
                    shadowRenderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                }
            }
            else if (listRenderer.Count == 1)
            {
                if (shadowRenderer != null)
                {
                    listRenderer[0].shadowCastingMode = ShadowCastingMode.Off;
                    shadowRenderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                }
                else
                {
                    listRenderer[0].shadowCastingMode = ShadowCastingMode.On;
                }
            }
            else
            {
                EditorUtility.DisplayDialog("提示", "生成失败！没有渲染 Renderer", "确定");
                Debug.LogError("生成失败！没有渲染 Renderer");
                return;
            }

            if (shadowRenderer != null)
            {
                listRenderer.Insert(0, shadowRenderer);
                listFilter.Insert(0, shadowFilter);
                listIndex.Insert(0, 0);
            }

            var animRender = goInst.GetComponent<GpuAnimRenderer>();
            if (animRender == null)
            {
                animRender = goInst.AddComponent<GpuAnimRenderer>();
            }

            animRender.m_animTexName = m_assetConfig.m_animTexName;
            animRender.m_boneInfoTexName = m_assetConfig.m_boneInfoTexName;
            animRender.m_sourceName = GetSourceResPath();
            //animRender.m_render = goInst.GetComponentInChildren<MeshRenderer>();
            //animRender.m_filter = goInst.GetComponentInChildren<MeshFilter>();
            animRender.m_listRender = listRenderer;
            //animRender.m_listFilter = listFilter;
            //animRender.m_listIndex = listIndex;
            //animRender.AnimatorCtrlData = m_BakedAnimatorData;
            //生成gpu的材质
            animRender.m_mat = LoadOrCreateGpuAnimMat();
            
            // 确保材质创建成功
            if (animRender.m_mat == null)
            {
                Debug.LogError("LoadOrCreateGpuAnimMat 返回null，尝试使用默认shader创建材质");
                var defaultShader = Shader.Find("Dodjoy/Actor/ActorToonGpuSkin");
                if (defaultShader != null)
                {
                    animRender.m_mat = new Material(defaultShader);
                    string defaultMatPath = Path.Combine(destDir, "DefaultGpuAnim.mat");
                    AssetDatabase.CreateAsset(animRender.m_mat, defaultMatPath);
                    animRender.m_mat = AssetDatabase.LoadAssetAtPath<Material>(defaultMatPath);
                }
            }
            
            // 确保所有Renderer都获得材质，包括通过GetComponentsInChildren获取的
            var allMeshRenderers = goInst.GetComponentsInChildren<MeshRenderer>(true);
            Debug.Log($"找到 {allMeshRenderers.Length} 个 MeshRenderer，准备赋予材质");
            foreach (var meshRenderer in allMeshRenderers)
            {
                if (animRender.m_mat != null)
                {
                    meshRenderer.sharedMaterial = animRender.m_mat;
                    EditorUtility.SetDirty(meshRenderer);  // 标记Renderer为已修改
                    Debug.Log($"赋予材质给: {meshRenderer.gameObject.name}");
                }
                else
                {
                    Debug.LogError($"材质为null，无法赋予给: {meshRenderer.gameObject.name}");
                }
            }
            
            // 标记GpuAnimRenderer为已修改
            EditorUtility.SetDirty(animRender);
            EditorUtility.SetDirty(goInst);

            var sourceCfg = goInst.GetComponent<AnimSourceConfig>();
            if (sourceCfg)
            {
                GameObject.DestroyImmediate(sourceCfg);
            }

            // var shaderConfig = goInst.GetComponent<ShaderConfig>();
            // if (shaderConfig)
            // {
            //     shaderConfig.CollectMesh();
            // }
            //
            // var animatorConfig = goInst.GetComponent<AnimatorConfig>();
            // if (animatorConfig)
            // {
            //     GameObject.DestroyImmediate(animatorConfig);
            // }
            //
            // var poolBehaviour = goInst.GetComponent<GoPoolBehaviourItem>();
            // if (poolBehaviour)
            // {
            //     poolBehaviour.EditorRefresh();
            // }

            // 先保存资源再保存Prefab
            AssetDatabase.SaveAssets();
            
            goPrefab = PrefabUtility.SaveAsPrefabAssetAndConnect(goInst, destPath, InteractionMode.AutomatedAction);
            if (goPrefab != null)
            {
                EditorUtility.SetDirty(goPrefab);
                Debug.Log($"Prefab保存成功: {destPath}");
            }
            else
            {
                Debug.LogError($"Prefab保存失败: {destPath}");
            }
            //PrefabUtility.SaveAsPrefabAssetAndConnect(goInst, destPath, InteractionMode.AutomatedAction);
            GameObject.DestroyImmediate(goInst);
        }

        private void SaveAnimShapePrefab()
        {
            var destPath = GetSourcePathWithoutExtension() + "_shape.prefab";
            UnityEngine.Object goPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(destPath);

            ///获取source mesh的位置和旋转
            var sourcePrefab = GetSourcePrefab();
            var sourceGo = GameObject.Instantiate(sourcePrefab);
            sourceGo.transform.position = Vector3.zero;
            sourceGo.transform.rotation = Quaternion.identity;
            sourceGo.transform.localScale = Vector3.one;

            var config = sourceGo.GetComponent<AnimSourceConfig>();
            var sourceMeshRender = config.m_bakMeshMesh;
            if (sourceMeshRender == null)
            {
                EditorUtility.DisplayDialog("导出错误", "Prefab 没有Mesh和Render : {0}", GetSourcePathWithoutExtension());
                GameObject.DestroyImmediate(sourceGo);
                return;
            }

            GameObject goInst = null;
            Renderer newMeshRender;
            if (goPrefab == null)
            {
                goPrefab = PrefabUtility.CreateEmptyPrefab(destPath);
                goInst = new GameObject(Path.GetFileNameWithoutExtension(destPath));
                var childMesh = new GameObject("mesh");
                childMesh.transform.parent = goInst.transform;
                childMesh.transform.localPosition = Vector3.zero;
                childMesh.transform.localRotation = Quaternion.identity;
                childMesh.transform.localScale = Vector3.one;

                childMesh.AddComponent<MeshFilter>();
                newMeshRender = childMesh.AddComponent<MeshRenderer>();

                childMesh.transform.localPosition = sourceMeshRender.transform.position;
                childMesh.transform.localRotation = sourceMeshRender.transform.rotation;
                childMesh.transform.localScale = sourceMeshRender.transform.lossyScale;
            }
            else
            {
                goInst = PrefabUtility.InstantiatePrefab(goPrefab) as GameObject;
                newMeshRender = goInst.GetComponentInChildren<Renderer>();
            }

            if (newMeshRender.sharedMaterial == null)
            {
                newMeshRender.sharedMaterial = sourceMeshRender.sharedMaterial;
            }

            GameObject.DestroyImmediate(sourceGo);

            var animRender = goInst.GetComponent<AnimShapeRenderer>();
            if (animRender == null)
            {
                animRender = goInst.AddComponent<AnimShapeRenderer>();
            }

            animRender.m_sourceName = GetSourceResPath();
            animRender.m_filter = goInst.GetComponentInChildren<MeshFilter>();
            animRender.m_render = goInst.GetComponentInChildren<Renderer>();
            PrefabUtility.ReplacePrefab(goInst, goPrefab, ReplacePrefabOptions.ConnectToPrefab);
            GameObject.DestroyImmediate(goInst);
        }

        /// <summary>
        /// 计算需要的贴图数量
        /// </summary>
        /// <param name="totalPixelCount"></param>
        /// <returns></returns>
        private int GetTextureSize(int totalPixelCount)
        {
            for (int i = 4; i < 10; i++)
            {
                int size = 1 << i;
                int dataSize = size * size;
                if (dataSize >= totalPixelCount)
                {
                    return size;
                }
            }

            return -1;
        }

        private GpuAnimTexData ComputeAnimTexData()
        {
            var texData = new GpuAnimTexData();
            var metaData = texData.m_metaData;

            var meshData = GpuSkinnedMeshCache.Instance.GetMeshData(m_assetConfig.m_srcAssetPath);
            if (meshData != null)
            {
                metaData.m_arrMeshData = meshData.m_meshData;
            }

            int totalPixelCount = 0;
            foreach (var animClipTask in m_listAnimClip)
            {
                metaData.m_listClip.Add(animClipTask.m_gpuData);

                if (animClipTask.m_animFrameData != null)
                {
                    totalPixelCount += animClipTask.m_animFrameData.Length;
                }
            }

            if (totalPixelCount <= 0)
            {
                EditorUtility.DisplayDialog("错误", "计算贴图尺寸失败", "ok");
                return null;
            }

            ///计算需要的贴图尺寸
            int texSize = GetTextureSize(totalPixelCount);
            if (texSize <= 0)
            {
                EditorUtility.DisplayDialog("错误", "计算贴图尺寸失败", "ok");
                return texData;
            }

            metaData.m_texSize = texSize;
            metaData.m_texHeight = Mathf.NextPowerOfTwo(totalPixelCount / texSize);
            metaData.m_boneCount = m_listAnimClip[0].m_gpuData.m_boneCount;


            metaData.m_fps = m_assetConfig.m_fps;

            TextureFormat format = TextureFormat.RGBAHalf;
            var texure = new Texture2D(texSize, metaData.m_texHeight, format, false);
            texure.filterMode = FilterMode.Point;

            var colorBuff = new Color[texSize * metaData.m_texHeight];
            int colorIndex = 0;
            foreach (var animClipTask in m_listAnimClip)
            {
                var animFrameData = animClipTask.m_animFrameData;
                var gpuData = animClipTask.m_gpuData;

                gpuData.m_frameStartPixel = colorIndex;
                for (int i = 0; i < animFrameData.Length; i++)
                {
                    colorBuff[colorIndex] = animFrameData[i];
                    colorIndex++;
                }
            }
            //多余的变成0
            for (int i = colorIndex; i < colorBuff.Length; i++)
            {
                colorBuff[i] = new Color(0, 0, 0);
            }

            texure.SetPixels(colorBuff);
            texData.m_animTexData = texure.GetRawTextureData();
            texData.m_texMemSize = texData.m_animTexData.Length;
            GameObject.DestroyImmediate(texure);
            return texData;
        }

        public bool IsFinish()
        {
            return m_currClipIndex >= m_listAnimClip.Count;
        }

        /// <summary>
        /// 立刻结束任务
        /// </summary>
        public void Abortion()
        {
            m_currClipIndex = m_listAnimClip.Count;
        }

        public string CurrClipName
        {
            get { return CurrClipTask.m_clipPath; }
        }

        /// <summary>
        /// 暂时不支持事件，后面需要的时候在加
        /// </summary>
        /// <param name="animSm"></param>
        /// <param name="listClip"></param>
        private void ParseAllAnimClip(GpuAnimSourceAssetConfig srcConfig,
                        AnimatorStateMachine animSm, AnimatorOverrideController overrideCtrl, List<GpuAnimClipTask> listClip)
        {
            var allStates = animSm.states;
            foreach (var state in allStates)
            {
                AnimationClip clip = state.state.motion as AnimationClip;
                if (clip != null)
                {
                    ParseAnimClip(clip, state.state.name,state.state.speed, srcConfig, overrideCtrl, listClip);
                }
                else
                {
                    BlendTree blendTree = state.state.motion as BlendTree;
                    if (blendTree != null)
                    {
                        var childCount = blendTree.children.Length;
                        for (int i = 0; i < childCount; i++)
                        {
                            var child = blendTree.children[i];
                            clip = child.motion as AnimationClip;
                            if (clip != null)
                            {
                                ParseAnimClip(clip, state.state.name, state.state.speed, srcConfig, overrideCtrl, listClip);
                            }
                        }
                    }
                }
            }

            foreach (var subSm in animSm.stateMachines)
            {
                if (subSm.stateMachine == null)
                {
                    continue;
                }

                ParseAllAnimClip(srcConfig, subSm.stateMachine, overrideCtrl, listClip);
            }
        }

        private void ParseAnimClip(AnimationClip clip, string animStateName,float animStateSpeed, GpuAnimSourceAssetConfig srcConfig, AnimatorOverrideController overrideCtrl, List<GpuAnimClipTask> listClip)
        {
            if (overrideCtrl != null)
            {
                var overClip = overrideCtrl[clip];
                if (overClip != null)
                {
                    clip = overClip;
                }
            }

            ///如果已经存在了，那么继续
            if (listClip.Find((item) => { return item.m_clip == clip; }) != null)
            {
                return;
            }

            var clipTask = new GpuAnimClipTask(srcConfig, clip, animStateName, animStateSpeed);
            listClip.Add(clipTask);
        }

        private BakedAnimatorData m_BakedAnimatorData;
        void GenBakeAnimatorCtrlData(AnimatorController controller)
        {
            m_BakedAnimatorData = null;
            if (controller == null)
                return;
            m_BakedAnimatorData = new BakedAnimatorData();

            var allParam = new Dictionary<string, AnimatorControllerParameter>();
            foreach (var p in controller.parameters)
            {
                allParam.Add(p.name, p);
            }
            List<BakedAnimatorState> stateList = new List<BakedAnimatorState>();
            foreach (var layer in controller.layers)
            {
                var states = layer.stateMachine.states;
                foreach (var state in states)
                {
                    var stateData = new BakedAnimatorState();
                    stateData.stateId = state.state.nameHash;
                    var transitions = state.state.transitions;
                    stateData.tranisitions = new GpuAnimatorTransition[transitions.Length];
                    stateList.Add(stateData);
                    for (int i = 0; i < transitions.Length; i++)
                    {
                        var t = transitions[i];
                        var srcCondition = t.conditions;
                        int destStateId = t.destinationState.nameHash;
                        GpuAnimCondition[] conditions = new GpuAnimCondition[srcCondition.Length];
                        float transitionDuration = t.duration;
                        bool fixedDuration = t.hasFixedDuration;
                        bool hasExitTime = t.hasExitTime;
                        float exitTime = t.exitTime;
                        var tData = new GpuAnimatorTransition(destStateId, conditions, transitionDuration, fixedDuration, hasExitTime, exitTime);
                        stateData.tranisitions[i] = tData;
                        tData.m_conditions = new GpuAnimCondition[t.conditions.Length];
                        tData.m_destStateId = t.destinationState.nameHash;
                        for (int j = 0; j < t.conditions.Length; j++)
                        {
                            var condition = t.conditions[j];

                            var value = new GpuAnimConditionParams();
                            value.m_EventTreshold = condition.threshold;
                            if (allParam.TryGetValue(condition.parameter, out var parameter))
                            {
                                switch (parameter.type)
                                {
                                    case AnimatorControllerParameterType.Bool:
                                        value.m_type = GouAnimConditionParamsType.Boolean;
                                        break;
                                    case AnimatorControllerParameterType.Float:
                                        value.m_type = GouAnimConditionParamsType.Float;
                                        break;
                                    case AnimatorControllerParameterType.Int:
                                        value.m_type = GouAnimConditionParamsType.Int;
                                        break;
                                    case AnimatorControllerParameterType.Trigger:
                                        value.m_type = GouAnimConditionParamsType.Trigger;
                                        break;
                                }
                            }

                            value.m_conditionMode = (DodGame.GPUAnimatorConditionMode)condition.mode;
                            var gpuCondition = new GpuAnimCondition(Animator.StringToHash(condition.parameter) , value);
                            tData.m_conditions[j] = gpuCondition;
                        }
                    }
                }
            }


            List<GpuAnimatorTransition> anyTransitions = new List<GpuAnimatorTransition>();
            foreach (var layer in controller.layers)
            {
                var anyStateTransitions = layer.stateMachine.anyStateTransitions;
                foreach (var transition in anyStateTransitions)
                {
                    
                    var destStateId = transition.destinationState.nameHash;
                    GpuAnimCondition[] conditions = new GpuAnimCondition[transition.conditions.Length];
                    for (int j = 0; j < transition.conditions.Length; j++)
                    {
                        var condition = transition.conditions[j];

                        var value = new GpuAnimConditionParams();
                        value.m_EventTreshold = condition.threshold;
                        if (allParam.TryGetValue(condition.parameter, out var parameter))
                        {
                            switch (parameter.type)
                            {
                                case AnimatorControllerParameterType.Bool:
                                    value.m_type = GouAnimConditionParamsType.Boolean;
                                    break;
                                case AnimatorControllerParameterType.Float:
                                    value.m_type = GouAnimConditionParamsType.Float;
                                    break;
                                case AnimatorControllerParameterType.Int:
                                    value.m_type = GouAnimConditionParamsType.Int;
                                    break;
                                case AnimatorControllerParameterType.Trigger:
                                    value.m_type = GouAnimConditionParamsType.Trigger;
                                    break;
                            }
                        }

                        value.m_conditionMode = (DodGame.GPUAnimatorConditionMode)condition.mode;
                        var gpuCondition = new GpuAnimCondition(Animator.StringToHash(condition.parameter) , value);
                        conditions[j] = gpuCondition;
                    }
                    var gpuAnimTransition = new GpuAnimatorTransition(destStateId, conditions, transition.duration, transition.hasFixedDuration, transition.hasExitTime, transition.exitTime);
                    anyTransitions.Add(gpuAnimTransition);
                }
            }
            m_BakedAnimatorData.states = stateList.ToArray();
            m_BakedAnimatorData.any = anyTransitions.ToArray();
        }

        private void CollectAnimationClip()
        {
            m_listAnimClip.Clear();
            var goPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_assetConfig.m_srcAssetPath);
            if (goPrefab == null)
            {
                BLogger.Error("CollectAnimationClip failed, Anim Tex: {0}, source asset path:{1}",
                    m_assetConfig.m_animTexName, m_assetConfig.m_srcAssetPath);
                return;
            }

            var goInst = GameObject.Instantiate(goPrefab);
            var allAnimator = goInst.GetComponentsInChildren<Animator>();
            foreach (var animator in allAnimator)
            {
                AnimatorOverrideController overrideCtrl = null;
                var animCtrl = animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
                if (animCtrl == null)
                {
                    overrideCtrl = animator.runtimeAnimatorController as AnimatorOverrideController;
                    animCtrl = overrideCtrl.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
                }

                if (animCtrl != null)
                {
                    GenBakeAnimatorCtrlData(animCtrl);
                    var animLayers = animCtrl.layers;
                    foreach (var animLayer in animLayers)
                    {
                        var animSm = animLayer.stateMachine;
                        ParseAllAnimClip(m_assetConfig, animSm, overrideCtrl, m_listAnimClip);
                    }
                }
                else
                {
                    BLogger.Error("Invalid animator controller");
                }
            }

            GameObject.DestroyImmediate(goInst);
        }
    }
}
