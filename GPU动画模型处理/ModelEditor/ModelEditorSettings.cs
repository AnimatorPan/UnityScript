using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEditor;

[Serializable]
public class ModelSaveData
{
    public ModelType ModelTypeEnum;
    public string PrefabSaveDir;
}

[CreateAssetMenu(menuName = "PGameTools/CreateGlobalSettings/ModelEditorSettings", fileName = "Assets/Editor/ModelEditorSettings/ModelEditorSettings")]
public class ModelEditorSettings : ScriptableObject
{
    public static readonly string ModelEditorSettingsFilePath = "Assets/Editor/ModelEditorSettings/ModelEditorSettings.asset";
    private static ModelEditorSettings s_settings;
    public static ModelEditorSettings Instance
    {
        get
        {
            if (s_settings == null)
                s_settings = AssetDatabase.LoadAssetAtPath<ModelEditorSettings>(ModelEditorSettingsFilePath);

            return s_settings;
        }
    }
    
    [ListDrawerSettings]
    public List<ModelSaveData> m_modelSaveDataDict = new List<ModelSaveData>();

    public bool GetModelSaveDataByModelType(ModelType modelType, out ModelSaveData data)
    {
        foreach (var saveData in m_modelSaveDataDict)
        {
            if (saveData.ModelTypeEnum == modelType)
            {
                data = saveData;
                return true;
            }
        }

        data = null;
        return false;
    }
}