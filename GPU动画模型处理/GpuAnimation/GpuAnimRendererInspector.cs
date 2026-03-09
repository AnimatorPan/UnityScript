using System;
using System.IO;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace DodGame
{
    [CustomEditor(typeof(GpuAnimRenderer))]
    public class GpuAnimRendererInspector : OdinEditor
    {
        private GpuAnimRenderer m_target;

        private void OnEnable()
        {
            m_target = target as GpuAnimRenderer;
            if (m_target == null)
                return;
            
        }


        bool DrawSelectAnimBtn(string title, ref GpuAnimData data)
        {
            if (GUILayout.Button(title))
            {
                var file = EditorUtility.OpenFilePanel("选择目标文件", "Assets/Resources/Actor/Player/GPU", "prefab");
                file.Replace('\\', '/');
                var dataPath = Application.dataPath.Replace('\\', '/');
                file = "Assets" + file.Remove(0, dataPath.Length);
                if (!string.IsNullOrEmpty(file))
                {
                    var go = AssetDatabase.LoadAssetAtPath<GameObject>(file);
                    if (go)
                    {
                        var gpuAnimRenderer = go.GetComponent<GpuAnimRenderer>();
                        if (gpuAnimRenderer)
                        {
                            data = new GpuAnimData()
                            {
                                m_boneInfoTexName = gpuAnimRenderer.m_boneInfoTexName,
                                m_animTexName = gpuAnimRenderer.m_animTexName
                            };
                            
                            EditorUtility.SetDirty(m_target);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            DrawSelectAnimBtn("指定无坐骑动画数据", ref m_target.m_normalAnimEntity);
            if (string.IsNullOrEmpty(m_target.m_animTexName))
            {
                m_target.m_animTexName = m_target.m_normalAnimEntity.m_animTexName;
                m_target.m_boneInfoTexName = m_target.m_normalAnimEntity.m_boneInfoTexName;
            }
            DrawSelectAnimBtn("指定坐姿动画数据", ref m_target.m_sitAnimEntity);
            DrawSelectAnimBtn("指定站姿动画数据", ref m_target.m_standAnimEntity);
            if (GUILayout.Button("修正mesh引用"))
            {
                if(m_target.m_listRender.Count == 0)
                    return;
                var meshFilter = m_target.m_listRender[0].GetComponent<MeshFilter>();
                ModelEditor.CreateMesh(m_target, meshFilter, 0);
            }
        }
    }
}