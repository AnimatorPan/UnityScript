using System.Collections.Generic;
using A9Game;
using UnityEditor;
using UnityEngine;

namespace DodGame
{
    class RagdollUtil
    {
        public static void CalculateDirection(Vector3 point, out int direction, out float distance)
        {
            // Calculate longest axis
            direction = 0;
            if (Mathf.Abs(point[1]) > Mathf.Abs(point[0]))
                direction = 1;
            if (Mathf.Abs(point[2]) > Mathf.Abs(point[direction]))
                direction = 2;

            distance = point[direction];
        }


        public static Vector3 CalculateDirectionAxis(Vector3 point)
        {
            int direction = 0;
            float distance;
            CalculateDirection(point, out direction, out distance);
            Vector3 axis = Vector3.zero;
            if (distance > 0)
                axis[direction] = 1.0F;
            else
                axis[direction] = -1.0F;
            return axis;
        }

        public static int SmallestComponent(Vector3 point)
        {
            int direction = 0;
            if (Mathf.Abs(point[1]) < Mathf.Abs(point[0]))
                direction = 1;
            if (Mathf.Abs(point[2]) < Mathf.Abs(point[direction]))
                direction = 2;
            return direction;
        }

        public static int LargestComponent(Vector3 point)
        {
            int direction = 0;
            if (Mathf.Abs(point[1]) > Mathf.Abs(point[0]))
                direction = 1;
            if (Mathf.Abs(point[2]) > Mathf.Abs(point[direction]))
                direction = 2;
            return direction;
        }


        #region 一些骨骼搜索和操作接口

        public static void AddHeadCollider(Transform head, Transform leftArm, Transform rightArm, Transform pelvis)
        {
            if (head == null)
            {
                return;
            }

            if (head.GetComponent<Collider>())
            {
                Object.DestroyImmediate(head.GetComponent<Collider>());
            }

            float radius = 0;

            radius = Vector3.Distance(leftArm.transform.position, rightArm.transform.position);
            radius /= 4;


            SphereCollider sphere = Undo.AddComponent<SphereCollider>(head.gameObject);
            sphere.radius = radius;
            Vector3 center = Vector3.zero;

            int direction;
            float distance;
            CalculateDirection(head.InverseTransformPoint(pelvis.position), out direction, out distance);
            if (distance > 0)
                center[direction] = -radius;
            else
                center[direction] = radius;
            sphere.center = center;
        }

        public static void CollectBones(Transform boneRoot, Dictionary<string, Transform> dictBones)
        {
            if (!dictBones.ContainsKey(boneRoot.name))
                dictBones.Add(boneRoot.name, boneRoot);
            for (int i = 0; i < boneRoot.childCount; i++)
            {
                CollectBones(boneRoot.GetChild(i), dictBones);
            }
        }

        /// <summary>
        /// 所有子节点查找，同名的子节点
        /// </summary>
        /// <param name="root"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Transform FindChildTransform(Transform root, string name)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child.name == name)
                {
                    return child;
                }

                var findTrans = FindChildTransform(child, name);
                if (findTrans != null)
                {
                    return findTrans;
                }
            }

            return null;
        }

        #endregion


        #region copy接口
        public static bool CloneRagdoll(Transform src, Transform dest)
        {
            if (!CheckRagdollChildSame(src, dest))
            {
                //return false;
            }

            var transMap = new Dictionary<Transform, Transform>();
            BuildSrcTransformMap(transMap, src, dest);
            return DoCloneRagdoll(transMap, src);
        }

        private static void BuildSrcTransformMap(Dictionary<Transform, Transform> map, Transform src, Transform dest)
        {
            for (int i = 0; i < src.childCount; i++)
            {
                var srcChild = src.GetChild(i);
                var destChild = FindMatchChild(dest, srcChild.name);
                if (destChild != null)
                {
                    map[srcChild] = destChild;
                    BuildSrcTransformMap(map, srcChild, destChild);
                }
            }
        }

        private static bool DoCloneRagdoll(Dictionary<Transform, Transform>  transMap, Transform src)
        {
            for (int i = 0; i < src.childCount; i++)
            {
                var srcChild = src.GetChild(i);
                Transform destChild;
                transMap.TryGetValue(srcChild, out destChild);
                if (destChild == null)
                {
                    BLogger.Error("clone child :{0} not exist", srcChild.name);
                    continue;
                }

                CloneCompt(transMap, srcChild.gameObject, destChild.gameObject);

                if (!DoCloneRagdoll(transMap, srcChild))
                {
                    return false;
                }
            }

            return true;
        }

        public static Transform CloneNewRagdoll(Transform src, Transform destParent)
        {
            var destGo = new GameObject(RagdollBoneMapper.RAGDOLL_BONES_ROOT);
            var destTrans = destGo.transform;
            destTrans.parent = destParent;
            destTrans.localPosition = src.localPosition;
            destTrans.localRotation = src.localRotation;

            CreateChildTransform(src, destTrans);
            if (!CloneRagdoll(src, destTrans))
            {
                GameObject.DestroyImmediate(destGo);
                return null;
            }

            return destTrans;
        }

        private static Transform FindMatchChild(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == name)
                {
                    return child;
                }
            }

            return null;
        }

        public static void CreateChildTransform(Transform src, Transform dest)
        {
            for (int i = 0; i < src.childCount; i++)
            {
                var srcChild = src.GetChild(i);
                var newChildGo = new GameObject(srcChild.name);
                newChildGo.transform.parent = dest;

                CreateChildTransform(srcChild, newChildGo.transform);
            }
        }

        /// <summary>
        /// 检查子节点是否是完全一样
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dest"></param>
        /// <returns></returns>
        private static bool CheckRagdollChildSame(Transform src, Transform dest)
        {
            if (src.childCount != dest.childCount)
            {
                BLogger.Error("ragdoll not same: {0},child count: {1}, {2}, child count:{3}", 
                    src.name, src.childCount, dest.name, dest.childCount);
                return false;
            }

            for (int i = 0; i < src.childCount; i++)
            {
                var srcChild = src.GetChild(i);
                var destChild = dest.GetChild(i);
                if (srcChild.name != destChild.name)
                {
                    return false;
                }

                if (!CheckRagdollChildSame(srcChild, destChild))
                {
                    return false;
                }
            }

            return true;
        }

        private static void CloneCompt(Dictionary<Transform, Transform> transMap, GameObject src, GameObject dest)
        {
            var srcCmpts = src.GetComponents<Component>();
            var destCmpts = dest.GetComponents<Component>();

            foreach (var srcCmpt in srcCmpts)
            {
                UnityEditorInternal.ComponentUtility.CopyComponent(srcCmpt);
                var destCmpt = dest.GetComponent(srcCmpt.GetType());
                if (destCmpt != null)
                {
                    UnityEditorInternal.ComponentUtility.PasteComponentValues(destCmpt);
                }
                else
                {
                    UnityEditorInternal.ComponentUtility.PasteComponentAsNew(dest);
                }
                
                if (srcCmpt is Joint)
                {
                    var srcJoint = srcCmpt as Joint;
                    var destCmptJoint = dest.GetComponent(srcCmpt.GetType()) as Joint;
                    Rigidbody destConnectRigid = null;
                    var srcConnectTrans = srcJoint.connectedBody != null ? srcJoint.connectedBody.transform : null;
                    Transform destConnectBodyTrans;
                    transMap.TryGetValue(srcConnectTrans, out destConnectBodyTrans);
                    if (destConnectBodyTrans != null)
                    {
                        destConnectRigid = destConnectBodyTrans.gameObject.GetComponent<Rigidbody>();
                    }

                    destCmptJoint.connectedBody = destConnectRigid;
                }
            }
        }
        
        #endregion
    }

    class RagdollEditorModel
    {
        public GameObject m_sourcePrefab;
        public GameObject m_previweGo;
        public GameObject m_previewGoBoneRoot;

        public bool Load(GameObject prefab)
        {
            m_sourcePrefab = prefab;
            m_previweGo = CreatePrefviewGoFromPrefab(prefab);
            FocusPreviewGo();
            return true;
        }

        public void Destroy()
        {
            if (m_previweGo != null)
            {
                GameObject.DestroyImmediate(m_previweGo);
                m_previweGo = null;
            }

            DestroySourceInst();
        }

        public void FocusPreviewGo()
        {
            if (m_previweGo != null)
            {
                Selection.activeObject = m_previweGo;
                SceneView.lastActiveSceneView.FrameSelected();
            }
        }

        public void CopyRagdollFromSource()
        {
            var srcRagdollRoot = FindSourceRagdollRoot();
            if (srcRagdollRoot == null)
            {
                return;
            }

            var destRagdollRoot = FindChildTransform(m_previweGo.transform, RagdollBoneMapper.BONES_ROOT);
            if (destRagdollRoot == null)
            {
                BLogger.Error("find {0} failed", RagdollBoneMapper.BONES_ROOT);
                return;
            }

            RagdollUtil.CloneRagdoll(srcRagdollRoot.transform, destRagdollRoot);
            ///DestroySourceInst();
        }

        public void ApplyRagdollToSource()
        {
            var destRagdollRoot = FindChildTransform(m_previweGo.transform, RagdollBoneMapper.BONES_ROOT);
            if (destRagdollRoot == null)
            {
                BLogger.Error("find {0} failed", RagdollBoneMapper.BONES_ROOT);
                return;
            }

            var srcRagdollRoot = FindSourceRagdollRoot();
            if (srcRagdollRoot == null)
            {
                RagdollUtil.CloneNewRagdoll(destRagdollRoot, m_sourceInst.transform);
                return;
            }
            
            RagdollUtil.CloneRagdoll(destRagdollRoot, srcRagdollRoot.transform);
            //DestroySourceInst();
        }

        private GameObject m_sourceInst = null;

        /// <summary>
        /// 所有子节点查找，同名的子节点
        /// </summary>
        /// <param name="root"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        Transform FindChildTransform(Transform root, string name)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child.name == name)
                {
                    return child;
                }

                var findTrans = FindChildTransform(child, name);
                if (findTrans != null)
                {
                    return findTrans;
                }
            }

            return null;
        }

        /// <summary>
        /// 获取源文件里ragdoll的root节点
        /// </summary>
        /// <returns></returns>
        GameObject FindSourceRagdollRoot()
        {
            var sourceInst = BeginSourceInstance();
            if (sourceInst != null)
            {
                var ragdollTrans = sourceInst.transform.Find(RagdollBoneMapper.RAGDOLL_BONES_ROOT);
                if (ragdollTrans != null)
                {
                    return ragdollTrans.gameObject;
                }
            }

            return null;
        }
        
        GameObject BeginSourceInstance()
        {
            if (m_sourceInst != null)
            {
                return m_sourceInst;
            }

            m_sourceInst = GameObject.Instantiate(m_sourcePrefab);
            return m_sourceInst;
        }

        void DestroySourceInst()
        {
            if (m_sourceInst != null)
            {
                GameObject.DestroyImmediate(m_sourceInst);
                m_sourceInst = null;
            }
        }

        GameObject CreatePrefviewGoFromPrefab(GameObject prefab)
        {
            var instGo = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            instGo.transform.position = Vector3.zero;
            instGo.transform.localRotation = Quaternion.identity;

            var tpsGo = instGo;
            var tpsTrans = instGo.transform.Find("TPS");
            if (tpsTrans != null)
            {
                tpsGo = tpsTrans.gameObject;
            }

            var previewGo = ClonePreviewFromTpsGo(tpsGo);
            GameObject.DestroyImmediate(instGo);
            return previewGo;
        }

        GameObject ClonePreviewFromTpsGo(GameObject tpsGo)
        {
            var skinnedMeshRender =  tpsGo.GetComponentInChildren<SkinnedMeshRenderer>();
            var sharedMesh = skinnedMeshRender.sharedMesh;
            var assetPath = AssetDatabase.GetAssetPath(sharedMesh);
            if (string.IsNullOrEmpty(assetPath))
            {
                BLogger.Error("invalid shared mesh");
                return null;
            }

            var newTempGo = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            var previewGo = GameObject.Instantiate(newTempGo);

            CopyPreviewCmpt(tpsGo, previewGo);
            return previewGo;
        }

        void CopyPreviewCmpt(GameObject src, GameObject dest)
        {
            var srcAnim = src.GetComponent<Animator>();
            var destAnim = dest.GetComponent<Animator>();
            if (destAnim != null && srcAnim != null)
            {
                //destAnim.runtimeAnimatorController = srcAnim.runtimeAnimatorController;
            }

            var allSrcRender = src.GetComponentsInChildren<Renderer>();
            var allDestRender = dest.GetComponentsInChildren<Renderer>();
            if (allSrcRender != null && allDestRender != null)
            {
                foreach (var srcRender in allSrcRender)
                {
                    var destRender = FindRender(allDestRender, srcRender);
                    if (destRender != null)
                    {
                        destRender.sharedMaterials = srcRender.sharedMaterials;
                    }
                }
            }
        }

        Renderer FindRender(Renderer[] renders, Renderer findRender)
        {
            foreach (var renderer in renders)
            {
                if (renderer.name == findRender.name)
                {
                    return renderer;
                }
            }

            return null;
        }
        
    }


    //[CustomEditor(typeof(RagdollConfig))]
    public class RagdollEditor : Editor
    {
        //private static string m_editorScene = "Assets/Scenes/SceneRagdoll.unity";
        private static RagdollEditor s_window = null;

        private GameObject m_lastSelectSrcPrefab = null;
        private RagdollEditorModel m_currModel = null;

        void OnEnable()
        {
            BLogger.SetLogHandler(new EditorLogHandler());
            BLogger.SetLevel((uint)BLogLevel.ALL);
        }
        
        //[MenuItem("DodTools/模型/Ragdoll")]
        //static void OpenEditorWindow()
        //{
        //    CheckScene();

        //    if (s_window == null)
        //    {
        //        var window = GetWindow<RagdollEditor>("Ragdoll配置");
        //        window.ShowUtility();
        //        window.Init();
        //    }
        //}

        //public static bool CheckScene()
        //{
        //    BLogger.SetLogHandler(new EditorLogHandler());
        //    BLogger.SetLevel((uint)BLogLevel.ALL);

        //    bool ret = true;
        //    if (m_editorScene != EditorApplication.currentScene)
        //    {
        //        EditorApplication.OpenScene(m_editorScene);
        //        ret = false;
        //    }

        //    //不需要强制启动
        //    if (!EditorApplication.isPlaying)
        //    {
        //        //EditorApplication.isPlaying = true;
        //        //ret = false;
        //    }

        //    return ret;
        //}

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            //DrawLoadedUI();
        }

#if false
        void DrawSelectUI()
        {
            GUILayout.Label("请选择TPS的Prefab");
            var selObject = Selection.activeGameObject;
            if (selObject != null)
            {
                if (!PrefabUtility.IsPartOfAnyPrefab(selObject))
                {
                    selObject = null;
                }
            }

            selObject = EditorGUILayout.ObjectField(selObject, typeof(GameObject)) as GameObject;
            if (selObject != null)
            {
                if (GUILayout.Button("载入"))
                {
                    if (!CheckScene())
                    {
                        return;
                    }

                    LoadModel(selObject);
                    m_lastSelectSrcPrefab = selObject;
                }
            }
            
        }
#endif

        void DrawLoadedUI()
        {
            var model = m_currModel;
            EditorGUILayout.ObjectField(model.m_sourcePrefab, typeof(GameObject));

            GUILayout.BeginVertical();
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("定位到预览对象"))
            {
                m_currModel.FocusPreviewGo();
            }
            
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("撤销修改"))
            {
                m_currModel.CopyRagdollFromSource();
            }

            GUILayout.Space(20);

            if (GUILayout.Button("保存修改"))
            {
                m_currModel.ApplyRagdollToSource();
            }

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();

            var backColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.red;
            var ret = GUILayout.Button("卸载");
            GUI.backgroundColor = backColor;

            if (ret)
            {
                m_currModel.Destroy();
                m_currModel = null;

                Selection.activeGameObject = m_lastSelectSrcPrefab;
                m_lastSelectSrcPrefab = null;
            }
            GUILayout.EndVertical();
        }

        void LoadModel(GameObject prefab)
        {
            var model = new RagdollEditorModel();
            if (model.Load(prefab))
            {
                m_currModel = model;
                m_currModel.CopyRagdollFromSource();
            }
        }
    }
}