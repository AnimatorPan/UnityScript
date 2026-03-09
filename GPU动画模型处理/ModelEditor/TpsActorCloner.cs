using System.Collections.Generic;
using DodGame;
using RootMotion.FinalIK;
using A9Game;
using UnityEditor;
using UnityEngine;


/// <summary>
/// TPS模型copy，避免每次更新模型都要手动设置一次
/// </summary>
public class TpsActorCloner : EditorWindow
{

    private GameObject m_sourcePrefab;
    private bool m_clearOldMesh = true;

    [MenuItem("Window/TPS模型复制")]
    public static void CloneEditor()
    {
        GetWindow(typeof(TpsActorCloner), false, "TPS模型复制");
    }

    public void OnGUI()
    {
        m_sourcePrefab = EditorGUILayout.ObjectField("选择源Prefab", m_sourcePrefab, typeof(GameObject)) as GameObject;

        m_clearOldMesh = GUILayout.Toggle(m_clearOldMesh,"是否清理旧的模型蒙皮");

        if (GUILayout.Button("开始copy到选择的prefab"))
        {
            CloneToSelection();
        }

        if (GUILayout.Button("只是copy ragdoll"))
        {
            CloneRagdollToSelection();
        }

        if (GUILayout.Button("Clear Ragdoll"))
        {
            ClearRagdoll();
        }

        if (GUILayout.Button("清理为死亡Prefab"))
        {
            ClearSelToDeathModel();
        }

        if (GUILayout.Button("只是copy collider(enable 状态与源目标相同)"))
        {
            CloneColliderToSelection();
        }

    }

    void ClearSelToDeathModel()
    {
        var activeGOs = Selection.GetFiltered(typeof(GameObject), SelectionMode.Editable | SelectionMode.TopLevel);
        if (activeGOs == null || activeGOs.Length <= 0)
        {
            EditorUtility.DisplayDialog("操作失败", "请选择带有RagdollController的Prefab操作", "OK");
            return;
        }

        //for (int i = 0; i < activeGOs.Length; i++)
        foreach (var goObj in activeGOs)
        {
            var go = goObj as GameObject;
            ClearToDeathMode(go);
        }
    }

    void ClearToDeathMode(GameObject go)
    {
        if (go == null)
        {
            return;
        }

        XGUtil.RemoveBehaviour<Animator>(go);
        XGUtil.RemoveBehaviour<HumanIK>(go);
        XGUtil.RemoveBehaviour<AimIK>(go);
        XGUtil.RemoveBehaviour<AimIK>(go);

    }

    void ClearRagdoll()
    {
        var activeGOs = Selection.GetFiltered(typeof(GameObject), SelectionMode.Editable | SelectionMode.TopLevel);
        if (activeGOs == null || activeGOs.Length <= 0)
        {
            EditorUtility.DisplayDialog("操作失败", "请选择带有RagdollController的Prefab操作", "OK");
            return;
        }

        //for (int i = 0; i < activeGOs.Length; i++)
        foreach (var goObj in activeGOs)
        {
            var go = goObj as GameObject;
            RagdollController ragCtrl = go.GetComponent<RagdollController>();
            if (ragCtrl != null)
            {
                if (ragCtrl.m_ragdollRoot == null)
                {
                    BLogger.Error("source ragdoll root is null");
                    EditorUtility.DisplayDialog("错误", "source ragdoll root is null", "OK");
                    return;
                }

                ClearGoRagdoll(ragCtrl.m_ragdollRoot);
                XGUtil.RemoveBehaviour<RagdollController>(go);
            }
        }
    }

    void ClearGoRagdoll(Transform trans)
    {
        XGUtil.RemoveBehaviour<Joint>(trans.gameObject);
        XGUtil.RemoveBehaviour<Rigidbody>(trans.gameObject);
        XGUtil.RemoveBehaviour<Collider>(trans.gameObject);

        for (int i = 0; i < trans.childCount; i++)
        {
            var childTrans = trans.GetChild(i);
            ClearGoRagdoll(childTrans);
        }
    }

    void CloneToSelection()
    {
        if (m_sourcePrefab == null)
        {
            EditorUtility.DisplayDialog("操作失败", "源Prefab 为空!!", "OK");
            return;
        }

        UnityEngine.Object[] activeGOs = Selection.GetFiltered(typeof(GameObject), SelectionMode.Editable | SelectionMode.TopLevel) ;       
        if (activeGOs == null || activeGOs.Length <= 0)
        {
            EditorUtility.DisplayDialog("操作失败", "请选择带有RagdollController的Prefab操作", "OK");
            return;
        }

        var srcGo = Instantiate(m_sourcePrefab) as GameObject;
        for (int i = 0; i < activeGOs.Length; i++)
        {
            CloneObject(srcGo, activeGOs[i] as GameObject, m_clearOldMesh);
        }

        DestroyImmediate(srcGo);
    }

    void CloneRagdollToSelection()
    {
        if (m_sourcePrefab == null)
        {
            EditorUtility.DisplayDialog("操作失败", "源Prefab 为空!!", "OK");
            return;
        }

        UnityEngine.Object[] activeGOs = Selection.GetFiltered(typeof(GameObject), SelectionMode.Editable | SelectionMode.TopLevel);
        if (activeGOs == null || activeGOs.Length <= 0)
        {
            EditorUtility.DisplayDialog("操作失败", "请选择带有RagdollController的Prefab操作", "OK");
            return;
        }

        var srcGo = Instantiate(m_sourcePrefab) as GameObject;
        for (int i = 0; i < activeGOs.Length; i++)
        {
            var selGo = activeGOs[i] as GameObject;
            CloneObject(srcGo, selGo, false);
        }

        DestroyImmediate(srcGo);
    }


    private void CloneColliderToSelection()
    {
        if (m_sourcePrefab == null)
        {
            EditorUtility.DisplayDialog("操作失败", "源Prefab 为空!!", "OK");
            return;
        }

        UnityEngine.Object[] activeGOs = Selection.GetFiltered(typeof(GameObject), SelectionMode.Editable | SelectionMode.TopLevel);
        if (activeGOs == null || activeGOs.Length <= 0)
        {
            EditorUtility.DisplayDialog("操作失败", "请选择目标模型", "OK");
            return;
        }


        var srcGo = Instantiate(m_sourcePrefab) as GameObject;
        for (int i = 0; i < activeGOs.Length; i++)
        {
            var selGo = activeGOs[i] as GameObject;
            DoCloneCollider(srcGo.transform, selGo.transform);
        }

        DestroyImmediate(srcGo);
    }

    public static void CloneObject(GameObject src, GameObject dest, bool clearOldMesh)
    {
        CloneRagdoll(src.transform, dest.transform);
        CloneHumanIK(src.transform, dest.transform);
        CloneTurrentIK(src.transform, dest.transform);
        //CloneShaderConfig(src.transform, dest.transform, clearOldMesh);
        CloneLodGroup(src.transform, dest.transform);
        CloneMeshProperty(src, dest);
        CloneTpsConfig(src.transform, dest.transform);
        CloneDynamicBone(src.transform, dest.transform);
        CloneAnimatorConfig(src.transform, dest.transform);
        ClonePoolBehaviourItem(src.transform, dest.transform);
        CloneLimbIK(src.transform, dest.transform);
    }

    
    public static void CloneMeshProperty(GameObject src, GameObject dest)
    {
        bool useLightProbe = false;
        var listRender = src.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in listRender)
        {
            if (renderer.useLightProbes)
            {
                useLightProbe = true;
                break;
            }
        }

        if (useLightProbe)
        {
            var listDestRender = dest.GetComponentsInChildren<Renderer>();
            foreach (var renderer in listDestRender)
            {
                renderer.useLightProbes = useLightProbe;
            }
        }
    }

    //clone Capsule Collider
    static void CloneCapsuleCollider(Transform baseObj, Transform targetObj, bool copyEnable = false, bool copyTag=false)
    {
        CapsuleCollider b = baseObj.GetComponent<CapsuleCollider>();
        CapsuleCollider t = targetObj.GetComponent<CapsuleCollider>();

        if (b && !t)	//we have just base - create the component on target
        {
            t = targetObj.gameObject.AddComponent<CapsuleCollider>();
            if (copyTag)
            {
                 targetObj.gameObject.tag= baseObj.gameObject.tag;
            }
        }

        if (b && t)
        {
            //			EditorUtility.CopySerialized(b, t);
            //			Debug.Log ("CloneCapsuleCollider:");

            //			DumpProps(t);

            t.isTrigger = b.isTrigger;
            t.material = b.material;
            t.center = b.center;
            t.radius = b.radius;
            t.height = b.height;
            t.direction = b.direction;
            if (copyEnable)
            {
                t.enabled = b.enabled;
            }
            else
            {
                t.enabled = false;
            }
           
            if (copyTag)
            {
                targetObj.gameObject.tag = baseObj.gameObject.tag;
            }
            EditorUtility.SetDirty(t);
        }
    }

    //clone Box Collider
    static void CloneBoxCollider(Transform baseObj, Transform targetObj, bool copyEnable = false, bool copyTag = false)
    {
        BoxCollider b = baseObj.GetComponent<BoxCollider>();
        BoxCollider t = targetObj.GetComponent<BoxCollider>();

        if (b && !t)	//we have just base - create the component on target
        {
            t = targetObj.gameObject.AddComponent<BoxCollider>();
            if (copyTag)
            {
                targetObj.gameObject.tag = baseObj.gameObject.tag;
            }
        }

        if (b && t)
        {
            //			EditorUtility.CopySerialized(b, t);
            //			Debug.Log ("CloneBoxCollider:");

            //			DumpProps(t);

            t.isTrigger = b.isTrigger;
            t.material = b.material;
            t.center = b.center;
            t.size = b.size;
            if (copyEnable)
            {
                t.enabled = b.enabled;
            }
            else
            {
                t.enabled = false;
            }
            if (copyTag)
            {
                targetObj.gameObject.tag = baseObj.gameObject.tag;
            }
            EditorUtility.SetDirty(t);
        }
    }

    //clone Sphere Collider
    static void CloneSphereCollider(Transform baseObj, Transform targetObj ,bool copyEnable = false,bool copyTag = false)
    {
        SphereCollider b = baseObj.GetComponent<SphereCollider>();
        SphereCollider t = targetObj.GetComponent<SphereCollider>();

        if (b && !t)	//we have just base - create the component on target
        {
            t = targetObj.gameObject.AddComponent<SphereCollider>();
            if (copyTag)
            {
                targetObj.gameObject.tag = baseObj.gameObject.tag;
            }
        }

        if (b && t)
        {
            //			EditorUtility.CopySerialized(b, t);
            //			Debug.Log ("CloneSphereCollider:");

            //			DumpProps(t);

            t.isTrigger = b.isTrigger;
            t.material = b.material;
            t.center = b.center;
            t.radius = b.radius;
            if (copyEnable)
            {
                t.enabled = b.enabled;
            }
            else
            {
                t.enabled = false;
            }
            if (copyTag)
            {
                targetObj.gameObject.tag = baseObj.gameObject.tag;
            }
            EditorUtility.SetDirty(t);
        }
    }

    //clone Rigidbody
    static void CloneRigidbody(Transform baseObj, Transform targetObj)
    {
        Rigidbody b = baseObj.GetComponent<Rigidbody>();
        Rigidbody t = targetObj.GetComponent<Rigidbody>();

        if (b && !t)	//we have just base - create the component on target
        {
            t = targetObj.gameObject.AddComponent<Rigidbody>();
        }

        if (b && t)
        {
            //			EditorUtility.CopySerialized(b, t);
            //			Debug.Log ("CloneRigidbody:");

            //			DumpProps(t);

            t.mass = b.mass;
            t.drag = b.drag;
            t.angularDrag = b.angularDrag;
            t.useGravity = b.useGravity;
            t.isKinematic = b.isKinematic;
            t.interpolation = b.interpolation;
            t.collisionDetectionMode = b.collisionDetectionMode;
            t.constraints = b.constraints;
            EditorUtility.SetDirty(t);
        }
    }

    //clone CharacterJoint
    static void CloneCharacterJoint(Transform baseObj, Transform targetObj)
    {
        CharacterJoint b = baseObj.GetComponent<CharacterJoint>();
        CharacterJoint t = targetObj.GetComponent<CharacterJoint>();

        if (b && !t)	//we have just base - create the component on target
        {
            t = targetObj.gameObject.AddComponent<CharacterJoint>();
        }

        if (b && t)
        {
            //			EditorUtility.CopySerialized(b, t);
            //			Debug.Log ("CloneCharacterJoint:");

            //			DumpProps(t);

            Rigidbody connBody = null;
            Transform parent = targetObj.parent;

            do
            {
                connBody = parent.GetComponent<Rigidbody>();
                parent = parent.parent;
            } while (connBody == null && parent.parent != null);

            t.connectedBody = connBody;
            t.anchor = b.anchor;
            t.axis = b.axis;
            t.swingAxis = b.swingAxis;
            t.lowTwistLimit = b.lowTwistLimit;
            t.highTwistLimit = b.highTwistLimit;
            t.swing1Limit = b.swing1Limit;
            t.swing2Limit = b.swing2Limit;
            t.breakForce = b.breakForce;
            t.breakTorque = b.breakTorque;
            b.enableCollision = false;

            EditorUtility.SetDirty(t);
        }
    }

    static bool CloneRagdoll(Transform srcObj, Transform destObj)
    {
        if (srcObj == destObj)
        {
            EditorUtility.DisplayDialog("操作失败", "不能选择同样的节点", "OK");
            return false;
        }

        RagdollController srcController = srcObj.GetComponent<RagdollController>();
        RagdollController destController = destObj.GetComponent<RagdollController>();

        if (srcController != null)
        {
            if (srcController.m_ragdollRoot == null)
            {
                Debug.LogError(srcObj.name + " ragdoll root is null");
                EditorUtility.DisplayDialog("错误", "source ragdoll root is null", "OK");
                return false;
            }

            string rootPath = GetTransformPath(srcController.m_ragdollRoot);
            BLogger.Assert(!string.IsNullOrEmpty(rootPath));

            var destRootTrans = destObj.Find(rootPath);
            if (destRootTrans == null)
            {
                EditorUtility.DisplayDialog("错误", "find dest root transform failed", "OK");
                return false;
            }

            if (destController == null)
            {
                destController = destObj.gameObject.AddComponent<RagdollController>();
            }

            destController.m_ragdollRoot = destRootTrans;
            destController.m_applyRoot = srcController.m_applyRoot;

            Transform srcRagollRoot = srcController.m_ragdollRoot;
            Transform destRagollRoot = destController.m_ragdollRoot;
            DoCloneRagdoll(srcRagollRoot, destRagollRoot);
            
            if (srcController.m_ragdollForcePoint != null)
            {
                Transform forcePoint = FindRelateTransform(srcController.m_ragdollForcePoint.transform, destObj);
                destController.m_ragdollForcePoint = forcePoint.GetComponent<Rigidbody>();
            }

            destController.CollectRagdollNode();
            destController.ForceSetRagdollEnable(false, true);
        }

        return true;
    }

    static void DoCloneRagdoll(Transform srcObj, Transform destObj)
    {
        //clone Capsule Collider
        CloneCapsuleCollider(srcObj, destObj);

        //clone Box Collider
        CloneBoxCollider(srcObj, destObj);

        //clone Sphere Collider
        CloneSphereCollider(srcObj, destObj);

        //clone Rigidbody
        CloneRigidbody(srcObj, destObj);

        //clone CharacterJoint
        CloneCharacterJoint(srcObj, destObj);

        //recurse to children
        foreach (Transform child in srcObj)
        {
            Transform targetChild = destObj.Find(child.name);

            if (targetChild)
                DoCloneRagdoll(child, targetChild);
        }
    }

    static void DoCloneCollider(Transform srcObj, Transform destObj)
    {
        //clone Capsule Collider
        CloneCapsuleCollider(srcObj, destObj,true,true);

        //clone Box Collider
        CloneBoxCollider(srcObj, destObj,true,true);

        //clone Sphere Collider
        CloneSphereCollider(srcObj, destObj,true,true);

        //recurse to children
        foreach (Transform child in srcObj)
        {
            Transform targetChild = destObj.Find(child.name);

            if (targetChild)
                DoCloneCollider(child, targetChild);
        }
    }

    public static bool CloneHumanIK(Transform baseObj, Transform targetObj)
    {
        AimIK srcIK = baseObj.GetComponent<AimIK>();
        AimIK destIk = targetObj.GetComponent<AimIK>();
        if (srcIK == null)
        {
            //Debug.LogError(string.Format("source object find aim ik null: {0}", baseObj.name));
            return false;
        }

        if (destIk == null)
        {
            destIk = targetObj.gameObject.AddComponent<AimIK>();
        }

        IKSolverAim srcSolover = srcIK.solver;
        IKSolverAim destSolover = destIk.solver;

        string targetPath = GetTransformPath(srcSolover.transform);
        //Debug.LogError(string.Format("taret path: {0}", targetPath));

        Transform destTargetTrans = targetObj.Find(targetPath);
        if (destTargetTrans == null)
        {
            Debug.LogError(string.Format("find target failed: {0}, {1}", targetObj.name, targetPath));
            return false;
        }

        destSolover.transform = destTargetTrans;
        destSolover.axis = srcSolover.axis;
        destSolover.clampWeight = srcSolover.clampWeight;
        destSolover.clampSmoothing = srcSolover.clampSmoothing;
        destSolover.IKPositionWeight = srcSolover.IKPositionWeight;
        destSolover.useRotationLimits = srcSolover.useRotationLimits;

        destSolover.bones = new IKSolver.Bone[srcSolover.bones.Length];
        for (int i = 0; i < srcSolover.bones.Length; i++)
        {
            IKSolver.Bone srcBone = srcSolover.bones[i];
            destSolover.bones[i] = CloneIKBone(srcBone, targetObj);
        }

        HumanIK srcHumanIK = baseObj.GetComponent<HumanIK>();
        HumanIK destHumanIK = targetObj.GetComponent<HumanIK>();

        if (srcHumanIK != null)
        {
            if (destHumanIK == null)
            {
                destHumanIK = targetObj.gameObject.AddComponent<HumanIK>();
            }

            destHumanIK.m_bodyYawSpeed = srcHumanIK.m_bodyYawSpeed;
            destHumanIK.m_bodyYawStandSpeed = srcHumanIK.m_bodyYawStandSpeed;
            destHumanIK.m_enableIK = srcHumanIK.m_enableIK;
        }
        else
        {
            Debug.LogError(string.Format("source object find human ik null: {0}", baseObj.name));
            return false;
        }

        return true;
    }


    public static bool CloneLimbIK(Transform baseObj, Transform targetObj)
    {
        LimbIK[] srcIK = baseObj.GetComponents<LimbIK>();
        LimbIK[] destIk = targetObj.GetComponents<LimbIK>();
        if (srcIK == null)
        {
            //Debug.LogError(string.Format("source object find aim ik null: {0}", baseObj.name));
            return false;
        }

        for (int i = 0; i < srcIK.Length; i++)
        {
            var src = srcIK[i];
            LimbIK dest;
            if (destIk.Length>i)
            {
                 dest = destIk[i];
            }
            else
            {
                dest = targetObj.gameObject.AddComponent<LimbIK>();
            }

            var srcBone1 = src.solver.bone1.transform;
            string targetPath1 = GetTransformPath(srcBone1);
            Transform destTargetTrans1 = targetObj.Find(targetPath1);
            dest.solver.bone1.transform = destTargetTrans1;

            var srcBone2 = src.solver.bone2.transform;
            string targetPath2 = GetTransformPath(srcBone2);
            Transform destTargetTrans2 = targetObj.Find(targetPath2);
            dest.solver.bone2.transform = destTargetTrans2;

            var srcBone3 = src.solver.bone3.transform;
            string targetPath3 = GetTransformPath(srcBone3);
            Transform destTargetTrans3 = targetObj.Find(targetPath3);
            dest.solver.bone3.transform = destTargetTrans3;

            dest.solver.goal = src.solver.goal;
            dest.solver.maintainRotationWeight = 1;
        }


        return true;
    }

    public static bool CloneTurrentIK(Transform baseObj, Transform targetObj)
    {
        var srcIK = baseObj.GetComponent<TurretIK>();
        if (srcIK == null || srcIK.m_parts == null || srcIK.m_parts.Length <= 0)
        {
            return false;
        }

        var destIk = targetObj.gameObject.GetComponent<TurretIK>();
        if (destIk == null)
        {
            destIk = targetObj.gameObject.AddComponent<TurretIK>();
        }
        
        destIk.m_parts = new TurretIK.Part[srcIK.m_parts.Length];
        for (int i = 0; i < srcIK.m_parts.Length; i++)
        {
            var srcPart = srcIK.m_parts[i];
            string targetPath = GetTransformPath(srcPart.m_transform);
            Transform destTargetTrans = targetObj.Find(targetPath);
            if (destTargetTrans == null)
            {
                Debug.LogError(string.Format("find target failed: {0}, {1}", targetObj.name, targetPath));
                return false;
            }

            destIk.m_parts[i] = new TurretIK.Part();
            var destPart = destIk.m_parts[i];
            destPart.m_transform = destTargetTrans;
            //destPart.m_axis = srcPart.m_axis;

            var srcRotLimit = srcPart.m_transform.GetComponent<RotationLimitHinge>();
            if (srcRotLimit != null)
            {
                var destRotLimit = destTargetTrans.GetComponent<RotationLimitHinge>();
                if (destRotLimit == null)
                {
                    destRotLimit = destTargetTrans.gameObject.AddComponent<RotationLimitHinge>();
                }

                destRotLimit.axis = srcRotLimit.axis;
                destRotLimit.useLimits = srcRotLimit.useLimits;
                destRotLimit.min = srcRotLimit.min;
                destRotLimit.max = srcRotLimit.max;
            }
        }
        
        return true;
    }


    static Transform FindRelateTransform(Transform srcTrans, Transform destRootTrans)
    {
        string path = GetTransformPath(srcTrans);
        if (!string.IsNullOrEmpty(path))
        {
            return destRootTrans.Find(path);
        }

        return destRootTrans;
    }

    static void CloneLodGroup(Transform baseObj, Transform targetObj)
    {
        var srcGroup = baseObj.GetComponent<LODGroup>();
        if (srcGroup == null)
        {
            return;
        }

        XGUtil.RemoveBehaviour<LODGroup>(targetObj.gameObject);
        var destGroup = XGUtil.AddMonoBehaviour<LODGroup>(targetObj.gameObject);
        if (destGroup == null)
        {
            return;
        }

        LodGroupInfo srcGroupInfo = new LodGroupInfo(srcGroup);
        LodGroupInfo dstGroupInfo = new LodGroupInfo(destGroup);

        int lodCnt = srcGroup.lodCount;
        while (dstGroupInfo.GetLodCount() < srcGroupInfo.GetLodCount())
        {
            if (!dstGroupInfo.InsertLodItemInfo())
            {
                Debug.LogError("insert log faield");
                return;
            }
        }

        ///然后开始copy
        for (int i = 0; i < srcGroupInfo.GetLodCount(); i++)
        {
            var srcLodInfo = srcGroupInfo.GetLodItemInfo(i);
            var dstLodInfo = dstGroupInfo.GetLodItemInfo(i);

            dstLodInfo.ScreenPercent = srcLodInfo.ScreenPercent;

            for (int k = 0; k < srcLodInfo.MeshCount; k++)
            {
                var srcRender = srcLodInfo.GetRenderer(k);
                if (srcRender == null)
                {
                    continue;
                }
                string targetRenderPath = GetTransformPath(srcRender.transform);
                var dstTransform = targetObj.Find(targetRenderPath);
                if (dstTransform != null)
                {
                    Renderer render = dstTransform.GetComponent<Renderer>();
                    if (render != null)
                    {
                        dstLodInfo.AddRender(render);
                    }
                    else
                    {
                        Debug.LogError(string.Format("lod renderer path not exist: {0}", targetRenderPath));
                    }
                }
            }
        }

        dstGroupInfo.ApplyModifiedProperties();
        destGroup.RecalculateBounds();
    }
    
    static IKSolver.Bone CloneIKBone(IKSolver.Bone src, Transform destRoot)
    {
        IKSolver.Bone dest = new IKSolver.Bone();

        if (src.transform != null)
        {
            string transPath = GetTransformPath(src.transform);
            Transform destTrans = destRoot.Find(transPath);
            dest.transform = destTrans;
        }

        dest.weight = src.weight;
        return dest;
    }

    static string GetTransformPath(Transform tran)
    {
        string path = tran.name;
        Transform parent = tran.parent;
        if (parent == null)
        {
            return null;
        }

        string append = string.Empty;
        while (parent != null && parent.parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }
    
    static bool CloneTpsConfig(Transform baseObj, Transform targetObj)
    {
        var srcConfig = baseObj.GetComponent<TpsConfig>();
        if (srcConfig == null)
        {
            Debug.LogError(string.Format("source object find TpsConfig null: {0}", baseObj.name));
            return false;
        }

        var destConfig = XGUtil.AddMonoBehaviour<TpsConfig>(targetObj.gameObject);
        destConfig.CollectCachePos();
        return true;
    }

    static bool CloneDynamicBone(Transform baseObj, Transform targetObj)
    {
        DynamicBone[] srcConfig = baseObj.GetComponents<DynamicBone>();
        if (srcConfig == null)
        {
            Debug.LogError(string.Format("source object find DynamicBone null: {0}", baseObj.name));
            return false;
        }
        for (int i = 0;i< srcConfig.Length;i++)
        {
            DynamicBone destConfig = targetObj.gameObject.AddComponent<DynamicBone>();//XGUtil.AddMonoBehaviour<DynamicBone>(targetObj.gameObject);
            destConfig.m_Root = FindRootChild(targetObj, srcConfig[i].m_Root.name);
            destConfig.m_UpdateRate = srcConfig[i].m_UpdateRate;
            destConfig.m_Damping = srcConfig[i].m_Damping;
            destConfig.m_DampingDistrib = srcConfig[i].m_DampingDistrib;
            destConfig.m_Elasticity = srcConfig[i].m_Elasticity;
            destConfig.m_ElasticityDistrib = srcConfig[i].m_ElasticityDistrib;
            destConfig.m_Stiffness = srcConfig[i].m_Stiffness;
            destConfig.m_StiffnessDistrib = srcConfig[i].m_StiffnessDistrib;
            destConfig.m_Inert = srcConfig[i].m_Inert;
            destConfig.m_InertDistrib = srcConfig[i].m_InertDistrib;
            destConfig.m_Radius = srcConfig[i].m_Radius;
            destConfig.m_RadiusDistrib = srcConfig[i].m_RadiusDistrib;
            destConfig.m_EndLength = srcConfig[i].m_EndLength;
            destConfig.m_EndOffset = srcConfig[i].m_EndOffset;
            destConfig.m_Gravity = srcConfig[i].m_Gravity;
            destConfig.m_Force = srcConfig[i].m_Force;

            destConfig.m_Colliders = new List<DynamicBoneCollider>();
            for (int j = 0;j<srcConfig[i].m_Colliders.Count;j++)
            {
                if (srcConfig[i].m_Colliders[j] == null)
                {
                    continue;
                }
                Transform theChild = FindRootChild(targetObj, srcConfig[i].m_Colliders[j].name);
                if (theChild != null)
                {
                    DynamicBoneCollider theCmpt = XGUtil.AddMonoBehaviour<DynamicBoneCollider>(theChild.gameObject);
                    destConfig.m_Colliders.Add(theCmpt);
                }
            }

            destConfig.m_Exclusions = new List<Transform>();
            for (int j = 0; j < srcConfig[i].m_Exclusions.Count; j++)
            {
                if (srcConfig[i].m_Exclusions[j] == null)
                {
                    continue;
                }
                Transform theChild = FindRootChild(targetObj, srcConfig[i].m_Exclusions[j].name);
                if (theChild != null)
                {
                    destConfig.m_Exclusions.Add(theChild);
                }
            }


            destConfig.m_FreezeAxis = srcConfig[i].m_FreezeAxis;
            destConfig.m_DistantDisable = srcConfig[i].m_DistantDisable;
            if (srcConfig[i].m_ReferenceObject != null)
            {
                destConfig.m_ReferenceObject = FindRootChild(targetObj, srcConfig[i].m_ReferenceObject.name);
            }
            destConfig.m_DistanceToObject = srcConfig[i].m_DistanceToObject;
        }

        return true;
    }

    //找到一个子节点
    static Transform FindRootChild(Transform root , string childName)
    {
        if (root.name == childName)
        {
            return root;
        }
        Transform theTransform = null;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform theChild = root.GetChild(i);
            if (theChild != null)
            {
                theTransform = FindRootChild(theChild, childName);
                if (theTransform != null)
                {
                    return theTransform;
                }
            }
        }
        return null;
    }

    static bool CloneAnimatorConfig(Transform baseObj, Transform targetObj)
    {
        var srcConfig = baseObj.GetComponent<AnimatorConfig>();
        if (srcConfig == null)
        {
            //死亡和展示模型没有
            return false;
        }

        var srcAnims = baseObj.GetComponentsInChildren<Animator>();
        if (srcAnims != null && srcAnims.Length > 0)
        {
            for (int i = 0; i < srcAnims.Length; i++)
            {
                var srcAnm = srcAnims[i];
                var destTrans = FindRelateTransform(srcAnm.transform, targetObj.transform);
                if (destTrans != null)
                {
                    var destAnim = destTrans.gameObject.GetComponent<Animator>();
                    if (destAnim != null)
                    {
                        destAnim.runtimeAnimatorController = srcAnm.runtimeAnimatorController;
                    }
                }
            }
        }
        
        var destConfig = XGUtil.AddMonoBehaviour<AnimatorConfig>(targetObj.gameObject);
        AnimatorConfigInspector.EditorCollect(destConfig);
        return true;
    }
    
    static bool ClonePoolBehaviourItem(Transform baseObj, Transform targetObj)
    {
        // var srcConfig = baseObj.GetComponent<GoPoolBehaviourItem>();
        // if (srcConfig == null)
        // {
        //     //展示模型没有
        //     return false;
        // }
        //
        // var destConfig = XGUtil.AddMonoBehaviour<GoPoolBehaviourItem>(targetObj.gameObject);
        // destConfig.EditorRefresh();
        return true;
    }
}
