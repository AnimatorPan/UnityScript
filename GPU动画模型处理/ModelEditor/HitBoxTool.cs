using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using DodGame;
using A9Game;

public class HitBoxTool
{

    public class DumplicateAnimationWizard : ScriptableWizard
    {
        public Dictionary<string, Transform> allBones = new Dictionary<string, Transform>();

        public Transform leftArm;
        public Transform rightArm;
        public Transform leftForArm;
        public Transform rightForArm = null;
        public Transform leftThigh;
        public Transform rightThigh;
        public Transform rightCalf;
        public Transform leftCalf;

        public Transform spine;
        public Transform pelvis;
        public Transform head;

        private void OnWizardCreate()
        {
            Bounds bounds = new Bounds();
            bounds.Encapsulate(spine.InverseTransformPoint(leftThigh.position));

            bounds.Encapsulate(spine.InverseTransformPoint(rightThigh.position));

            bounds.Encapsulate(spine.InverseTransformPoint(leftArm.position));

            bounds.Encapsulate(spine.InverseTransformPoint(rightArm.position));

            Transform nack = null;
            if (allBones.TryGetValue(RagdollBoneMapper.NACK, out nack))
            {
                bounds.Encapsulate(spine.InverseTransformPoint(nack.position));
            }

            Transform hitBoxBody = spine;// GetOrCrerateChild(spine, "HitBox-Body");
            var box = GetOrCreateComponent<BoxCollider>(hitBoxBody.gameObject);
            box.center = bounds.center;
            var size = bounds.size;
            size.y = size.x * 0.5f;
            box.size = size;

            hitBoxBody.tag = GameTag.ColliderBody;

            Transform headHitBox = head; //GetOrCrerateChild(head, "HitBox-Head");
            headHitBox.tag = GameTag.ColliderHead;
            AddHeadCollider(headHitBox, leftArm, rightArm, pelvis);

            AddCapsule(leftArm, "HitBox-Arm", 0.2f);
            SetTag(leftArm, GameTag.ColliderBody);
            AddCapsule(rightArm, "HitBox-Arm", 0.2f);
            SetTag(rightArm, GameTag.ColliderBody);

            AddCapsule(leftThigh, "HitBox-Thigh", 0.2f);
            SetTag(leftThigh, GameTag.ColliderBody);
            AddCapsule(rightThigh, "HitBox-Thigh", 0.2f);
            SetTag(rightThigh, GameTag.ColliderBody);

            AddCapsule(leftCalf, "HitBox-Calf", 0.2f);
            SetTag(leftCalf, GameTag.ColliderBody);
            AddCapsule(rightCalf, "HitBox-Calf", 0.2f);
            SetTag(rightCalf, GameTag.ColliderBody);

            AddCapsule(leftForArm, "HitBox-ForArm", 0.2f);
            SetTag(leftForArm, GameTag.ColliderBody);
            AddCapsule(rightForArm, "HitBox-ForArm", 0.2f);
            SetTag(rightForArm, GameTag.ColliderBody);
        }

        public bool InitBones(Transform root)
        {
            RagdollUtil.CollectBones(root, allBones);

            allBones.TryGetValue(RagdollBoneMapper.LEFT_ARM, out leftArm);
            if (leftArm == null)
            {
                Debug.LogError("no bone: " + RagdollBoneMapper.LEFT_ARM);
                return false;
            }

            allBones.TryGetValue(RagdollBoneMapper.RIGHT_ARM, out rightArm);
            if (rightArm == null)
            {
                Debug.LogError("no bone: " + RagdollBoneMapper.RIGHT_ARM);
                return false;
            }

            if (!allBones.TryGetValue(RagdollBoneMapper.LEFT_FOREARM, out leftForArm))
            {
                Debug.LogError("no bone: " + RagdollBoneMapper.LEFT_FOREARM);
                return false;
            }


            if (!allBones.TryGetValue(RagdollBoneMapper.RIGHT_FOREARM, out rightForArm))
            {
                Debug.LogError("no bone: " + RagdollBoneMapper.RIGHT_FOREARM);
                return false;
            }

            allBones.TryGetValue(RagdollBoneMapper.LEFT_THIGH, out leftThigh);
            if (leftThigh == null)
            {
                Debug.LogError("no bone: " + RagdollBoneMapper.LEFT_THIGH);
                return false;
            }

            allBones.TryGetValue(RagdollBoneMapper.RIGHT_THIGH, out rightThigh);
            if (rightThigh == null)
            {
                Debug.LogError("no bone: " + RagdollBoneMapper.RIGHT_THIGH);
                return false;
            }

            allBones.TryGetValue(RagdollBoneMapper.RTGHT_CALF, out rightCalf);
            if (rightCalf == null)
            {
                Debug.LogError("no bone rightCalf ");
                return false;
            }

            allBones.TryGetValue(RagdollBoneMapper.LEFT_CALF, out leftCalf);
            if (leftCalf == null)
            {
                Debug.LogError("no bone leftCalf ");
                return false;
            }

            allBones.TryGetValue(RagdollBoneMapper.MIDDLE_SPINE, out spine);
            if (spine == null)
            {
                Debug.LogError("no spine bone!");
                return false;
            }

            allBones.TryGetValue(RagdollBoneMapper.PELVIS, out pelvis);
            if (pelvis == null)
            {
                Debug.LogError("no pelvis bone!");
                return false;
            }

            allBones.TryGetValue(RagdollBoneMapper.HEAD, out head);
            if (head == null)
            {
                Debug.LogError("no head bone!!");
                return false;
            }
            return true;
        }


    }

    [MenuItem("GameObject/Add HitBox", priority = -100)]
    public static void AddBodyBox()
    {
        var wizard = ScriptableWizard.DisplayWizard<DumplicateAnimationWizard>("Add HitBox");

        var rootBone = Selection.activeTransform.Find("Bip001");
        if (rootBone == null)
        {
            return;
        }

        if (!wizard.InitBones(rootBone))
            return;


    }

    static void AddHeadCollider(Transform head, Transform leftArm, Transform rightArm, Transform pelvis)
    {
        if (head == null)
        {
            return;
        }

        float radius = Vector3.Distance(leftArm.transform.position, rightArm.transform.position);
        radius /= 4;

        SphereCollider sphere = GetOrCreateComponent<SphereCollider>(head.gameObject);
        sphere.radius = radius;
        Vector3 center = Vector3.zero;

        int direction;
        float distance;
        RagdollUtil.CalculateDirection(head.InverseTransformPoint(pelvis.position), out direction, out distance);
        if (distance > 0)
        {
            center[direction] = -radius;
        }
        else
        {
            center[direction] = radius;
        }
        sphere.center = center;
    }

    static void AddCapsule(Transform boneAnchor, string hitBoxName, float scale)
    {
        var hitBoxTrans = boneAnchor;// GetOrCrerateChild(boneAnchor, hitBoxName);
        if (boneAnchor.childCount >= 1)
        {
            Transform endPoint = null;
            for (int i = 0; i < boneAnchor.childCount; i++)
            {
                var child = boneAnchor.GetChild(i);
                var childName = child.name;
                if (childName.IndexOf("Twist") == -1 && childName.IndexOf("DM_") == -1)
                {
                    endPoint = child;
                    break;
                }
            }

            endPoint = endPoint == null ? boneAnchor.GetChild(0) : endPoint;
            var localPos = boneAnchor.InverseTransformPoint(endPoint.position);
            int dir;
            float distance;

            RagdollUtil.CalculateDirection(localPos, out dir, out distance);

            CapsuleCollider collider = GetOrCreateComponent<CapsuleCollider>(hitBoxTrans.gameObject);
            collider.direction = dir;

            Vector3 center = Vector3.zero;
            center[dir] = distance * 0.5F;
            collider.center = center;
            collider.height = Mathf.Abs(distance);
            collider.radius = Mathf.Abs(distance * scale);
        }
    }

    static void SetTag(Transform boneAnchor,string tagname)
    {
        boneAnchor.tag = tagname;
    }

    static T GetOrCreateComponent<T>(GameObject go) where T : Component
    {
        T ret = go.GetComponent<T>();
        if (ret == null)
        {
            ret = Undo.AddComponent<T>(go);
        }

        return ret;
    }

    static Transform GetOrCrerateChild(Transform parent, string name)
    {
        var child = parent.Find(name);
        if (child == null)
        {
            var go = new GameObject(name);
            child = go.transform;
            child.parent = parent;
            child.localPosition = Vector3.zero;
            child.localRotation = Quaternion.identity;
        }
        return child;
    }

    //[MenuItem("DodTools/模型/更新Hit Reactionbox节点")]
    //static void RefreshHitReactionBox()
    //{
    //    var hitReaction = GetOrCreateHitReaction(Selection.activeTransform);
    //    var effectorHitPoints = hitReaction.effectorHitPoints;

    //    Dictionary<string, Transform> allBones = new Dictionary<string, Transform>();
    //    RagdollUtil.CollectBones(Selection.activeTransform, allBones);
    //    foreach (var effector in effectorHitPoints)
    //    {
    //        Transform target = GetBone(effector.name, allBones);


    //        if (target != null)
    //        {
    //            effector.collider = target.GetComponent<Collider>();
    //        }
    //    }

    //    foreach (var item in hitReaction.boneHitPoints)
    //    {
    //        Transform target = GetBone(item.name, allBones);
    //        if (target != null)
    //        {
    //            item.collider = target.GetComponent<Collider>();
    //        }
    //    }
    //    var fullBodyIk = GetOrCreateComponent<FullBodyBipedIK>(Selection.activeGameObject);
    //    //var dodHitR = GetOrCreateComponent<DodHitReactionTrigger>(Selection.activeGameObject);
    //    //dodHitR.m_fullBodyBipedIK = fullBodyIk;
    //    //dodHitR.m_hitReaction = hitReaction;
    //    //hitReaction.ik = fullBodyIk;
    //}

    static RootMotion.FinalIK.HitReaction GetOrCreateHitReaction(Transform trans)
    {
        var hitR = trans.GetComponent<RootMotion.FinalIK.HitReaction>();
        if (hitR == null)
        {
            hitR = trans.gameObject.AddComponent<RootMotion.FinalIK.HitReaction>();
            var temp = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Editor/ModelTool/HitReactionTemplate.prefab");
            if (temp != null)
            {
                var tempHitR = temp.GetComponent<RootMotion.FinalIK.HitReaction>();
                UnityEditorInternal.ComponentUtility.CopyComponent(tempHitR);
                UnityEditorInternal.ComponentUtility.PasteComponentValues(hitR);
            }
        }

        return hitR;
    }

    static Transform GetBone(string name, Dictionary<string, Transform> allBones)
    {
        Transform target = null;
        if (name == "L Forearm")
        {
            target = allBones[RagdollBoneMapper.LEFT_FOREARM];
        }
        else if (name == "Head")
        {
            target = allBones[RagdollBoneMapper.HEAD];
        }
        else if (name == "Hips")
        {
            target = allBones[RagdollBoneMapper.MIDDLE_SPINE];
        }
        else if (name == "Hips: Hands Down" || name == "Chest")
        {
            target = allBones[RagdollBoneMapper.MIDDLE_SPINE];
        }
        else if (name == "L Upper Arm")
        {
            target = allBones[RagdollBoneMapper.LEFT_ARM];
        }
        else if (name == "R Upper Arm")
        {
            target = allBones[RagdollBoneMapper.RIGHT_ARM];
        }
        else if (name == "R Forearm")
        {
            target = allBones[RagdollBoneMapper.RIGHT_FOREARM];
        }
        else if (name == "R Thigh")
        {
            target = allBones[RagdollBoneMapper.RIGHT_THIGH];
        }
        else if (name == "L Thigh")
        {
            target = allBones[RagdollBoneMapper.LEFT_THIGH];
        }
        else if (name == "L Calf")
        {
            target = allBones[RagdollBoneMapper.LEFT_CALF];
        }
        else if (name == "R Calf")
            target = allBones[RagdollBoneMapper.RTGHT_CALF];

        return target;
    }
}
