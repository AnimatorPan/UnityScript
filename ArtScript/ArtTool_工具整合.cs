using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;

public class MyToolsWindow : EditorWindow
{
    private List<MethodInfo> toolMethods = new List<MethodInfo>();

    [MenuItem("Window/My Tools")]
    public static void ShowWindow()
    {
        GetWindow<MyToolsWindow>("My Tools");
    }

    private void OnGUI()
    {
        GUILayout.Label("工具集合", EditorStyles.boldLabel);

        if (GUILayout.Button("工具 1"))
        {
            Tool1();
        }

        if (GUILayout.Button("工具 2"))
        {
            Tool2();
        }

        if (GUILayout.Button("工具 3"))
        {
            Tool3();
        }

        if (GUILayout.Button("提取FBX动画切片"))
        {
            ExtractFBXAnimations.ExtractAnimations();
        }

        foreach (var method in toolMethods)
        {
            if (GUILayout.Button(method.Name))
            {
                method.Invoke(null, null);
            }
        }

        GUILayout.Space(20);
        GUILayout.Label("拖动脚本到此处以添加工具", EditorStyles.boldLabel);
        HandleDragAndDrop();
    }

    private void HandleDragAndDrop()
    {
        Event evt = Event.current;
        Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "拖动脚本到此处");

        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!dropArea.Contains(evt.mousePosition))
                    return;

                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();

                    foreach (var draggedObject in DragAndDrop.objectReferences)
                    {
                        MonoScript script = draggedObject as MonoScript;
                        if (script != null)
                        {
                            Type scriptType = script.GetClass();
                            if (scriptType != null)
                            {
                                MethodInfo[] methods = scriptType.GetMethods(BindingFlags.Public | BindingFlags.Static);
                                foreach (var method in methods)
                                {
                                    toolMethods.Add(method);
                                }
                            }
                        }
                    }
                }
                Event.current.Use();
                break;
        }
    }

    private void Tool1()
    {
        // 工具 1 的代码
        Debug.Log("工具 1 被调用");
    }

    private void Tool2()
    {
        // 工具 2 的代码
        Debug.Log("工具 2 被调用");
    }

    private void Tool3()
    {
        // 工具 3 的代码
        Debug.Log("工具 3 被调用");
    }
}