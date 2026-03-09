using UnityEngine;
using UnityEditor;


[CustomEditor(typeof (RagdollController))]
internal class RagdollControllerInspector : Editor
{
    private SerializedObject m_object = null;

    public void OnEnable()
    {
        m_object = new SerializedObject(target);
    }

    public override void OnInspectorGUI()
    {
        var configData = target as RagdollController;
        if (configData == null)
        {
            return;
        }

        EditorGUILayout.BeginVertical();

        
        EditorGUILayout.PropertyField(m_object.FindProperty("m_applyEveryNode"));
        if (((RagdollController)m_object.targetObject).m_applyEveryNode)
        {
            EditorGUILayout.PropertyField(m_object.FindProperty("m_everyNodeForceRate"));
        }
        else
        {
            EditorGUILayout.PropertyField(m_object.FindProperty("m_ragdollRoot"));
            EditorGUILayout.PropertyField(m_object.FindProperty("m_ragdollForcePoint"));
            EditorGUILayout.PropertyField(m_object.FindProperty("m_applyRoot"));
        }


        EditorGUILayout.Space();

        var ragdollNodeRef = m_object.FindProperty("m_ragdollNode");
        ragdollNodeRef.arraySize = EditorGUILayout.IntField("NodeCount", ragdollNodeRef.arraySize);

        if (EditorGUILayout.PropertyField(ragdollNodeRef))
        {
            for (int i = 0; i < ragdollNodeRef.arraySize; i++)
            {
                var nodeRef = ragdollNodeRef.GetArrayElementAtIndex(i);
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
        if (GUILayout.Button("Collect all mesh"))
        {
            configData.CollectRagdollNode();
            configData.ForceSetRagdollEnable(false, true);
            m_object = new SerializedObject(configData);
        }
        
        if (GUILayout.Button("expand all mesh"))
        {
            ragdollNodeRef = m_object.FindProperty("m_ragdollNode");
            var end = ragdollNodeRef.GetEndProperty(true);
            ragdollNodeRef.isExpanded = true;

            while (ragdollNodeRef.Next(true) && ragdollNodeRef != end)
            {
                if (ragdollNodeRef.hasChildren)
                {
                    ragdollNodeRef.isExpanded = true;
                }
            }
        }

        if (GUILayout.Button("collapse all mesh"))
        {
            ragdollNodeRef = m_object.FindProperty("m_ragdollNode");
            var end = ragdollNodeRef.GetEndProperty(true);
            ragdollNodeRef.isExpanded = false;

            while (ragdollNodeRef.Next(true) && ragdollNodeRef != end)
            {
                if (ragdollNodeRef.hasChildren)
                {
                    ragdollNodeRef.isExpanded = false;
                }
            }
        }

        if (Application.isPlaying)
        {
            if (GUILayout.Button("test ragdoll"))
            {
                configData.ForceSetRagdollEnable(true);
            }

#if JEASON_TEST
            if (GUILayout.Button("test ragdoll with force"))
            {
                var force = configData.m_ragdollForcePoint != null ? configData.m_ragdollForcePoint.mass : 10.0f;
                configData.ForceSetRagdollEnable(true, true);
                configData.OnAddRagdollForce(new Vector3(
                    Random.Range(-50, 50) * force, 
                    Random.Range(-50, 50) * force, 
                    Random.Range(-50, 50) * force));
            }
#endif
        }
    }

}