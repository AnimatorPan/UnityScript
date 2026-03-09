using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DodGame
{
    class GpuSkinnedMeshUtil
    {

        public static bool MergeBone(SkinnedMeshRenderer[] arrRenderer, out Transform[] arrBone, out Matrix4x4[] arrBindPose, out GpuAnimMeshData[] arrMeshData)
        {
            arrBone = null;
            arrBindPose = null;
            arrMeshData = null;

            var listBone = new List<Transform>();
            var listBindPos = new List<Matrix4x4>();
            var listRenderer = new List<SkinnedMeshRenderer>(arrRenderer);
            listRenderer.Sort(GpuAnimMgr.SortSkinnedMeshRenderer);

            for (int i = 0; i < listRenderer.Count; ++i)
            {
                var renderer = listRenderer[i];
                var mesh = renderer.sharedMesh;

                //处理骨骼优化
                var meshAssetPath = AssetDatabase.GetAssetPath(mesh);
                var modelImporter = ModelImporter.GetAtPath(meshAssetPath) as ModelImporter;
                if (modelImporter != null)
                {
                    if (modelImporter.optimizeGameObjects)
                    {
                        modelImporter.optimizeGameObjects = false;
                        modelImporter.SaveAndReimport();

                        //AssetDatabase.ImportAsset(meshAssetPath, ImportAssetOptions.Default);
                    }
                }

                var bones = renderer.bones;
                var bindposes = mesh.bindposes;

                if (bones == null || bones.Length <= 0)
                {
                    BLogger.Error("Invalid bones, please clear fbx Optimize GameObjects option: {0}, {1}", renderer.name, meshAssetPath);
                    return false;
                }

                if (bindposes == null || bindposes.Length <= 0)
                {
                    BLogger.Error("Invalid bindpos, please clear fbx Optimize GameObjects option: {0}, {1}", renderer.name, meshAssetPath);
                    return false;
                }

                //BLogger.Error("mesh:{0} bone count:{1}, bindPos:{2}", skinnedMesh.name, bones.Length, checkBindPose.Length);
                for (int j = 0; j != bones.Length; ++j)
                {
                    var bone = bones[j];
                    var bindPose = bindposes[j];
                    BLogger.Assert(bindPose.determinant != 0, "The bind pose can't be 0 matrix.");

                    // the bind pose is correct base on the skinnedMeshRenderer, so we need to replace it
                    int index = listBone.FindIndex(q => q == bone);
                    if (index < 0)
                    {
                        listBone.Add(bone);
                        if (listBindPos != null)
                        {
                            listBindPos.Add(bindPose);
                        }
                    }
                    else
                    {
                        listBindPos[index] = bindPose;
                    }
                }

                renderer.enabled = false;
            }

            arrBone = listBone.ToArray();
            arrBindPose = listBindPos.ToArray();

            // 骨骼合并完成后，再更新各个Mesh顶点骨骼Index和权重
            arrMeshData = new GpuAnimMeshData[listRenderer.Count];
            for (int i = 0; i < listRenderer.Count; i++)
            {
                var renderer = listRenderer[i];
                var mesh = renderer.sharedMesh;
                var bones = renderer.bones;
                var boneWeights = mesh.boneWeights;

                var meshData = arrMeshData[i] = new GpuAnimMeshData();
                meshData.meshBoneIndex = new Vector4[boneWeights.Length];
                meshData.meshBoneWeight = new Vector4[boneWeights.Length];
                for (int j = 0; j < boneWeights.Length; j++)
                {
                    var boneWeight = boneWeights[j];
                    var boneIndex0 = listBone.IndexOf(bones[boneWeight.boneIndex0]);
                    var boneIndex1 = listBone.IndexOf(bones[boneWeight.boneIndex1]);
                    var boneIndex2 = listBone.IndexOf(bones[boneWeight.boneIndex2]);
                    var boneIndex3 = listBone.IndexOf(bones[boneWeight.boneIndex3]);

                    //if (boneIndex0 != boneWeight.boneIndex0 || boneIndex1 != boneWeight.boneIndex1 ||
                    //    boneIndex2 != boneWeight.boneIndex2 || boneIndex3 != boneWeight.boneIndex3)
                    //{
                    //    Debug.Log("test");
                    //}

                    meshData.meshBoneIndex[j] = new Vector4(boneIndex0, boneIndex1, boneIndex2, boneIndex3);
                    meshData.meshBoneWeight[j] = new Vector4(boneWeight.weight0, boneWeight.weight1, boneWeight.weight2, boneWeight.weight3);
                }
            }

            return true;
        }

        public static Matrix4x4[] CalculateSkinMatrix(Transform[] bonePose,
            Matrix4x4[] bindPose,
            Matrix4x4 rootMatrix1stFrame,
            bool haveRootMotion)
        {
            if (bonePose.Length == 0)
                return null;

            Transform root = bonePose[0];
            while (root.parent != null)
            {
                root = root.parent;
            }
            Matrix4x4 rootMat = root.worldToLocalMatrix;

            Matrix4x4[] matrix = new Matrix4x4[bonePose.Length];
            for (int i = 0; i != bonePose.Length; ++i)
            {
                matrix[i] = rootMat * bonePose[i].localToWorldMatrix * bindPose[i];
            }
            return matrix;
        }

        public static void Convert2Color(Color[] dest, ref int destIndex, Matrix4x4[] boneMatrix, int boneCount)
        {
            foreach (var obj in boneMatrix)
            {
                dest[destIndex++] = obj.GetRow(0);
                dest[destIndex++] = obj.GetRow(1);
                dest[destIndex++] = obj.GetRow(2);
                dest[destIndex++] = obj.GetRow(3);
            }
        }

        public static void Convert2ColorDebug(Vector4[] dest, ref int destIndex, Matrix4x4[] boneMatrix, int boneCount)
        {
            foreach (var obj in boneMatrix)
            {
                dest[destIndex++] = obj.GetRow(0);
                dest[destIndex++] = obj.GetRow(1);
                dest[destIndex++] = obj.GetRow(2);
                dest[destIndex++] = obj.GetRow(3);
            }
        }
    }

    class GpuBoneInfoUtil
    {
        private static List<Transform> m_cash=new List<Transform>();
        private static List<Transform> m_cashWeak=new List<Transform>();
        private static Transform[] m_dummyCache = new Transform[(int)DummyPoint.DM_MAX];
        //private static BoneInfo[] m_boneInfoOffsetCache = new BoneInfo[(int)];
        public static BoneInfo[] CalculateBoneInfo(GpuSkinnedMeshData meshData, List<string> dummyNames, Transform[] bonePose)
        {
            Transform root = bonePose[0];
            while (root.parent != null)
            {
                root = root.parent;
            }
            BoneInfo[] boneInfoOffsets = new BoneInfo[dummyNames.Count];
            for (int i = 0; i < dummyNames.Count; i++)
            {
                var dummyName = dummyNames[i];
                if (meshData.TryGetDummy(dummyName, out var dummy))
                {
                    boneInfoOffsets[i] = new BoneInfo();
                    boneInfoOffsets[i].m_positionOffset
                        = dummy.position - root.position;

                    boneInfoOffsets[i].m_forward
                        = dummy.forward;

                    boneInfoOffsets[i].m_rotation
                        = dummy.eulerAngles-root.eulerAngles;

                    boneInfoOffsets[i].m_scale
                        = dummy.localScale;
                }
            }

            return boneInfoOffsets;
        }
        public static BoneInfo[] CalculateBoneInfo(int cnt, Transform[] bonePose, GpuAnimSourceAssetConfig config)
        {
            m_cash.Clear();
            m_cashWeak.Clear();
            if (bonePose.Length == 0)
                return null;
            Transform root = bonePose[0];
            while (root.parent != null)
            {
                root = root.parent;
            }
            m_cash.Add(root);
            
            BoneInfo[] boneInfoOffsetCache = new BoneInfo[cnt];
            for (int index = 0; index < m_dummyCache.Length; index++)
            {
                m_dummyCache[index] = null;
            }

            for (int index = 0; index < boneInfoOffsetCache.Length; index++)
            {
                boneInfoOffsetCache[index] = null;
            }
            var weakTransFormCount = config.m_etrBoneNum;
            for (int i = 0; i < weakTransFormCount; i++)
                m_cashWeak.Add(null);
            for (int index = 0; index < m_cash.Count; index++)
            {
                var currTrans = m_cash[index];
                var currDm = AvatarDefine.GetDummyPointByName(currTrans.name);
                if (currDm != DummyPoint.DM_NONE)
                {
                    m_dummyCache[(int)currDm] = currTrans;
                }
                var etrBoneIndex = config.GetEtrBoneIndex(currTrans.name);

                if (etrBoneIndex >= 0)
                {
                    m_cashWeak[etrBoneIndex] = currTrans;
                }
                for (int k = 0; k < currTrans.childCount; k++)
                {
                    m_cash.Add(currTrans.GetChild(k));
                }
            }
            //Transform dummyRoot = m_dummyCache[(int)DummyPoint.DM_ROOT];
            //if (dummyRoot==null)
            //{
            //    Debug.LogError(root.name+"do not have "+ DummyPoint.DM_ROOT);
            //    return null;
            //}
            for (int index = 0; index < boneInfoOffsetCache.Length; index++)
            {
                if (index< AvatarDefine.m_needRecordBoneInfo.Count)
                {
                    int i = (int)AvatarDefine.m_needRecordBoneInfo[index];
                    if (i >= 0 && m_dummyCache[i] != null)
                    {
                        boneInfoOffsetCache[index] = new BoneInfo();
                        boneInfoOffsetCache[index].m_positionOffset
                            = m_dummyCache[i].position - root.position;

                        boneInfoOffsetCache[index].m_forward
                            = m_dummyCache[i].forward;

                        boneInfoOffsetCache[index].m_rotation
                            = m_dummyCache[i].eulerAngles-root.eulerAngles;

                        boneInfoOffsetCache[index].m_scale
                            = m_dummyCache[i].localScale;
                    }
                }
                else
                {
                    var weakIndex = index - AvatarDefine.m_needRecordBoneInfo.Count;
                    boneInfoOffsetCache[index] = new BoneInfo();
                    if (m_cashWeak[weakIndex] != null)
                    {
                        boneInfoOffsetCache[index].m_positionOffset
                        = m_cashWeak[weakIndex].position - root.position;

                        boneInfoOffsetCache[index].m_forward
                            = m_cashWeak[weakIndex].forward;

                        boneInfoOffsetCache[index].m_rotation
                            = m_cashWeak[weakIndex].eulerAngles;

                        boneInfoOffsetCache[index].m_scale
                            = m_cashWeak[weakIndex].localScale;
                    }
                }
            }
            return boneInfoOffsetCache;
        }

        private static bool CheckIsWeakTrans(string currTransName, string[] weakTransform)
        {
            for (int i = 0; i < weakTransform.Length; i++)
            {
                var weakTrans = weakTransform[i];
                if (!string.IsNullOrEmpty(weakTrans)&&weakTrans== currTransName)
                {
                    return true;
                }
            }

            return false;
        }
    }

}
