
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;
using Unity.EditorCoroutines.Editor;

namespace DodGame
{

    [Serializable]
    public class GpuAnimSourceAssetConfig
    {
        /// <summary>
        /// 烘培的模型prefab
        /// </summary>
        public string m_srcAssetPath;

        /// <summary>
        /// 烘培的动画帧率
        /// </summary>
        public int m_fps = 30;

        /// <summary>
        /// 烘培的贴图文件名
        /// </summary>
        public string m_animTexName;

        /// <summary>
        /// 烘培的骨骼信息文件名
        /// </summary>
        public string m_boneInfoTexName;

        public int m_etrBoneNum = 1;
        public string[] m_etrBoneName=new string[5];
        public string[] m_additionBOnes;

        public string m_gpuAnimMaterialPath;

        [NonSerialized]
        private Material m_material;

        [NonSerialized]
        public GPUAnimParam m_param;

        //[NonSerialized]
        public Material gpuAnimMaterial
        {
            get
            {
                if (m_material == null)
                {
                    if (!string.IsNullOrEmpty(m_gpuAnimMaterialPath))
                    {
                        m_material = AssetDatabase.LoadAssetAtPath<Material>(m_gpuAnimMaterialPath);
                    }
                }
                return m_material;
            }
        }

        public void SetMaterial(Material value)
        {
            if (value == m_material)
                return;
            if (value != null)
            {
                m_gpuAnimMaterialPath = AssetDatabase.GetAssetPath(value);
            }
            m_material = value;
        }

        public int GetEtrBoneIndex(string boneName)
        {
            for (int i = 0; i < m_etrBoneNum; i++)
            {
                if (boneName == m_etrBoneName[i])
                    return i;
            }

            return -1;
        }

        public GpuAnimSourceAssetConfig()
        {
        }

        public GpuAnimSourceAssetConfig(string assetPath)
        {
            m_srcAssetPath = assetPath;
            m_animTexName = "AnimTex_" + Path.GetFileNameWithoutExtension(assetPath);
            m_boneInfoTexName = "BoneInfoTex_" + Path.GetFileNameWithoutExtension(assetPath);
        }

        public GpuAnimSourceAssetConfig(GpuAnimSourceAssetConfig src)
        {
            m_srcAssetPath = src.m_srcAssetPath;
            m_fps = src.m_fps;
            m_animTexName = src.m_animTexName;
        }
    }

    /// <summary>
    /// 动画贴图的配置文件
    /// </summary>
    [Serializable]
    public class GpuAnimTexConfig
    {
        public string m_animTexName;
        public List<GpuAnimSourceAssetConfig> m_allSrcAsset = new List<GpuAnimSourceAssetConfig>();
    }

    /// <summary>
    /// 内存对象
    /// </summary>
    class GpuAnimTexItem
    {
        public GpuAnimTexConfig m_config;
        public Dictionary<string, GpuAnimSourceAssetConfig> m_dirtys = new Dictionary<string, GpuAnimSourceAssetConfig>();

        public GpuAnimTexItem(GpuAnimTexConfig config)
        {
            m_config = config;
        }

        public GpuAnimTexItem(string animTexName)
        {
            m_config = new GpuAnimTexConfig();
            m_config.m_animTexName = animTexName;
        }

        public void RmvSrcAsset(string assetPath)
        {
            int index = FindAssetIndex(assetPath);
            if (index >= 0)
            {
                if (!m_dirtys.ContainsKey(assetPath)) m_dirtys.Add(assetPath, m_config.m_allSrcAsset[index]);
                m_config.m_allSrcAsset.RemoveAt(index);
            }
        }

        public void AddSrcAsset(GpuAnimSourceAssetConfig newAsset)
        {
            int index = FindAssetIndex(newAsset.m_srcAssetPath);
            if (index < 0)
            {
                if (!m_dirtys.ContainsKey(newAsset.m_srcAssetPath)) m_dirtys.Add(newAsset.m_srcAssetPath, newAsset);
                m_config.m_allSrcAsset.Add(new GpuAnimSourceAssetConfig(newAsset));
            }
        }

        public bool IsExist(GpuAnimSourceAssetConfig asset)
        {
            return FindAssetIndex(asset.m_srcAssetPath) >= 0;
        }

        public void UpdateAnimBakConfig(GpuAnimSourceAssetConfig asset)
        {
            var index = FindAssetIndex(asset.m_srcAssetPath);
            if (index >= 0)
            {
                m_config.m_allSrcAsset[index].m_fps = asset.m_fps;
            }
        }

        int FindAssetIndex(string assetPath)
        {
            for (int i = 0; i < m_config.m_allSrcAsset.Count; i++)
            {
                var existSrcAsset = m_config.m_allSrcAsset[i].m_srcAssetPath;
                if (existSrcAsset == assetPath)
                {
                    return i;
                }
            }

            return -1;
        }
    }

    /// <summary>
    /// 统一的管理项目中所有烘培的动画和贴图的管理工作
    /// </summary>
    public class GpuAnimBakeMgr : BSingleton<GpuAnimBakeMgr>
    {
        private Dictionary<string, GpuAnimTexItem> m_allAnimTex = new Dictionary<string, GpuAnimTexItem>();
        private Dictionary<string, GpuAnimTexItem> m_asset2AnimTex = new Dictionary<string, GpuAnimTexItem>();
        private System.Collections.IEnumerator currentTask = null;
        private Action m_onFinish;

        public GpuAnimBakeMgr()
        {
            Reload();
        }

        public void ExecuteBake(Action onFinish, GPUAnimParam param)
        {
            m_onFinish = onFinish;
            EditorCoroutineUtility.StartCoroutineOwnerless(ExecuteTask(param));
        }

        private System.Collections.IEnumerator ExecuteTask(GPUAnimParam param)
        {
            UnityEditor.EditorUtility.DisplayProgressBar("正在烘焙", "", 0);
            int count = 0;
            foreach (var pair in m_allAnimTex)
            {
                foreach (var kv in pair.Value.m_dirtys)
                {
                    count++;
                }
            }

            int bakedCount = 0;
            foreach (var pair in m_allAnimTex)
            {
                foreach (var kv in pair.Value.m_dirtys)
                {
                    var task = new GpuAnimBakeTask(kv.Value, param);
                    EditorUtility.DisplayProgressBar(string.Format("正在烘培: {0}", kv.Value.m_animTexName), task.CurrClipName, task.Progress);
                    try
                    {
                        do
                        {
                            yield return null;
                            task.Step();
                            bakedCount++;
                            UnityEditor.EditorUtility.DisplayProgressBar("正在烘焙", kv.Key, (float)bakedCount / (float)count);
                        }
                        while (!task.IsFinish());
                    }
                    finally
                    {
                        EditorUtility.ClearProgressBar();
                    }
                }
                pair.Value.m_dirtys.Clear();
            }
            UnityEditor.EditorUtility.ClearProgressBar();
            m_onFinish?.Invoke();
        }


        public void Reload()
        {
            m_allAnimTex.Clear();
            m_asset2AnimTex.Clear();

            var xmlDir = GetAnimTexConfigXmlDir();

            ///读取所有文件列表
            var listTexList = new List<string>();
            LoadFoldFileList(listTexList, xmlDir);

            foreach (var xmlPath in listTexList)
            {
                var texConfig = XmlSerializeUtil.DeSerializeFromXmlFile<GpuAnimTexConfig>(xmlPath);
                if (texConfig == null)
                {
                    BLogger.Error("load anim texture config failed: {0}", xmlPath);
                    continue;
                }

                m_allAnimTex.Add(texConfig.m_animTexName, new GpuAnimTexItem(texConfig));
            }

            BuildAnimAssetMap();
        }

        private void BuildAnimAssetMap()
        {
            m_asset2AnimTex.Clear();
            foreach (var kv in m_allAnimTex)
            {
                var animTexItem = kv.Value;
                foreach (var srcAsset in animTexItem.m_config.m_allSrcAsset)
                {
                    if (m_asset2AnimTex.ContainsKey(srcAsset.m_srcAssetPath))
                    {
                        BLogger.Error("Repeat include animation: {0}, new: {1}, exist:{2}",
                            srcAsset.m_srcAssetPath, animTexItem.m_config.m_animTexName,
                            m_asset2AnimTex[srcAsset.m_srcAssetPath].m_config.m_animTexName);
                    }
                    else
                    {
                        m_asset2AnimTex.Add(srcAsset.m_srcAssetPath, animTexItem);
                    }
                }
            }
        }

        /// <summary>
        /// 添加烘培任务
        /// </summary>
        /// <param name="bakConfig"></param>
        /// <returns></returns>
        public bool AddAnimBakTask(GpuAnimSourceAssetConfig bakConfig)
        {
            var animTexName = bakConfig.m_animTexName;
            if (string.IsNullOrEmpty(animTexName))
            {
                BLogger.Error("Invalid anim tex name");
                return false;
            }

            ///判断是否存在
            GpuAnimTexItem texItem;
            if (m_allAnimTex.TryGetValue(bakConfig.m_animTexName, out texItem))
            {
                ///如果加入的已经存在这个动画了，那么返回
                if (texItem.IsExist(bakConfig))
                {
                    texItem.UpdateAnimBakConfig(bakConfig);
                    if (!texItem.m_dirtys.ContainsKey(bakConfig.m_srcAssetPath)) texItem.m_dirtys.Add(bakConfig.m_srcAssetPath, bakConfig);
                    return true;
                }

                GpuAnimTexItem existOldTex;
                if (m_asset2AnimTex.TryGetValue(bakConfig.m_srcAssetPath, out existOldTex))
                {
                    existOldTex.RmvSrcAsset(bakConfig.m_srcAssetPath);
                }
            }
            else
            {
                texItem = new GpuAnimTexItem(bakConfig.m_animTexName);
                m_allAnimTex.Add(bakConfig.m_animTexName, texItem);
            }

            texItem.AddSrcAsset(bakConfig);
            m_asset2AnimTex[bakConfig.m_srcAssetPath] = texItem;

            SaveModifyAnimTexConfig();
            return true;
        }

        public bool AddRefreshAnimBakTask(GpuAnimSourceAssetConfig bakConfig)
        {
            var animTexName = bakConfig.m_animTexName;
            if (string.IsNullOrEmpty(animTexName))
            {
                BLogger.Error("Invalid anim tex name");
                return false;
            }

            ///判断是否存在
            GpuAnimTexItem texItem;
            if (m_allAnimTex.TryGetValue(bakConfig.m_animTexName, out texItem))
            {
                texItem.m_config.m_allSrcAsset.Clear();
                texItem.m_dirtys.Add(bakConfig.m_srcAssetPath, bakConfig);

                GpuAnimTexItem existOldTex;
                if (m_asset2AnimTex.TryGetValue(bakConfig.m_srcAssetPath, out existOldTex))
                {
                    existOldTex.RmvSrcAsset(bakConfig.m_srcAssetPath);
                }
            }
            else
            {
                texItem = new GpuAnimTexItem(bakConfig.m_animTexName);
                m_allAnimTex.Add(bakConfig.m_animTexName, texItem);
            }

            texItem.AddSrcAsset(bakConfig);
            m_asset2AnimTex[bakConfig.m_srcAssetPath] = texItem;

            SaveModifyAnimTexConfig();
            return true;
        }

        private string GetAnimTexConfigXmlDir()
        {
            string assetPath = Application.dataPath + "/../EditorData/GpuInstEditor/AnimTexXml/";
            return assetPath;
        }

        /// <summary>
        /// 搜集目录下的所有文件，包括子目录
        /// </summary>
        /// <param name="exportList"></param>
        /// <param name="folderPath"></param>
        private bool LoadFoldFileList(List<string> assetFileList, string folderPath)
        {
            string fullPath = folderPath;
            if (!Directory.Exists(fullPath))
            {
                BLogger.Error("folder not exist: {0}", fullPath);
                return false;
            }

            string[] subFile = Directory.GetFiles(fullPath);
            foreach (string fileName in subFile)
            {
                assetFileList.Add(fileName);
            }

            string[] subFolders = Directory.GetDirectories(fullPath);
            foreach (string subFolderName in subFolders)
            {
                LoadFoldFileList(assetFileList, subFolderName);
            }

            return true;
        }
        private void SaveModifyAnimTexConfig()
        {
            var xmlDir = GetAnimTexConfigXmlDir();
            foreach (var kv in m_allAnimTex)
            {
                var animTexItem = kv.Value;
                if (animTexItem.m_dirtys.Count == 0)
                {
                    continue;
                }

                var xmlPath = xmlDir + animTexItem.m_config.m_animTexName + ".xml";
                if (!XmlSerializeUtil.SerializeToXmlFile<GpuAnimTexConfig>(animTexItem.m_config, xmlPath))
                {
                    BLogger.Error("SaveAnimTexItem failed: {0}", xmlPath);
                }
            }
        }
    }
}
