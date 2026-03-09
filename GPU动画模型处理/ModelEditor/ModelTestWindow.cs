using System.Collections.Generic;
using DodGame;
using A9Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Random = UnityEngine.Random;

public class ModelTestWindow : EditorWindow
{
    private string m_curSceneName;

    public int m_assetCount = 10; //资源个数
    public Vector3 m_centerPos = new Vector3(0, 1, 3);
    public float m_radius = 10;
    public Vector3 m_eulerAngle = new Vector3(0, 180, 0);

    private static List<GameObject> m_listData = new List<GameObject>();

    [MenuItem("Window/模型展示")]
    static void OpenEditorWindow()
    {
        var window = GetWindow(typeof(ModelTestWindow), false, "模型展示");
        window.minSize = new Vector2(455, 300);
    }

    public void OnGUI()
    {
        //var runner = GetRunner();

        EditorGUILayout.BeginVertical();

        //m_testTime = EditorGUILayout.FloatField("展示时长", m_testTime);
        m_assetCount = EditorGUILayout.IntField("展示个数", m_assetCount);

        string activeSceneName = "DefaultSceneName";
        var activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene != null)
        {
            activeSceneName = activeScene.name;
        }
        if (activeSceneName != m_curSceneName)
        {
            m_curSceneName = activeSceneName;
            m_centerPos.x = PlayerPrefs.GetFloat(m_curSceneName + "_EffectProfilerWindow.m_centerPos.x", m_centerPos.x);
            m_centerPos.y = PlayerPrefs.GetFloat(m_curSceneName + "_EffectProfilerWindow.m_centerPos.y", m_centerPos.y);
            m_centerPos.z = PlayerPrefs.GetFloat(m_curSceneName + "_EffectProfilerWindow.m_centerPos.z", m_centerPos.z);
        }
        var centerPos = EditorGUILayout.Vector3Field("中心位置", m_centerPos);
        if (centerPos != m_centerPos)
        {
            m_centerPos = centerPos;
            PlayerPrefs.SetFloat(m_curSceneName + "_EffectProfilerWindow.m_centerPos.x", m_centerPos.x);
            PlayerPrefs.SetFloat(m_curSceneName + "_EffectProfilerWindow.m_centerPos.y", m_centerPos.y);
            PlayerPrefs.SetFloat(m_curSceneName + "_EffectProfilerWindow.m_centerPos.z", m_centerPos.z);
        }

        m_radius = EditorGUILayout.FloatField("半径", m_radius);
        m_eulerAngle = EditorGUILayout.Vector3Field("旋转", m_eulerAngle);

        EditorGUILayout.EndVertical();

        if (!EditorApplication.isPlaying)
        {
            if (GUILayout.Button("初始化"))
            {
                var baker = Transform.FindObjectOfType<OcclusionBaker>();
                if (baker != null)
                {
                    baker.enabled = false;
                }

                EditorApplication.isPlaying = true;
            }
        }
        else
        {
            if (GUILayout.Button("展示"))
            {
                for (int i = 0; i < m_listData.Count; i++)
                {
                    DestroyImmediate(m_listData[i]);
                }
                m_listData.Clear();

                var prefab = Selection.activeGameObject;
                if (prefab != null)
                {
                    for (int i = 0; i < m_assetCount; i++)
                    {
                        var go = GameObject.Instantiate(prefab);
                        if (go != null)
                        {
                            var gpuAnimRenderer = go.GetComponent<GpuAnimRenderer>();
                            if (gpuAnimRenderer != null)
                            {
                                gpuAnimRenderer.CreateRender(0);
                                gpuAnimRenderer.BindAnimatorCtrl(BakeAnimConfigFactory.GetDefaultConfig());
                                gpuAnimRenderer.Play(AnimatorStateName.Idel);
                            }

                            var offset = Random.insideUnitCircle;
                            var startPos = m_centerPos + new Vector3(offset.x, 0, offset.y) * m_radius;

                            go.transform.position = startPos;
                            go.transform.localRotation = Quaternion.Euler(m_eulerAngle);
                            go.transform.localScale = Vector3.one;

                            m_listData.Add(go);
                        }
                    }
                }
            }
        }


    }

    //private List<GameObject> GetBenchEffectList()
    //{
    //    var result = new List<GameObject>();
    //    var assetPaths = DodEditorTools.GetSelectedAssetPaths("*.prefab");
    //    foreach (var assetPath in assetPaths)
    //    {
    //        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
    //        if (prefab != null)
    //        {
    //            result.Add(prefab);
    //        }
    //    }
    //    return result.Count > 0 ? result : null;
    //}
}
