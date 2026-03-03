 using UnityEngine;
using UnityEditor;
using System.IO;
using System;

public class RenameTool : EditorWindow
{
    private string prefix = "";
    private string suffix = "";
    private string replaceFrom = "";
    private string replaceTo = "";
    private bool useNumbering = false;
    private int startNumber = 1;
    private int numberPadding = 3;
    private string[] selectedFilePaths;
    private string[] selectedFileNames;
    private string[] selectedFileExtensions;

    /// <summary>
    /// 显示批量重命名对话框
    /// </summary>
    [MenuItem("Assets/批量重命名", false, 100)]
    private static void ShowRenameWindow()
    {
        // 显示重命名窗口
        RenameTool window = GetWindow<RenameTool>(true, "批量重命名");
        window.minSize = new Vector2(400, 330);
        window.maxSize = new Vector2(400, 330);

        // 初始化窗口数据
        window.UpdateSelectedFiles();

        window.Show();
    }

    /// <summary>
    /// 更新选中的文件列表
    /// </summary>
    private void UpdateSelectedFiles()
    {
        // 获取选中的资产路径
        string[] selectedAssets = Selection.assetGUIDs;
        
        // 收集所有选中的有效文件路径
        int validFileCount = 0;
        for (int i = 0; i < selectedAssets.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(selectedAssets[i]);
            if (!string.IsNullOrEmpty(assetPath) && !Directory.Exists(assetPath))
            {
                validFileCount++;
            }
        }

        if (validFileCount > 0)
        {
            selectedFilePaths = new string[validFileCount];
            selectedFileNames = new string[validFileCount];
            selectedFileExtensions = new string[validFileCount];
            
            int index = 0;
            for (int i = 0; i < selectedAssets.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(selectedAssets[i]);
                if (!string.IsNullOrEmpty(assetPath) && !Directory.Exists(assetPath))
                {
                    selectedFilePaths[index] = assetPath;
                    selectedFileNames[index] = Path.GetFileNameWithoutExtension(assetPath);
                    selectedFileExtensions[index] = Path.GetExtension(assetPath);
                    index++;
                }
            }
        }
        else
        {
            // 如果没有选中有效文件，初始化空数组
            selectedFilePaths = new string[0];
            selectedFileNames = new string[0];
            selectedFileExtensions = new string[0];
        }
        
        // 刷新窗口
        Repaint();
    }

    /// <summary>
    /// 当窗口获得焦点时自动更新选中的文件列表
    /// </summary>
    private void OnFocus()
    {
        UpdateSelectedFiles();
    }

    /// <summary>
    /// 绘制批量重命名窗口
    /// </summary>
    private void OnGUI()
    {
        GUILayout.Space(10);

        GUILayout.Label("批量重命名设置", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // 显示选中文件数量
        GUILayout.Label(string.Format("已选择 {0} 个文件", selectedFilePaths.Length));
        
        // 重新选择文件按钮
        if (GUILayout.Button("重新选择文件", GUILayout.Height(25)))
        {
            ReselectFiles();
        }
        GUILayout.Space(10);

        // 前缀设置
        GUILayout.BeginHorizontal();
        GUILayout.Label("前缀:", GUILayout.Width(80));
        prefix = GUILayout.TextField(prefix);
        GUILayout.EndHorizontal();

        // 后缀设置
        GUILayout.BeginHorizontal();
        GUILayout.Label("后缀:", GUILayout.Width(80));
        suffix = GUILayout.TextField(suffix);
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        GUILayout.Label("替换功能", EditorStyles.boldLabel);
        GUILayout.Space(5);

        // 替换设置
        GUILayout.BeginHorizontal();
        GUILayout.Label("查找:", GUILayout.Width(80));
        replaceFrom = GUILayout.TextField(replaceFrom);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("替换为:", GUILayout.Width(80));
        replaceTo = GUILayout.TextField(replaceTo);
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        GUILayout.Label("编号功能", EditorStyles.boldLabel);
        GUILayout.Space(5);

        // 编号设置
        GUILayout.BeginHorizontal();
        useNumbering = GUILayout.Toggle(useNumbering, "使用编号");
        GUILayout.EndHorizontal();

        if (useNumbering)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("起始编号:", GUILayout.Width(100));
            startNumber = EditorGUILayout.IntField(startNumber);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("编号位数:", GUILayout.Width(100));
            numberPadding = EditorGUILayout.IntField(numberPadding);
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(20);

        // 操作按钮
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("预览", GUILayout.Height(30)))
        {
            PreviewRename();
        }

        if (GUILayout.Button("执行重命名", GUILayout.Height(30)))
        {
            BatchRenameFiles();
        }

        if (GUILayout.Button("取消", GUILayout.Height(30)))
        {
            Close();
        }
        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// 预览重命名结果
    /// </summary>
    private void PreviewRename()
    {
        string previewText = "重命名预览:\n\n";
        
        for (int i = 0; i < selectedFilePaths.Length; i++)
        {
            string oldName = selectedFileNames[i];
            string newName = GenerateNewFileName(oldName, i);
            previewText += string.Format("{0} → {1}{2}\n", oldName, newName, selectedFileExtensions[i]);
        }
        
        EditorUtility.DisplayDialog("重命名预览", previewText, "确定");
    }

    /// <summary>
    /// 执行批量重命名
    /// </summary>
    private void BatchRenameFiles()
    {
        // 验证设置
        if (string.IsNullOrEmpty(prefix) && string.IsNullOrEmpty(suffix) && string.IsNullOrEmpty(replaceFrom) && !useNumbering)
        {
            EditorUtility.DisplayDialog("提示", "请至少设置一项重命名规则！", "确定");
            return;
        }

        int successCount = 0;
        int failureCount = 0;

        // 执行批量重命名
        try
        {
            // 先检查所有新文件名是否会导致冲突
            string[] newFileNames = new string[selectedFilePaths.Length];
            bool hasConflict = false;
            
            for (int i = 0; i < selectedFilePaths.Length; i++)
            {
                newFileNames[i] = GenerateNewFileName(selectedFileNames[i], i);
                
                // 检查非法字符
                string invalidChars = new string(Path.GetInvalidFileNameChars());
                if (newFileNames[i].IndexOfAny(invalidChars.ToCharArray()) >= 0)
                {
                    EditorUtility.DisplayDialog("错误", string.Format("文件名包含非法字符: {0}", newFileNames[i]), "确定");
                    hasConflict = true;
                    break;
                }
            }
            
            if (hasConflict)
            {
                return;
            }
            
            // 检查文件名是否重复或已存在
            for (int i = 0; i < selectedFilePaths.Length; i++)
            {
                string directoryPath = Path.GetDirectoryName(selectedFilePaths[i]);
                string newFilePath = Path.Combine(directoryPath, newFileNames[i] + selectedFileExtensions[i]);
                
                // 检查是否与其他选中文件重名
                for (int j = i + 1; j < selectedFilePaths.Length; j++)
                {
                    string otherDirectoryPath = Path.GetDirectoryName(selectedFilePaths[j]);
                    string otherNewFilePath = Path.Combine(otherDirectoryPath, newFileNames[j] + selectedFileExtensions[j]);
                    
                    if (newFilePath.Equals(otherNewFilePath, StringComparison.OrdinalIgnoreCase))
                    {
                        EditorUtility.DisplayDialog("错误", string.Format("生成的文件名重复: {0}", newFileNames[i] + selectedFileExtensions[i]), "确定");
                        hasConflict = true;
                        break;
                    }
                }
                
                if (hasConflict)
                {
                    break;
                }
                
                // 检查文件是否已存在
                string fullNewFilePath = Path.Combine(Application.dataPath.Replace("Assets", ""), newFilePath);
                if (File.Exists(fullNewFilePath))
                {
                    EditorUtility.DisplayDialog("错误", string.Format("文件名已存在: {0}", newFileNames[i] + selectedFileExtensions[i]), "确定");
                    hasConflict = true;
                    break;
                }
            }
            
            if (hasConflict)
            {
                return;
            }
            
            // 执行重命名操作
            for (int i = 0; i < selectedFilePaths.Length; i++)
            {
                string result = AssetDatabase.RenameAsset(selectedFilePaths[i], newFileNames[i]);
                if (string.IsNullOrEmpty(result))
                {
                    successCount++;
                }
                else
                {
                    failureCount++;
                }
            }

            // 刷新资产数据库
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 显示结果
            string resultMessage = string.Format("批量重命名完成！\n成功: {0} 个文件\n失败: {1} 个文件", successCount, failureCount);
            EditorUtility.DisplayDialog("批量重命名结果", resultMessage, "确定");
            // 重命名后保持窗口打开，移除Close()调用
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("错误", "重命名失败: " + e.Message, "确定");
        }
    }

    /// <summary>
    /// 生成新文件名
    /// </summary>
    private string GenerateNewFileName(string oldName, int index)
    {
        string newName = oldName;
        
        // 替换功能
        if (!string.IsNullOrEmpty(replaceFrom))
        {
            newName = newName.Replace(replaceFrom, replaceTo);
        }
        
        // 前缀和后缀
        newName = prefix + newName + suffix;
        
        // 编号功能
        if (useNumbering)
        {
            string number = (startNumber + index).ToString().PadLeft(numberPadding, '0');
            newName = newName + number;
        }
        
        return newName;
    }

    /// <summary>
    /// 重新选择文件
    /// </summary>
    private void ReselectFiles()
    {
        // 提示用户在Project窗口中重新选择文件
        EditorUtility.DisplayDialog("提示", "请在Project窗口中选择要重命名的文件，然后点击确定按钮继续。", "确定");
        
        // 更新选中的文件列表
        UpdateSelectedFiles();
        
        // 验证选择结果
        if (selectedFilePaths.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "未选择任何有效文件，请重新选择！", "确定");
        }
        else
        {
            EditorUtility.DisplayDialog("提示", string.Format("已成功选择 {0} 个文件！", selectedFilePaths.Length), "确定");
        }
    }

    /// <summary>
    /// 修改所有文件（原有功能保留）
    /// </summary>
    /// <param name="folderPath">项目路径</param>
    /// <param name="_path2">目标父文件夹</param>
    /// <param name="_id">修改的后缀</param>
    /// <param name="_isFile">是否单个文件夹</param>
    private static void ListDirectory(string folderPath, string _path2, string _id, bool _isFile)
    {
        var _path11 = folderPath + @"\" + _path2;
        if (_isFile)
        {
            foreach (string directory in System.IO.Directory.GetDirectories(_path11, "*", SearchOption.AllDirectories))
            {
                Debug.LogError(directory);
                foreach (string file in System.IO.Directory.GetFiles(directory))
                {
                    FileInfo f = new FileInfo(file);
                    if (f.Extension != ".meta")
                    {
                        Debug.Log(f.Name);
                        var _name = f.Name.Replace(f.Extension, "") + _id;
                        var _pat2 = f.FullName.Replace(folderPath + @"\", "");
                        Debug.Log(AssetDatabase.RenameAsset(_pat2, _name));
                    }
                }
            }
        }
        else
        {
            foreach (string file in System.IO.Directory.GetFiles(_path11))
            {
                FileInfo f = new FileInfo(file);
                if (f.Extension != ".meta")
                {
                    Debug.Log(f.Name);
                    var _name = f.Name.Replace(f.Extension, "") + _id;
                    var _pat2 = f.FullName.Replace(folderPath + @"\", "");
                    Debug.Log(_pat2);
                    Debug.Log(_name);
                    Debug.Log(AssetDatabase.RenameAsset(_pat2, _name));
                }
            }
        }

        // 刷新资产数据库
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}