using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace A9Game
{
    class ModelCombineMeshNode
    {
        public Renderer m_render;
        public Mesh m_mesh;

        public ModelCombineMeshNode(Renderer render, Mesh mesh)
        {
            m_render = render;
            m_mesh = mesh;
        }
    }

    public class ModelMeshCombineTool
    {
        [MenuItem("ResourceRuleCheck/合并模型Mesh")]
        private static void CombineSelect()
        {
            var allSelObj = Selection.GetFiltered(typeof(GameObject), SelectionMode.TopLevel);
            if (allSelObj != null)
            {
                foreach (var selObj in allSelObj)
                {
                    var selGo = selObj as GameObject;
                    CombineMesh(selGo);
                    RefreshGo(selGo);
                }
            }
        }

        public static bool CheckAvatarFromFbx(GameObject go)
        {
            bool changed = false;
            var allAnimator = go.GetComponentsInChildren<Animator>();
            if (allAnimator != null)
            {
                for (int i = 0; i < allAnimator.Length; i++)
                {
                    var animator = allAnimator[i];
                    var sharedAvatar = animator.avatar;
                    if (sharedAvatar != null)
                    {
                        string avatarPath = AssetDatabase.GetAssetPath(sharedAvatar);
                        if (avatarPath.EndsWith("fbx", true, CultureInfo.CurrentCulture))
                        {
                            string avtarName = Path.GetFileNameWithoutExtension(avatarPath);
                            string newAvatarPath = Path.GetDirectoryName(avatarPath) + "/" + avtarName + "_avtar.asset";

                            var newAvtar = UnityEngine.Object.Instantiate(sharedAvatar);
                            DodEditorUtil.SaveAsset(newAvtar, newAvatarPath);
                            sharedAvatar = AssetDatabase.LoadAssetAtPath(newAvatarPath, typeof(Avatar)) as Avatar;
                            Debug.LogError(string.Format("save avatar[{0}] to new asset: {1} ", avatarPath, newAvatarPath), sharedAvatar);

                            animator.avatar = sharedAvatar;
                            changed = true;
                        }
                    }
                }
            }

            return changed;
        }
        
        public static bool CombineMesh(GameObject go)
        {
            bool changed = false;
            var lodGroup = go.GetComponent<LODGroup>();
            if (lodGroup != null)
            {
                LodGroupInfo groupInfo = new LodGroupInfo(lodGroup);
                for (int i = 0; i < groupInfo.GetLodCount(); i++)
                {
                    List<Renderer> listSubRender = new List<Renderer>();
                    var lodItemInfo = groupInfo.GetLodItemInfo(i);
                    for (int k = 0; k < lodItemInfo.MeshCount; k++)
                    {
                        var subRender = lodItemInfo.GetRenderer(k);
                        if (subRender != null)
                        {
                            listSubRender.Add(subRender);
                        }
                        else if (k != 0)
                        {
                            Debug.LogError("invalid sub render, LodIndex: " + i + "mesh index: " + k + ", " + go.name);
                        }
                    }

                    if (listSubRender.Count > 1)
                    {
                        var newMesh = CombineRenderList(go, listSubRender.ToArray());
                        if (newMesh != null)
                        {
                            changed = true;
                            lodItemInfo.ClearReander();
                            lodItemInfo.AddRender(newMesh.GetComponent<Renderer>());
                            groupInfo.ApplyModifiedProperties();
                        }
                    }
                }

            }
            else
            {
                var allRender = go.GetComponentsInChildren<Renderer>(go);
                if (CombineRenderList(go, allRender) != null)
                {
                    changed = true;
                }
            }

            return changed;
        }

        private static void RefreshGo(GameObject go)
        {
        }

        private static GameObject CombineRenderList(GameObject goRoot, Renderer[] allRender)
        {
            ///只合并一级mehs，里面骨骼的可能是其他的挂点物件，不管
            bool isSkinnedMesh = false;
            bool isMeshFilter = false;

            List<Transform> listBones = new List<Transform>();
            List<ModelCombineMeshNode> listToCombine = new List<ModelCombineMeshNode>();
            Mesh sharedMesh = null;

            Transform shareMeshParent = null;

            foreach (var renderer in allRender)
            {
                var parentTrans = renderer.transform.parent;
                if (parentTrans == null)
                {
                    continue;
                }


                ///一定要公共的父节点
                if (parentTrans == goRoot.transform ||
                    parentTrans.GetComponent<Animator>() != null)
                {
                    if (shareMeshParent == null)
                    {
                        shareMeshParent = parentTrans;
                    }

                    if (parentTrans == shareMeshParent)
                    {
                        if (renderer is SkinnedMeshRenderer)
                        {
                            var skinnMeshRender = renderer as SkinnedMeshRenderer;
                            sharedMesh = skinnMeshRender.sharedMesh;
                            isSkinnedMesh = true;

                            //Debug.LogError("skinnMeshRender bone: " + skinnMeshRender.bones.Length);
                            listBones.AddRange(skinnMeshRender.bones);
                        }
                        else if (renderer is MeshRenderer)
                        {
                            var meshFilter = renderer.GetComponent<MeshFilter>();
                            if (meshFilter != null)
                            {
                                sharedMesh = meshFilter.sharedMesh;
                            }

                            isMeshFilter = true;
                        }

                        if (sharedMesh != null)
                        {
                            listToCombine.Add(new ModelCombineMeshNode(renderer, sharedMesh));
                        }
                    }
                }
            }

            if (isMeshFilter && isSkinnedMesh || !isMeshFilter && !isSkinnedMesh || shareMeshParent == null)
            {
                Debug.LogError("合并失败,不能同时包含MeshFilter和SkinnedMesh: " + goRoot.name
                    + ",sharedMesh: " + (sharedMesh != null ? sharedMesh.name : "Null"), goRoot);

                EditorUtility.DisplayDialog("合并失败,不能同时包含MeshFilter和SkinnedMesh: " + goRoot.name
                    + ",sharedMesh: " + (sharedMesh != null?sharedMesh.name:"Null"), null, "ok");
                return null;
            }
            
            return DoCombineMesh(goRoot, shareMeshParent, listToCombine, isSkinnedMesh);
        }

        private static GameObject DoCombineMesh(GameObject goRoot, Transform shareParent, List<ModelCombineMeshNode> listToCombine, bool isSkinnedMesh)
        {
            ///判断是不是公用的材质
            SkinnedMeshRenderer firstSkinMeshRender = null;
            MeshRenderer firstMeshRenderer = null;
            Material sharedMat = null;
            foreach (var combineMesh in listToCombine)
            {
                if (isSkinnedMesh && firstSkinMeshRender == null)
                {
                    firstSkinMeshRender = combineMesh.m_render as SkinnedMeshRenderer;
                }
                else if (!isSkinnedMesh && firstMeshRenderer == null)
                {
                    firstMeshRenderer = combineMesh.m_render as MeshRenderer;
                }

                if (sharedMat == null)
                {
                    sharedMat = combineMesh.m_render.sharedMaterial;
                }
                else
                {
                    if (sharedMat != combineMesh.m_render.sharedMaterial)
                    {
                        Debug.LogError("合并失败,存在不同的材质: " + goRoot.name);
                        EditorUtility.DisplayDialog("合并失败,存在不同的材质: " + goRoot.name, null, "ok");
                        return null;
                    }
                }
            }

            if (sharedMat == null)
            {
                Debug.LogError("合并失败,材质丢失了: " + goRoot.name, goRoot);
                EditorUtility.DisplayDialog("合并失败,材质丢失了: " + goRoot.name, null, "ok");
                return null;
            }

            ////开始计算mesh
            List<Transform> listBones = new List<Transform>();
            List<CombineInstance> listInst = new List<CombineInstance>();
            for (int i = 0; i < listToCombine.Count; i++)
            {
                var combineMeshNode = listToCombine[i];
                var newInst = new CombineInstance();
                newInst.mesh = combineMeshNode.m_mesh;
                newInst.subMeshIndex = 0;

                //var trans = combineMeshNode.m_render.transform;
                //newInst.transform = trans.parent.worldToLocalMatrix * trans.localToWorldMatrix;
                newInst.transform = Matrix4x4.identity;
                listInst.Add(newInst);

                if (isSkinnedMesh)
                {
                    var toAddSkinMesn = combineMeshNode.m_render as SkinnedMeshRenderer;
                    listBones.AddRange(toAddSkinMesn.bones);
                }
            }

            if (listToCombine.Count <= 1)
            {
                return null;
            }

            ///开始合并
            Mesh resultMesh = new Mesh();
            resultMesh.CombineMeshes(listInst.ToArray());
            resultMesh.RecalculateBounds();
            resultMesh.RecalculateNormals();

            ///保存下来
            string srcPath = AssetDatabase.GetAssetPath(listInst[0].mesh);
            string meshName = "cmb_" + Path.GetFileNameWithoutExtension(srcPath);
            var allChildMesh = AssetDatabase.LoadAllAssetsAtPath(srcPath);
            if (allChildMesh != null && allChildMesh.Length > 1)
            {
                meshName += string.Format("_{0}", listInst[0].mesh.name);
            }

            string combinePath = Path.GetDirectoryName(srcPath) + "/" + meshName + ".asset";
            ///AssetDatabase.CreateAsset(resultMesh, combinePath);
            DodEditorUtil.SaveAsset(resultMesh, combinePath);
            AssetDatabase.SaveAssets();
            resultMesh = AssetDatabase.LoadAssetAtPath(combinePath, typeof(Mesh)) as Mesh;

            ///创建节点
            GameObject newGo = new GameObject(meshName);
            newGo.transform.parent = shareParent;
            newGo.transform.localPosition = listToCombine[0].m_render.gameObject.transform.localPosition;
            newGo.transform.localRotation = listToCombine[0].m_render.gameObject.transform.localRotation;

            if (isSkinnedMesh)
            {
                var newSkinMeshRender = newGo.AddComponent<SkinnedMeshRenderer>();
                newSkinMeshRender.sharedMaterial = sharedMat;
                newSkinMeshRender.sharedMesh = resultMesh;
                newSkinMeshRender.quality = firstSkinMeshRender.quality;

                newSkinMeshRender.bones = listBones.ToArray();
                newSkinMeshRender.rootBone = firstSkinMeshRender.rootBone;
                newSkinMeshRender.useLightProbes = firstSkinMeshRender.useLightProbes;
            }
            else
            {
                var newMeshFilter = newGo.AddComponent<MeshFilter>();
                var newMeshRender = newGo.AddComponent<MeshRenderer>();
                newMeshFilter.sharedMesh = resultMesh;
                newMeshRender.sharedMaterial = sharedMat;
                newMeshRender.useLightProbes = firstMeshRenderer.useLightProbes;
            }
            
            for (int i = 0; i < listToCombine.Count; i++)
            {
                var combineMeshNode = listToCombine[i];
                GameObject.DestroyImmediate(combineMeshNode.m_render.gameObject);
                //combineMeshNode.m_render.gameObject.SetActive(false);
            }

            if (newGo != null)
            {
                CheckAvatarFromFbx(goRoot);
            }

            return newGo;
        }
    }
}
