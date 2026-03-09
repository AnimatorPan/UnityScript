using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DodGame
{
    [Serializable]
    public class ModelEditorConfig
    {
        public string prefabPath;
        public string animDir;
        public string saveDir;
    }
    
    [Serializable]
    public class ActorAnimationConfig
    {
        //private Dictionary<string, string> m_dictActorAnimInfos = new Dictionary<string, string>();

        private Dictionary<string, ModelEditorConfig> m_dictModelEditorConfigs =
            new Dictionary<string, ModelEditorConfig>(); 
        private const string configFileName = "../EditorData/GpuInstEditor/ActorAnimationDir.cfg";
        private static string m_filePath;
        private static string m_modelConfigPath;

        private static ActorAnimationConfig m_instance;  
        public static ActorAnimationConfig Instance
        {
            get
            {
                if (m_instance == null)
                {

                    m_instance = new ActorAnimationConfig();
                    m_filePath = Path.Combine(UnityEngine.Application.dataPath, configFileName);
                    if (File.Exists(m_filePath))
                    {
                        var lines = File.ReadAllLines(m_filePath);
                        foreach (var line  in  lines)
                        {
                            var arr = line.Split(':');
                            if (arr.Length == 2)
                            {
                                //m_instance.m_dictActorAnimInfos.Add(arr[0], arr[1]);
                                m_instance.m_dictModelEditorConfigs.Add(arr[0], new ModelEditorConfig()
                                {
                                    prefabPath = arr[0],
                                    animDir =  arr[1]
                                });
                            }
                            else if (arr.Length == 3)
                            {
                                //m_instance.m_dictActorAnimInfos.Add(arr[0], arr[1]);
                                m_instance.m_dictModelEditorConfigs.Add(arr[0], new ModelEditorConfig()
                                {
                                    prefabPath = arr[0],
                                    animDir =  arr[1],
                                    saveDir =  arr[2],
                                });
                            }
                        }
                    }
                    else
                    {
                        m_instance = new ActorAnimationConfig();
                    }
                }

                return m_instance;
            }
        }

        public bool TryGetDir(string fbxName, out ModelEditorConfig config)
        {
            return m_dictModelEditorConfigs.TryGetValue(fbxName, out config);
        }

        public void Set(string fbxName, string dirName, string saveDir)
        {
            bool isDirty = false;
            if (!m_dictModelEditorConfigs.TryGetValue(fbxName, out var config))
            {
                config = new ModelEditorConfig();
                if (config.animDir != dirName)
                {
                    config.animDir = dirName;
                    isDirty = true;
                }

                if (config.saveDir != saveDir)
                {
                    config.saveDir = saveDir;
                    isDirty = true;
                }
            }

            if (isDirty)
            {
                SaveConfig();;
            }
        }

        void SaveConfig()
        {
            var sb = new StringBuilder();
            foreach (var kv in m_dictModelEditorConfigs)
            {
                sb.AppendFormat("{0}:{1}:{2}", kv.Key, kv.Value.animDir, kv.Value.saveDir).AppendLine();
            }
            File.WriteAllText(m_filePath, sb.ToString());
        }
    }
}