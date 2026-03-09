using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using NUnit.Framework.Constraints;
using UnityEngine;

namespace DodGame
{
    class GpuBakerMeshDebug : BSingleton<GpuBakerMeshDebug>
    {
        private GameObject m_root = null;
        private Dictionary<string, GameObject> m_debugGo = new  Dictionary<string, GameObject>();

        private Transform RootTrans
        {
            get
            {
                if (m_root == null)
                {
                    m_root = new GameObject("GpuBakDebug");
                    m_root.transform.position = Vector3.zero;
                    m_root.transform.rotation = Quaternion.identity;
                }

                return m_root.transform;
            }
        }

        public void Clear()
        {
            List<Transform> listChild = new List<Transform>();
            if (m_root != null)
            {
                GameObject.DestroyImmediate(m_root);
                m_root = null;
            }

            var rootGo = GameObject.Find("GpuBakDebug");
            if (rootGo != null)
            {
                GameObject.DestroyImmediate(rootGo);
            }

            m_debugGo.Clear();
        }

        private GpuAnimDebugPlay GetDebugPlayScript(string animTexName)
        {
            GameObject goDebug;
            if (m_debugGo.TryGetValue(animTexName, out goDebug))
            {
                return goDebug.GetComponent<GpuAnimDebugPlay>();
            }

            return null;
        }

        GpuAnimDebugPlay GetOrCreateDebugPlayScript(GpuSkinnedMeshData meshData, string animTexName)
        {
            GameObject goDebug;
            if (!m_debugGo.TryGetValue(animTexName, out goDebug))
            {
                goDebug = GameObject.Instantiate(meshData.GoPrefab);
                goDebug.name = animTexName;
                goDebug.transform.parent = RootTrans;
                goDebug.transform.localPosition = Vector3.zero;
                goDebug.transform.localRotation = Quaternion.identity;

                var allAnimator = goDebug.GetComponentsInChildren<Animator>();
                foreach (var animator in allAnimator)
                {
                    GameObject.DestroyImmediate(animator);
                }

                var skinRender = goDebug.GetComponentInChildren<SkinnedMeshRenderer>(true);
                if (skinRender != null)
                {
                    skinRender.enabled = false;
                    var goMesh = new GameObject("mesh");
                    goMesh.AddComponent<MeshFilter>();
                    var normalRender = goMesh.AddComponent<MeshRenderer>();
                    normalRender.sharedMaterials = skinRender.sharedMaterials;

                    goMesh.transform.parent = skinRender.transform.parent;
                    goMesh.transform.localPosition = Vector3.zero;
                    goMesh.transform.localRotation = Quaternion.identity;
                    goMesh.transform.localScale = Vector3.one;
                }

                goDebug.AddComponent<GpuAnimDebugPlay>();
                m_debugGo[animTexName] = goDebug;
            }

            return goDebug.GetComponent<GpuAnimDebugPlay>();
        }

        public void BegDebugMesh(GpuSkinnedMeshData meshData, string animTexName, string animName, int fps)
        {
            var debugPlay = GetOrCreateDebugPlayScript(meshData, animTexName);
            if (debugPlay == null)
            {
                return;
            }

            debugPlay.BegDebugAnimMesh(animName, fps);
        }

        public void AddDebugMesh(GpuSkinnedMeshData meshData, string animTexName, Mesh mesh, Vector3[] skinnedVertsDebug, string animName)
        {
            var debugPlay = GetOrCreateDebugPlayScript(meshData, animTexName);
            if (debugPlay == null)
            {
                return;
            }

            debugPlay.AddAnimMesh(animName, mesh, skinnedVertsDebug);
        }

        public void BindAnimTexData(string animTexName, GpuAnimTexData texData)
        {
            var debugPlay = GetDebugPlayScript(animTexName);
            if (debugPlay != null)
            {
                debugPlay.BindAnimTexData(texData);
            }
        }

        public void BindBoneInfoTexData(string mAnimTexName, GpuAnimBoneInfoTexData texData)
        {
            var debugPlay = GetDebugPlayScript(mAnimTexName);
            if (debugPlay != null)
            {
                debugPlay.BindBoneInfoTexData(texData);
            }
        }
    }
}
