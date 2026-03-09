using System.IO;
using UnityEngine;
using UnityEditor;

namespace DodGame
{
    public class SoldierAnimGenToolWindow : EditorWindow
    {
        private string outputName;
        private GameObject srcMode;
        private Material material;
        private AnimatorOverrideController anim, src;
        private Object outputDir;
        private bool LogFlag = false;
        private void Err(string msg)
        {
            if (LogFlag) Debug.LogError(msg);
        }
        private bool CheckOutputName()
        {
            if (string.IsNullOrEmpty(outputName))
            {
                Err("输出名称不能为空");
                return false;
            }
            return true;
        }
        private bool CheckSrcMode()
        {
            if (!srcMode)
            {
                Err("模型源文件为空");
                return false;
            }
            if (!srcMode.GetComponent<Animator>())
            {
                Err("模型源文件没有动画控制器");
                return false;
            }
            var renderers = srcMode.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (renderers.Length != 1)
            {
                Err("模型源文件下的SkinnedMeshRenderer组件数量不为1");
                return false;
            }
            return true;
        }
        private bool CheckMat()
        {
            if (!material)
            {
                Err("没有材质");
                return false;
            }
            if (!material.shader || material.shader.name != "Dodjoy/Actor/ActorDiffuse")
            {
                Err("材质作色器设置错误");
                return false;
            }
            return true;
        }
        private bool CheckOutputDir()
        {
            if (outputDir)
            {
                var path = AssetDatabase.GetAssetPath(outputDir);
                if (!string.IsNullOrEmpty(path))
                {
                    path = Application.dataPath.Replace("Asstes", path);
                    if (Directory.Exists(path))
                    {
                        return true;
                    }
                }
            }
            Err("输出目录不存在");
            return false;
        }
        private Color defColor;
        private void SetErrColor(bool notErr)
        {
            if (notErr)
            {
                GUI.color = defColor;
            }
            else
            {
                GUI.color = Color.red;
            }
        }
        private void OnGUI()
        {
            LogFlag = false;
            defColor = GUI.color;
            SetErrColor(CheckOutputName());
            outputName = EditorGUILayout.TextField(outputName);
            SetErrColor(CheckSrcMode());
            var mode = EditorGUILayout.ObjectField("模型源文件", srcMode, typeof(GameObject), true) as GameObject;
            if (mode != srcMode)
            {
                srcMode = mode;
                outputName = TryGetOutputName(mode);
            }
            SetErrColor(CheckMat());
            material = EditorGUILayout.ObjectField("材质", material, typeof(Material), true) as Material;
            SetErrColor(anim);
            anim = EditorGUILayout.ObjectField("动画控制器", anim, typeof(AnimatorOverrideController), true) as AnimatorOverrideController;
            SetErrColor(src);
            src = EditorGUILayout.ObjectField("GPU动画控制器", src, typeof(AnimatorOverrideController), true) as AnimatorOverrideController;
            SetErrColor(CheckOutputDir());
            outputDir = EditorGUILayout.ObjectField("输出目录", outputDir, typeof(DefaultAsset), true);
            SetErrColor(true);
            if (GUILayout.Button("生成"))
            {
                LogFlag = true;
                if (CheckOutputName() && CheckSrcMode() && CheckMat() && anim && src && CheckOutputDir())
                {
                    var ap = AssetDatabase.GetAssetPath(outputDir) + "/{0}.prefab";
                    var tmp = Instantiate(srcMode) as GameObject;
                    tmp.GetComponentInChildren<SkinnedMeshRenderer>().material = material;
                    tmp.name = outputName + "_low";
                    tmp.GetComponent<Animator>().runtimeAnimatorController = anim;
                    bool suc;
                    var p1 = PrefabUtility.SaveAsPrefabAsset(tmp, string.Format(ap, tmp.name), out suc);
                    if (!suc)
                    {
                        DestroyImmediate(tmp, true);
                        DestroyImmediate(p1, true);
                        Err("预制保存失败");
                        return;
                    }
                    tmp.name = outputName + "_low_source";
                    tmp.GetComponent<Animator>().runtimeAnimatorController = src;
                    ap = string.Format(ap, tmp.name);
                    var p2 = PrefabUtility.SaveAsPrefabAsset(tmp, ap, out suc);
                    if (!suc)
                    {
                        DestroyImmediate(tmp, true);
                        DestroyImmediate(p1, true);
                        DestroyImmediate(p2, true);
                        Err("预制保存失败");
                        return;
                    }
                    DestroyImmediate(tmp, true);
                    GpuAnimEditor.InitLog();
                    GpuAnimEditor.ClearDebug();
                    GpuAnimBakeMgr.Instance.Reload();
                    var bakCfg = GpuAnimEditor.GetBakConfig(ap);
                    GpuAnimEditor.InitLog();
                    GpuAnimEditor.SaveBakConfig(bakCfg);
                    GpuAnimBakeMgr.Instance.AddAnimBakTask(bakCfg);
                }
            }
        }
        private void OnEnable()
        {
            GpuAnimEditor.InitLog();
            GpuAnimEditor.ClearDebug();
            GpuAnimBakeMgr.Instance.Reload();
        }
        private void Update()
        {
            GpuAnimBakeMgr.Instance.ExecuteBake(null, null);
        }
        private string TryGetOutputName(Object obj)
        {
            try
            {
                var name = obj.name;
                var sidx = name.IndexOf('_');
                return name.Substring(sidx + 1, name.IndexOf('_', sidx + 1) - sidx - 1);
            }
            catch (System.Exception)
            {
                return outputName;
            }
        }
        [MenuItem("GpuInstance/小兵低模动画预制生成工具")]
        private static void ShowWindow()
        {
            GetWindow<SoldierAnimGenToolWindow>(true, "动画生成工具", true);
        }
    }
}
