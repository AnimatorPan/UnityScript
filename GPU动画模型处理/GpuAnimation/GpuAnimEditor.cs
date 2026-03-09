using System;
using System.Diagnostics;
using System.IO;
using DEditorUIRuntime;
using Sirenix.OdinInspector.Editor;
//using A9Game;
using UnityEditor;
using UnityEngine;


namespace DodGame
{
    public class GPUAnimParam
    {
        public string m_destPath;
        public Shader m_shader;
        public string m_materialNameSuffix;
    }

    /// <summary>
    /// 动画对象GPU Instance编辑工具
    /// </summary>
    public class GpuAnimEditor : EditorWindow
    {
        private static GpuAnimEditor s_window;

        private GameObject m_srcPrefab = null;
        private GpuAnimSourceAssetConfig m_bakConfig = null;
        private Func<bool> m_onFinish;
        private GPUAnimParam m_param;

        private bool m_auto;
        private GameObject SrcPrefab
        {
            set
            {
                if (m_srcPrefab != value)
                {
                    m_srcPrefab = value;
                    LoadOrNewBakConfig();
                }
            }
        }


        [MenuItem("GpuInstance/Animation Generator", false)]
        private static void OpenAnimEditor()
        {
            InitLog();

            ClearDebug();
            GpuAnimBakeMgr.Instance.Reload();
            s_window = GetWindow(typeof(GpuAnimEditor), false, "动画烘培") as GpuAnimEditor;
        }

        public static void InitLog()
        {
            BLogger.SetLogHandler(new GameLogHandler());
            BLogger.SetLevel((uint)BLogLevel.ALL);
        }

        private void OnInspectorUpdate()
        {
            //GpuAnimBakeMgr.Instance.Update();
        }

        public void Init(GameObject prefab, GPUAnimParam param, Func<bool> onFinish, bool auto = false, string[] additionBones = null)
        {
            SrcPrefab = prefab;
            m_onFinish = onFinish;
            m_param = param;
            m_auto = auto;
            m_bakConfig.m_additionBOnes = additionBones;
        }

        private void OnGUI()
        {
            SrcPrefab = EditorGUILayout.ObjectField("选择模型Prefab", m_srcPrefab, typeof(GameObject)) as GameObject;
            if (m_srcPrefab != null && m_bakConfig != null)
            {
                m_bakConfig.m_fps = EditorGUILayout.IntField("动画FPS", m_bakConfig.m_fps);
                m_bakConfig.m_animTexName = EditorGUILayout.TextField("动画贴图名称", m_bakConfig.m_animTexName);
                if (!string.IsNullOrEmpty(m_bakConfig.m_boneInfoTexName))
                {
                    m_bakConfig.m_boneInfoTexName = EditorGUILayout.TextField("骨骼信息名称", m_bakConfig.m_boneInfoTexName);
                }
                else
                {
                    m_bakConfig.m_boneInfoTexName = EditorGUILayout.TextField("骨骼信息名称", "BoneInfoTex_"+m_srcPrefab.name);
                }
                
                m_bakConfig.m_etrBoneNum = EditorGUILayout.IntField("额外记录骨骼个数", m_bakConfig.m_etrBoneNum);
                for (int i = 0; i < m_bakConfig.m_etrBoneNum; i++)
                {
                    m_bakConfig.m_etrBoneName[i] = EditorGUILayout.TextField("额外骨骼名字", m_bakConfig.m_etrBoneName[i]);
                }

                var gpuAnimMaterial = (Material)EditorGUILayout.ObjectField("GpuAnim材质: ", m_bakConfig.gpuAnimMaterial, typeof(Material), allowSceneObjects: false);
                m_bakConfig.SetMaterial(gpuAnimMaterial);
                //var boneConfig = m_srcPrefab.GetComponent<BoneConfig>();
                //if (boneConfig==null)
                //{
                //    if (GUILayout.Button("Add BoneConfig"))
                //    {
                //        m_srcPrefab.AddComponent<BoneConfig>();
                //        EditorUtility.SetDirty(m_srcPrefab);
                //        AssetDatabase.SaveAssets();
                //        AssetDatabase.Refresh();
                //    }
                //}
                
                //if (GUILayout.Button("Bak"))
                //{
                //    InitLog();
                //    SaveBakConfig(m_bakConfig);
                //    BakGpuAnim();
                //}

                if (GUILayout.Button("烘焙"))
                {
                    InitLog();
                    ClearDebug();
                    SaveBakConfig(m_bakConfig);
                    GpuAnimBakeMgr.Instance.Reload();
                    RefreshBagGpuAnim();
                    GpuAnimBakeMgr.Instance.ExecuteBake(()=> {
                        m_onFinish?.Invoke();
                        Close();
                    }, m_param);
                }

                if (m_auto)
                {
                    m_auto = false;
                    InitLog();
                    ClearDebug();
                    SaveBakConfig(m_bakConfig);
                    GpuAnimBakeMgr.Instance.Reload();
                    RefreshBagGpuAnim();
                    GpuAnimBakeMgr.Instance.ExecuteBake(()=> {
                        m_onFinish?.Invoke();
                        Close();
                    }, m_param);
                }
            }

            if (GUILayout.Button("Clear Debug"))
            {
                ClearDebug();
            }
        }

        public static void ClearDebug()
        {
            GpuSkinnedMeshCache.Instance.Reset();
            GpuBakerMeshDebug.Instance.Clear();
        }

        /// <summary>
        /// 烘培动画到贴图中
        /// </summary>
        private void BakGpuAnim()
        {
            if (m_srcPrefab == null || string.IsNullOrEmpty(m_bakConfig.m_srcAssetPath))
            {
                return;
            }

            GpuAnimBakeMgr.Instance.AddAnimBakTask(m_bakConfig);
        }

        private void RefreshBagGpuAnim()
        {
            if (m_srcPrefab == null || string.IsNullOrEmpty(m_bakConfig.m_srcAssetPath))
            {
                return;
            }
            
            GpuAnimBakeMgr.Instance.AddRefreshAnimBakTask(m_bakConfig);
        }

        private string GetOutDir()
        {
            if (m_srcPrefab == null)
            {
                return string.Empty;
            }

            var srcAssetPath = AssetDatabase.GetAssetPath(m_srcPrefab);
            var assetFileName = Path.GetFileNameWithoutExtension(srcAssetPath);
            var outPath = "ModelGpuAnim/" + assetFileName;
            return outPath;
        }

        private void LoadOrNewBakConfig()
        {
            if (m_srcPrefab == null)
            {
                m_bakConfig = null;
                return;
            }

            var assetPath = AssetDatabase.GetAssetPath(m_srcPrefab);
            if (m_bakConfig != null && assetPath == m_bakConfig.m_srcAssetPath)
            {
                return;
            }

            m_bakConfig = GetBakConfig(assetPath);
            if (m_bakConfig == null)
            {
                m_bakConfig = new GpuAnimSourceAssetConfig(assetPath);
            }
        }

        private static string GetBakConfigFileName(string assetPath)
        {
            var path = Path.GetDirectoryName(assetPath) + Path.GetFileNameWithoutExtension(assetPath);
            return path.ToLower().Replace("/", "_") + ".txt";
        }

        private static string GetAnimXmlDir()
        {
            string assetPath = Application.dataPath + "/../EditorData/GpuInstEditor/AnimBakXml/";
            return assetPath;
        }

        public static GpuAnimSourceAssetConfig GetBakConfig(string assetPath)
        {
            var configFileName = GetBakConfigFileName(assetPath);
            var xmlPath = GetAnimXmlDir() + configFileName;
            var bakCfg = XmlSerializeUtil.DeSerializeFromXmlFile<GpuAnimSourceAssetConfig>(xmlPath);
            if (bakCfg == null)
            {
                bakCfg = new GpuAnimSourceAssetConfig(assetPath);
            }

            return bakCfg;
        }

        public static bool SaveBakConfig(GpuAnimSourceAssetConfig configData)
        {
            if (configData == null || string.IsNullOrEmpty(configData.m_srcAssetPath))
            {
                return false;
            }

            var assetPath = configData.m_srcAssetPath;
            var configFileName = GetBakConfigFileName(assetPath);
            var xmlPath = GetAnimXmlDir() + configFileName;

            return XmlSerializeUtil.SerializeToXmlFile<GpuAnimSourceAssetConfig>(configData, xmlPath);
        }

    }

    public class GameLogHandler : BIDLogHandler
    {
        public void Log(BLogEvent dLogEvent)
        {
            switch (dLogEvent.Level)
            {
                case BLogLevel.NONE:
                    return;
                case BLogLevel.DEBUG:
                    UnityEngine.Debug.LogFormat("GpuAnimEditor Debug==> Time: {0} Message: {1}", dLogEvent.Time.ToString(), dLogEvent.Message);
                    break;
                case BLogLevel.INFO:
                    UnityEngine.Debug.LogFormat("GpuAnimEditor Info==> Time: {0} Message: {1}", dLogEvent.Time.ToString(), dLogEvent.Message);
                    break;
                case BLogLevel.WARNING:
                    UnityEngine.Debug.LogWarningFormat("GpuAnimEditor Warning==> Time: {0} Message: {1}", dLogEvent.Time.ToString(), dLogEvent.Message);
                    break;
                case BLogLevel.ERROR:
                    UnityEngine.Debug.LogErrorFormat("GpuAnimEditor Error==> Time: {0} Message: {1}", dLogEvent.Time.ToString(), dLogEvent.Message);
                    break;
                case BLogLevel.FATAL:
                    UnityEngine.Debug.LogErrorFormat("GpuAnimEditor Fatal==> Time: {0} Message: {1}", dLogEvent.Time.ToString(), dLogEvent.Message);
                    break;
            }
        }
    }
}
