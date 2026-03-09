using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace DodGame
{
    class GpuSkinnedMeshData
    {
        public Animator m_animator;
        public SkinnedMeshRenderer[] m_renderers;
        public Transform[] m_bones;
        public Matrix4x4[] m_bindPose;
        public GpuAnimMeshData[] m_meshData;

        private string m_modifyModelImportAssetPath = null;
        private GameObject m_go;
        public string m_assetPath;
        private AnimatorOverrideController m_overrideController;
        private Dictionary<string, Transform> m_dummys = new Dictionary<string, Transform>();
        public GameObject GoPrefab
        {
            get { return AssetDatabase.LoadAssetAtPath<GameObject>(m_assetPath); }
        }

        public bool IsValidMesh
        {
            get
            {
                return m_go != null && m_renderers != null &&
                m_renderers.Length > 0 && m_bones != null && m_bindPose.Length > 0;
            }
        }

        void CloneAnimator(GameObject src, GameObject dest)
        {
            var srcAnim = src.GetComponent<Animator>();
            var destAnim = dest.GetComponent<Animator>();
            if(destAnim == null)
            {
                destAnim = dest.AddComponent<Animator>();
            }
            if (srcAnim != null)
            {
                destAnim.runtimeAnimatorController = srcAnim.runtimeAnimatorController;
            }
        }

        GameObject InstanceBakGo(GameObject prefab, out string modifyModelImportAssetPath)
        {
            modifyModelImportAssetPath = null;
            GameObject resultGo = null;
            var sourceGo = GameObject.Instantiate(prefab);
            var skinnedMeshRender = sourceGo.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (skinnedMeshRender != null && skinnedMeshRender.sharedMesh != null)
            {
                var meshAssetPath = AssetDatabase.GetAssetPath(skinnedMeshRender.sharedMesh);
                if (!string.IsNullOrEmpty(meshAssetPath))
                {
                    var modelImporter = ModelImporter.GetAtPath(meshAssetPath) as ModelImporter;

                    if (modelImporter.optimizeGameObjects)
                    {
                        modelImporter.optimizeGameObjects = false;
                        modelImporter.SaveAndReimport();
                        modifyModelImportAssetPath = meshAssetPath;
                    }

                    if (!modelImporter.isReadable)
                    {
                        modelImporter.isReadable = true;
                        modelImporter.SaveAndReimport();
                    }

                    AssetDatabase.Refresh();
                    var instGo = AssetDatabase.LoadAssetAtPath<GameObject>(meshAssetPath);
                    var newInstGo = GameObject.Instantiate(instGo);
                    newInstGo.name = sourceGo.name;
                    CloneAnimator(sourceGo, newInstGo);

                    resultGo =  newInstGo;
                }
            }

            GameObject.DestroyImmediate(sourceGo);
            return resultGo;
        }

        public bool TryGetDummy(string dummyName, out Transform dummy)
        {
            return m_dummys.TryGetValue(dummyName, out dummy);
        }

        public bool HasDummy(string dummyName)
        {
            return m_dummys.ContainsKey(dummyName);
        }

        void InitDummys()
        {
            m_dummys.Clear();
            if (m_go != null)
            {
                Queue<Transform> stack = new Queue<Transform>();
                stack.Enqueue(m_go.transform);
                while (stack.Count > 0)
                {
                    var trans = stack.Dequeue();
                    m_dummys.Add(trans.name, trans);
                    for (int i = 0; i < trans.childCount; i++)
                    {
                        stack.Enqueue(trans.GetChild(i));
                    }
                }
            }
        }
        
        public GpuSkinnedMeshData(string assetPath)
        {
            m_assetPath = assetPath;
            var goPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (goPrefab != null)
            {
                m_go = InstanceBakGo(goPrefab, out m_modifyModelImportAssetPath);
                if (m_go == null)
                {
                    BLogger.EditorFatal("invalid skinned mesh data: {0}", assetPath);
                }

                InitDummys();
                //m_go = GameObject.Instantiate(goPrefab);
                m_go.transform.position = Vector3.zero;
                m_go.transform.rotation = Quaternion.identity;
                m_go.transform.localScale = Vector3.one;

                m_animator = m_go.GetComponentInChildren<Animator>();
                if (m_animator != null)
                {
                    m_animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                }

                //需要用一个单一的状态机来烘图 不然两个动画CrossFade会造成烘出的动画帧贴图有问题
                m_overrideController = GameObject.Instantiate<AnimatorOverrideController>(AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>("Assets/Editor/ModelEditor/Res/GpuAnimBakeOverrideCtrl.overrideController"));
                m_animator.runtimeAnimatorController = m_overrideController;
                m_renderers = m_go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                if (m_renderers != null)
                {
                    GpuSkinnedMeshUtil.MergeBone(m_renderers, out m_bones, out m_bindPose, out m_meshData);

                    if (m_bindPose != null && m_bindPose.Length <= 0)
                    {
                        BLogger.Error("Invalid BonePos");
                    }
                }
                else
                {
                    BLogger.Error("Invalid SkinnedMesh");
                }
            }
        }

        public void Destroy()
        {
            if (m_go != null)
            {
                GameObject.DestroyImmediate(m_overrideController);
                GameObject.DestroyImmediate(m_go);
                m_go = null;

                if (!string.IsNullOrEmpty(m_modifyModelImportAssetPath))
                {
                    var modelImport = ModelImporter.GetAtPath(m_modifyModelImportAssetPath) as ModelImporter;
                    modelImport.optimizeGameObjects = true;
                    modelImport.SaveAndReimport();

                    m_modifyModelImportAssetPath = null;
                }
            }
        }

        public void BeginPlayAnimation(AnimationClip clip)
        {
            //重置normalizedTime 不然非Loop的AnimationClip会只有一帧
            m_animator.Play("Idle", 0, 0f);
            m_overrideController["idle"] = clip;
        }

        /// <summary>
        /// 设置动画到某一时间片
        /// </summary>
        /// <param name="clip"></param>
        /// <param name="time"></param>
        public void BeginPlayAnimation(string clipStateName)
        {
            m_animator.enabled = false;
            m_animator.enabled = true;
            m_animator.Play(clipStateName);
        }

        public void StepAnimator(float stepTime)
        {
            m_animator.Update(stepTime);
        }
    }

    /// <summary>
    /// 复杂统一管理需要烘培的mesh等信息
    /// </summary>
    class GpuSkinnedMeshCache : BSingleton<GpuSkinnedMeshCache>
    {
        public List<GpuSkinnedMeshData> m_cacheMeshData = new List<GpuSkinnedMeshData>();

        /// <summary>
        /// 每次新的烘培重置下
        /// </summary>
        public void Reset()
        {
            foreach (var cacheData in m_cacheMeshData)
            {
                cacheData.Destroy();
            }

            m_cacheMeshData.Clear();
        }

        public GpuSkinnedMeshData GetMeshData(string assetPath)
        {
            var cacheData = m_cacheMeshData.Find((item) => { return item.m_assetPath == assetPath; });
            if (cacheData == null)
            {
                cacheData = LoadCache(assetPath);
            }

            return cacheData;
        }


        public GpuSkinnedMeshData LoadCache(string assetPath)
        {
            GpuSkinnedMeshData cacheData = new GpuSkinnedMeshData(assetPath);
            if (cacheData.IsValidMesh)
            {
                m_cacheMeshData.Add(cacheData);
                return cacheData;
            }
            else
            {
                cacheData.Destroy();
            }

            return null;
        }
    }
}
