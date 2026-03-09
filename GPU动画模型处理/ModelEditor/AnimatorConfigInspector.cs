using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;
using AnimatorControllerParameterType = UnityEngine.AnimatorControllerParameterType;

namespace A9Game
{
    [CustomEditor(typeof(AnimatorConfig))]
    class AnimatorConfigInspector : Editor
    {
        private SerializedObject m_object;
        public void OnEnable()
        {
            m_object = new SerializedObject(target);
        }

        void OnDisable()
        {
            if (m_object != null)
            {
                m_object.Dispose();
            }

            m_object = null;
        }

        public override void OnInspectorGUI()
        {
            var config = target as AnimatorConfig;
            var itr = m_object.GetIterator();
            if (!itr.NextVisible(true))
            {
                return;
            }

            while (true)
            {
                EditorGUILayout.PropertyField(itr, true);
                if (!itr.NextVisible(false))
                {
                    break;
                }
            }

            m_object.ApplyModifiedProperties();

            if (GUILayout.Button("Collect"))
            {
                if (EditorCollect(config))
                {
                    m_object = new SerializedObject(config);
                    Debug.Log("animator config changed : " + config.name);
                }
            }
        }

        public static bool EditorCollect(AnimatorConfig config)
        {
            var oldAnimator = config.m_allAnimator;
            var oldBoolEvent = config.m_listBoolEvent;
            var oldFloatEvent = config.m_listFloatEvent;
            var oldIntEvent = config.m_listIntEvent;

            config.m_allAnimator = config.gameObject.GetComponentsInChildren<Animator>();

            config.m_listBoolEvent = null;
            config.m_listFloatEvent = null;
            config.m_listIntEvent = null;

            var nameMerge = new HashSet<string>();
            var listBoolEvent = new List<DodAnimatorBoolEventData>();
            var listFloatEvent = new List<DodAnimatorFloatEventData>();
            var listIntEvent = new List<DodAnimatorIntEventData>();

            if (config.m_allAnimator != null)
            {
                for (int i = 0; i < config.m_allAnimator.Length; i++)
                {
                    var animator = config.m_allAnimator[i];
                    
                    UnityEditor.Animations.AnimatorController animatorController = animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController; 
                    //UnityEditor.Animations.AnimatorController.GetEffectiveAnimatorController(animator);
                    if (animatorController == null)
                    {
                        continue;
                    }

                    var allParams = animatorController.parameters;
                    for (int k = 0; k < allParams.Length; k++)
                    {
                        var param = allParams[k];
                        if (nameMerge.Contains(param.name))
                        {
                            continue;
                        }

                        nameMerge.Add(param.name);
                        if (param.type == AnimatorControllerParameterType.Bool)
                        {
                            listBoolEvent.Add(new DodAnimatorBoolEventData(param.name, param.defaultBool));
                        }
                        else if (param.type == AnimatorControllerParameterType.Float)
                        {
                            listFloatEvent.Add(new DodAnimatorFloatEventData(param.name, param.defaultFloat));
                        }
                        else if (param.type == AnimatorControllerParameterType.Int)
                        {
                            listIntEvent.Add(new DodAnimatorIntEventData(param.name, param.defaultInt));
                        }
                    }
                }
            }

            if (listBoolEvent.Count > 0)
            {
                config.m_listBoolEvent = listBoolEvent.ToArray();
            }
            if (listFloatEvent.Count > 0)
            {
                config.m_listFloatEvent = listFloatEvent.ToArray();
            }
            if (listIntEvent.Count > 0)
            {
                config.m_listIntEvent = listIntEvent.ToArray();
            }

            ///检查是否修改了
            return IsChanged(oldAnimator, config.m_allAnimator) ||
                   IsChangedData(oldBoolEvent, config.m_listBoolEvent) ||
                   IsChangedData(oldFloatEvent, config.m_listFloatEvent) ||
                   IsChangedData(oldIntEvent, config.m_listIntEvent);
        }

        static bool IsChanged<T>(T[] a, T[] b) where T : class 
        {
            int aLen = a == null ? 0 : a.Length;
            int bLen = b == null ? 0 : b.Length;
            if (aLen != bLen)
            {
                return true;
            }

            if (aLen <= 0)
            {
                return false;
            }

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return true;
                }
            }

            return false;
        }

        static bool IsChangedData<T>(T[] a, T[] b) where T : class 
        {
            int aLen = a == null ? 0 : a.Length;
            int bLen = b == null ? 0 : b.Length;
            if (aLen != bLen)
            {
                return true;
            }

            if (aLen <= 0)
            {
                return false;
            }

            for (int i = 0; i < a.Length; i++)
            {
                if (!a[i].Equals(b[i]))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
