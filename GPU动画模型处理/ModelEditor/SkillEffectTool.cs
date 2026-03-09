using DodGame;
using UnityEditor;
using UnityEngine;

namespace A9Game
{
    public class SkillEffectTool
    {

        [MenuItem("skill resource check/检查特效资源里是否有灯光")]
        static void CheckHaveLight()
        {
            var allSelObj = Selection.GetFiltered(typeof(Object), SelectionMode.DeepAssets | SelectionMode.TopLevel);
            foreach (var selObj in allSelObj)
            {
                var goPrefab = selObj as GameObject;
                if (goPrefab == null)
                {
                    continue;
                }

                CheckAndDeleteLight(goPrefab);
            }

            AssetDatabase.SaveAssets();
        }

        [MenuItem("skill resource check/设置Animator为BaseOnRender")]
        static void CheckAnimatorRender()
        {
            var allSelObj = Selection.GetFiltered(typeof(Object), SelectionMode.DeepAssets | SelectionMode.TopLevel);
            foreach (var selObj in allSelObj)
            {
                var goPrefab = selObj as GameObject;
                if (goPrefab == null)
                {
                    continue;
                }

                CheckAnimatorRenderType(goPrefab);
            }

            AssetDatabase.SaveAssets();
        }

        [MenuItem("skill resource check/取消Animator ApplyRoot标记")]
        static void CheckAnimatorApplyRoot()
        {
            var allSelObj = Selection.GetFiltered(typeof(Object), SelectionMode.DeepAssets | SelectionMode.TopLevel);
            foreach (var selObj in allSelObj)
            {
                var goPrefab = selObj as GameObject;
                if (goPrefab == null)
                {
                    continue;
                }

                CheckAnimatorApplyRootType(goPrefab);
            }

            AssetDatabase.SaveAssets();
        }

        [MenuItem("skill resource check/默认不显示中高效果")]
        static void DisableLodGo()
        {
            EditorUtility.DisplayProgressBar("处理特效", "Please wait...", 0);
            var allSelObj = Selection.GetFiltered(typeof(Object), SelectionMode.DeepAssets | SelectionMode.TopLevel);
            for (int i = 0; i < allSelObj.Length; i++)
            {
                EditorUtility.DisplayProgressBar("处理特效", "Please wait...", (float)(i + 1) / allSelObj.Length);
                var selObj = allSelObj[i];
                var goPrefab = selObj as GameObject;
                if (goPrefab == null)
                {
                    continue;
                }

                CheckLodStatus(goPrefab);
            }
            EditorUtility.ClearProgressBar();
        }

        static void CheckAndDeleteLight(GameObject goPrefab)
        {
            string assetPath = AssetDatabase.GetAssetPath(goPrefab);
            var prefabType = PrefabUtility.GetPrefabType(goPrefab);
            if (prefabType != PrefabType.Prefab)
            {
                Debug.LogError("asset " + assetPath + " is not pefab: " + prefabType, goPrefab);
                return;
            }
            
            var goObj = PrefabUtility.InstantiatePrefab(goPrefab) as GameObject;
            var allLight = goObj.GetComponentsInChildren<Light>();
            if (allLight != null && allLight.Length > 0)
            {
                foreach (Light light in allLight)
                {
                    Debug.LogError("Destory light info: " + assetPath, goPrefab);

                    var anim = light.gameObject.GetComponent<Animator>();
                    if (anim != null)
                    {
                        Object.DestroyImmediate(anim);
                    }

                    Object.DestroyImmediate(light);
                }
            }

            if (allLight.Length > 0)
            {
                PrefabUtility.ReplacePrefab(goObj, goPrefab);
            }
            else
            {
                Debug.Log(assetPath + " check pass");
            }

            Object.DestroyImmediate(goObj);
        }

        static void CheckAnimatorRenderType(GameObject goPrefab)
        {
            string assetPath = AssetDatabase.GetAssetPath(goPrefab);
            var prefabType = PrefabUtility.GetPrefabType(goPrefab);
            if (prefabType != PrefabType.Prefab)
            {
                Debug.LogError("asset " + assetPath + " is not pefab: " + prefabType, goPrefab);
                return;
            }

            bool changed = false;
            var goObj = PrefabUtility.InstantiatePrefab(goPrefab) as GameObject;
            var allAnim = goObj.GetComponentsInChildren<Animator>();
            if (allAnim != null && allAnim.Length > 0)
            {
                foreach (var anim in allAnim)
                {
                    if (anim.cullingMode == AnimatorCullingMode.AlwaysAnimate)
                    {
                        anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                PrefabUtility.ReplacePrefab(goObj, goPrefab);
                Debug.LogError("change render type to baseonRender: " + assetPath, goPrefab);
            }
            else if (allAnim != null && allAnim.Length > 0)
            {
                Debug.Log(assetPath + " check pass");
            }

            Object.DestroyImmediate(goObj);
        }

        static void CheckAnimatorApplyRootType(GameObject goPrefab)
        {
            string assetPath = AssetDatabase.GetAssetPath(goPrefab);
            var prefabType = PrefabUtility.GetPrefabType(goPrefab);
            if (prefabType != PrefabType.Prefab)
            {
                Debug.LogError("asset " + assetPath + " is not pefab: " + prefabType, goPrefab);
                return;
            }

            bool changed = false;
            var goObj = PrefabUtility.InstantiatePrefab(goPrefab) as GameObject;
            var allAnim = goObj.GetComponentsInChildren<Animator>();
            if (allAnim != null && allAnim.Length > 0)
            {
                foreach (var anim in allAnim)
                {
                    if (anim.applyRootMotion)
                    {
                        anim.applyRootMotion = false;
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                PrefabUtility.ReplacePrefab(goObj, goPrefab);
                Debug.LogError("clear applyRoot flag: " + assetPath, goPrefab);
            }
            else if (allAnim != null && allAnim.Length > 0)
            {
                Debug.Log(assetPath + " check pass");
            }

            Object.DestroyImmediate(goObj);
        }

        private static void CheckLodStatus(GameObject goPrefab)
        {
            string assetPath = AssetDatabase.GetAssetPath(goPrefab);
            var prefabType = PrefabUtility.GetPrefabType(goPrefab);
            if (prefabType != PrefabType.Prefab)
            {
                Debug.LogError("asset " + assetPath + " is not pefab: " + prefabType, goPrefab);
                return;
            }

            
            bool changed = false;
            var goObj = PrefabUtility.InstantiatePrefab(goPrefab) as GameObject;
            LodBehaviour lod = goObj.GetComponent<LodBehaviour>();
            if (lod != null)
            {
                foreach (var highObject in lod.m_highObjects)
                {
                    if (highObject != null && highObject.activeSelf)
                    {
                        highObject.SetActive(false);
                        changed = true;
                    }
                }
                foreach (var mediemObject in lod.m_mediemObjects)
                {
//                     if (mediemObject != null)
//                     {
//                         Debug.LogError(string.Format("{0} {1} {2}", mediemObject.activeInHierarchy, mediemObject.activeSelf, mediemObject.active));
//                     }
                    if (mediemObject != null && mediemObject.activeSelf)
                    {
                        mediemObject.SetActive(false);
                        changed = true;
                    }
                }
            }

            LodBehaviourEx lodEx = goObj.GetComponent<LodBehaviourEx>();
            if (lodEx != null)
            {
                foreach (var highObject in lodEx.m_highObjects)
                {
                    if (highObject != null && highObject.activeSelf)
                    {
                        highObject.SetActive(false);
                        changed = true;
                    }
                }
                foreach (var mediemObject in lodEx.m_mediemObjects)
                {
//                     if (mediemObject != null)
//                     {
//                         Debug.LogError(string.Format("{0} {1} {2}", mediemObject.activeInHierarchy, mediemObject.activeSelf, mediemObject.active));
//                     }
                    if (mediemObject != null && mediemObject.activeSelf)
                    {
                        mediemObject.SetActive(false);
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                PrefabUtility.ReplacePrefab(goObj, goPrefab);
            }

            Object.DestroyImmediate(goObj);
        }
    }
}
