using A9Game;
using UnityEngine;
using UnityEditor;


[CustomEditor(typeof (TpsConfig))]
internal class TpsConfigInspector : Editor
{
    private SerializedObject m_object = null;

    public void OnEnable()
    {
        m_object = new SerializedObject(target);
    }

    public override void OnInspectorGUI()
    {
        var configData = target as TpsConfig;
        if (configData == null)
        {
            return;
        }

        EditorGUILayout.BeginVertical();
        EditorGUILayout.Space();

        var cacheNodeRef = m_object.FindProperty("m_allCachePos");
        cacheNodeRef.arraySize = EditorGUILayout.IntField("PosCount", cacheNodeRef.arraySize);

        if (EditorGUILayout.PropertyField(cacheNodeRef))
        {
            for (int i = 0; i < cacheNodeRef.arraySize; i++)
            {
                var nodeRef = cacheNodeRef.GetArrayElementAtIndex(i);
                if (nodeRef != null)
                {
                    if (EditorGUILayout.PropertyField(nodeRef))
                    {
                        var end = nodeRef.GetEndProperty();
                        while (nodeRef.NextVisible(true) && !SerializedProperty.EqualContents(nodeRef, end))
                        {
                            EditorGUILayout.PropertyField(nodeRef);
                        }

                        nodeRef.Reset();
                    }
                }
            }
        }
        
        EditorGUILayout.Space();

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        m_object.ApplyModifiedProperties();
        if (GUILayout.Button("Collect all node"))
        {
            configData.CollectCachePos();
            m_object = new SerializedObject(configData);
        }
        
        if (GUILayout.Button("expand all node"))
        {
            cacheNodeRef = m_object.FindProperty("m_allCachePos");
            var end = cacheNodeRef.GetEndProperty(true);
            cacheNodeRef.isExpanded = true;

            while (cacheNodeRef.Next(true) && cacheNodeRef != end)
            {
                if (cacheNodeRef.hasChildren)
                {
                    cacheNodeRef.isExpanded = true;
                }
            }
        }

        if (GUILayout.Button("collapse all node"))
        {
            cacheNodeRef = m_object.FindProperty("m_allCachePos");
            var end = cacheNodeRef.GetEndProperty(true);
            cacheNodeRef.isExpanded = false;

            while (cacheNodeRef.Next(true) && cacheNodeRef != end)
            {
                if (cacheNodeRef.hasChildren)
                {
                    cacheNodeRef.isExpanded = false;
                }
            }
        }

        if (Application.isPlaying)
        {
            if (GUILayout.Button("test recover"))
            {
            }
        }
    }

}