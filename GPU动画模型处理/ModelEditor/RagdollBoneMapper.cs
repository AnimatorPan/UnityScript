using System;
using System.Collections.Generic;
using System.Reflection;
using DEditorUIRuntime;

namespace DodGame
{

    public enum RagdollBoneColliderType
    {
        None,
        Sphere,
        Capsules,
        Box,
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class RagdollBoneColliderTypeAttr : Attribute
    {
        public RagdollBoneColliderType m_type;

        public RagdollBoneColliderTypeAttr(RagdollBoneColliderType type)
        {
            m_type = type;
        }
    }

    class RagdollBoneMapper
    {
        [DisplayName("盆骨")]
        [RagdollBoneColliderTypeAttr(RagdollBoneColliderType.Box)]
        public const string PELVIS = "Bip001 Pelvis";

        [DisplayName("左臀")]
        [RagdollBoneColliderTypeAttr(RagdollBoneColliderType.Capsules)]
        public const string LEFT_THIGH = "Bip001 L Thigh";

        [DisplayName("左膝")]
        [RagdollBoneColliderTypeAttr(RagdollBoneColliderType.Capsules)]
        public const string LEFT_CALF = "Bip001 L Calf";

        [DisplayName("右臀")]
        [RagdollBoneColliderTypeAttr(RagdollBoneColliderType.Capsules)]
        public const string RIGHT_THIGH = "Bip001 R Thigh";

        [DisplayName("右膝")]
        [RagdollBoneColliderTypeAttr(RagdollBoneColliderType.Capsules)]
        public const string RTGHT_CALF = "Bip001 R Calf";

        [DisplayName("左上臂")]
        [RagdollBoneColliderTypeAttr(RagdollBoneColliderType.Capsules)]
        public const string LEFT_ARM = "Bip001 L UpperArm";

        [DisplayName("左下臂")]
        [RagdollBoneColliderTypeAttr(RagdollBoneColliderType.Capsules)]
        public const string LEFT_FOREARM = "Bip001 L Forearm";

        [DisplayName("右上臂")]
        [RagdollBoneColliderTypeAttr(RagdollBoneColliderType.Capsules)]
        public const string RIGHT_ARM = "Bip001 R UpperArm";

        [DisplayName("右下臂")]
        [RagdollBoneColliderTypeAttr(RagdollBoneColliderType.Capsules)]
        public const string RIGHT_FOREARM = "Bip001 R Forearm";

        [RagdollBoneColliderTypeAttr(RagdollBoneColliderType.Box)]
        [DisplayName("中间脊柱")]
        public const string MIDDLE_SPINE = "Bip001 Spine";

        [DisplayName("头部")]
        [RagdollBoneColliderTypeAttr(RagdollBoneColliderType.Sphere)]
        public const string HEAD = "Bip001 Head";

        [DisplayName("根节点")]
        public const string BONES_ROOT = "Bip001";

        [DisplayName("Ragdoll根节点")]
        public const string RAGDOLL_BONES_ROOT = "RagdollBones";

        [DisplayName("脖子")]
        public const string NACK = "Bip001 Neck";


        class RagdollBoneMeta
        {
            public RagdollBoneColliderType m_boneType;
            public string m_showName;
            public string m_boneName;

            public RagdollBoneMeta(RagdollBoneColliderType boneType, string showName, string boneName)
            {
                m_boneType = boneType;
                m_showName = showName;
                m_boneName = boneName;
            }
        }

        private static List<RagdollBoneMeta> s_allBoneMeta = null;

        public static int GetBoneMetaCount()
        {
            if (s_allBoneMeta == null)
            {
                GenerateBoneMeta();
            }

            return s_allBoneMeta.Count;
        }

        public static bool TryGetBoneMeta(int index, out RagdollBoneColliderType boneType, out string showName, out string boneName)
        {
            if (index >= 0 && index < GetBoneMetaCount())
            {
                showName = s_allBoneMeta[index].m_showName;
                boneName = s_allBoneMeta[index].m_boneName;
                boneType = s_allBoneMeta[index].m_boneType;
                return true;
            }

            boneType = RagdollBoneColliderType.None;
            showName = boneName = null;
            return false;
        }

        private static void GenerateBoneMeta()
        {
            s_allBoneMeta = new List<RagdollBoneMeta>();
            Type type = typeof(RagdollBoneMapper);
            var allFieldInfos = type.GetFields(BindingFlags.Public | BindingFlags.Static);
            if (allFieldInfos != null)
            {
                BLogger.Error("fieldInfo count: {0}", allFieldInfos.Length);

                foreach (var fieldInfo in allFieldInfos)
                {
                    if (fieldInfo.IsLiteral && !fieldInfo.IsInitOnly)
                    {
                        var displayName = fieldInfo.GetCustomAttribute<DisplayName>();
                        string showName = fieldInfo.Name;
                        if (displayName != null)
                        {
                            showName = displayName.displayName;
                        }

                        var boneType = RagdollBoneColliderType.None;
                        var typeAttr = fieldInfo.GetCustomAttribute<RagdollBoneColliderTypeAttr>();
                        if (typeAttr != null)
                        {
                            boneType = typeAttr.m_type;
                        }

                        var newBoneMeta = new RagdollBoneMeta(boneType, showName, fieldInfo.GetRawConstantValue() as string);
                        s_allBoneMeta.Add(newBoneMeta);
                    }
                }
            }
        }
    }
}