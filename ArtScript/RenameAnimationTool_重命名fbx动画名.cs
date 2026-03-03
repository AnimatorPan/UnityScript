using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Collections.Generic;
using System;
using System.Linq;



public class RenameAnimationTool : EditorWindow
{
    // 选中的FBX文件路径
    private string[] selectedFbxPaths = new string[0];
    // 动画片段信息
    private Dictionary<string, List<AnimationClip>> fbxAnimationClips = new Dictionary<string, List<AnimationClip>>();
    
    // 命名规则设置
    private string prefix = "";
    private string suffix = "";
    private string replaceFrom = "";
    private string replaceTo = "";
    private bool useRegex = false;
    private string regexPattern = "";
    private string regexReplacement = "";
    
    // 设置选项
    private bool showPreview = true;
    
    // 滚动位置
    private Vector2 scrollPosition;
    
    // 刷新状态
    private bool isRefreshing = false;
    private float refreshProgress = 0f;
    private string refreshStatus = "就绪";
    
    // 用于记录是否正在刷新，避免重复刷新
    private bool isRefreshingSelection = false;
    // 用于记录上次选择的对象，用于比较选择变化
    private UnityEngine.Object[] lastSelection = new UnityEngine.Object[0];
    
    // 修改预览信息
    private Dictionary<string, Dictionary<string, string>> previewChanges = new Dictionary<string, Dictionary<string, string>>();
    
    /// <summary>
    /// 显示批量修改动画片段名称窗口
    /// </summary>
    [MenuItem("Assets/批量修改动画片段名称", false, 101)]
    private static void ShowRenameAnimationWindow()
    {
        // 显示重命名窗口
        RenameAnimationTool window = GetWindow<RenameAnimationTool>(true, "批量修改动画片段名称");
        window.minSize = new Vector2(600, 500);
        window.maxSize = new Vector2(800, 700);
        
        // 初始化窗口数据
        window.CollectAndRefresh();
        window.Show();
    }
    
    /// <summary>
    /// 收集并刷新选中的FBX文件
    /// </summary>
    private void CollectAndRefresh()
    {
        // 收集当前选择的FBX文件
        string[] selectedAssets = Selection.assetGUIDs;
        List<string> fbxPaths = new List<string>();
        
        // 直接获取当前选择的文件，确保获取最新选择
        foreach (var obj in Selection.objects)
        {
            if (obj != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(assetPath) && Path.GetExtension(assetPath).ToLower() == ".fbx")
                {
                    fbxPaths.Add(assetPath);
                }
            }
        }
        
        // 更新选择的文件路径
        selectedFbxPaths = fbxPaths.ToArray();
        
        // 刷新动画片段
        RefreshAnimationClips();
        
        // 立即刷新UI
        Repaint();
    }
    
    /// <summary>
    /// 更新选中的FBX文件
    /// </summary>
    private void UpdateSelectedFiles()
    {
        CollectAndRefresh();
    }
    
    /// <summary>
    /// 当窗口启用时注册事件监听
    /// </summary>
    private void OnEnable()
    {
        // 使用EditorApplication.update来定期检查选择变化，参考用户提供的实现方式
        EditorApplication.update += OnEditorUpdate;
        // 记录初始选择
        lastSelection = Selection.objects;
    }
    
    /// <summary>
    /// 当窗口禁用时取消事件监听
    /// </summary>
    private void OnDisable()
    {
        // 取消注册EditorApplication.update事件，避免内存泄漏
        EditorApplication.update -= OnEditorUpdate;
    }
    
    /// <summary>
    /// 编辑器每帧更新，用于检测选择变化
    /// </summary>
    private void OnEditorUpdate()
    {
        // 直接在EditorApplication.update中处理选择变化，参考用户提供的实现方式
        if (!isRefreshingSelection)
        {
            isRefreshingSelection = true;
            
            try
            {
                // 检查选择是否变化
                if (HasSelectionChanged())
                {
                    // 选择发生变化，刷新文件列表
                    SelectFbxFiles();
                    // 更新上次选择
                    lastSelection = Selection.objects;
                }
            }
            finally
            {
                // 释放锁
                isRefreshingSelection = false;
            }
        }
    }
    
    /// <summary>
    /// 检查选择是否变化
    /// </summary>
    /// <returns>如果选择变化返回true，否则返回false</returns>
    private bool HasSelectionChanged()
    {
        // 获取当前选择
        UnityEngine.Object[] currentSelection = Selection.objects;
        
        // 检查选择数量是否变化
        if (currentSelection.Length != lastSelection.Length)
        {
            return true;
        }
        
        // 检查选择的对象是否完全相同
        for (int i = 0; i < currentSelection.Length; i++)
        {
            // 检查当前索引位置的对象是否相同
            if (currentSelection[i] != lastSelection[i])
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 当窗口获得焦点时自动更新选中的文件列表
    /// </summary>
    private void OnFocus()
    {
        CollectAndRefresh();
    }

    private void OnGUI()
    {
        // 移除每次绘制GUI时的刷新，改为使用Selection.selectionChanged事件
        // 这样可以避免不必要的性能开销，只在选择真正变化时刷新
        
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        
        GUILayout.Space(10);
        GUILayout.Label("批量修改动画片段名称工具", EditorStyles.boldLabel);
        GUILayout.Space(15);
        
        // 选择FBX文件
        GUILayout.Label("1. 选择FBX文件", EditorStyles.boldLabel);
        GUILayout.Space(5);
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("选择FBX文件", GUILayout.Height(30)))
        {
            SelectFbxFiles();
        }
        
        if (GUILayout.Button("刷新动画片段", GUILayout.Height(30)))
        {
            RefreshAnimationClips();
        }
        GUILayout.EndHorizontal();
        
        // 显示选中的FBX文件数量
        GUILayout.Space(10);
        GUILayout.Label(string.Format("已选择 {0} 个FBX文件", selectedFbxPaths != null ? selectedFbxPaths.Length : 0));
        
        // 显示刷新状态
        GUILayout.Space(10);
        GUILayout.BeginHorizontal();
        GUILayout.Label("刷新状态:", EditorStyles.boldLabel);
        GUILayout.Label(refreshStatus);
        GUILayout.EndHorizontal();
        
        // 显示刷新进度
        if (isRefreshing)
        {
            GUILayout.Space(5);
            GUILayout.Label(string.Format("刷新中... {0}%", Mathf.RoundToInt(refreshProgress * 100)), EditorStyles.boldLabel);
        }
        
        GUILayout.Space(20);
        GUILayout.Label("2. 命名规则设置", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        // 前缀设置
        GUILayout.BeginHorizontal();
        GUILayout.Label("前缀:", GUILayout.Width(100));
        prefix = GUILayout.TextField(prefix);
        GUILayout.EndHorizontal();
        
        // 后缀设置
        GUILayout.BeginHorizontal();
        GUILayout.Label("后缀:", GUILayout.Width(100));
        suffix = GUILayout.TextField(suffix);
        GUILayout.EndHorizontal();
        
        GUILayout.Space(10);
        GUILayout.Label("替换功能", EditorStyles.boldLabel);
        GUILayout.Space(5);
        
        // 替换设置
        GUILayout.BeginHorizontal();
        GUILayout.Label("查找:", GUILayout.Width(100));
        replaceFrom = GUILayout.TextField(replaceFrom);
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        GUILayout.Label("替换为:", GUILayout.Width(100));
        replaceTo = GUILayout.TextField(replaceTo);
        GUILayout.EndHorizontal();
        
        // 正则表达式设置
        GUILayout.Space(10);
        useRegex = GUILayout.Toggle(useRegex, "使用正则表达式替换");
        
        if (useRegex)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("正则表达式:", GUILayout.Width(100));
            regexPattern = GUILayout.TextField(regexPattern);
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("替换格式:", GUILayout.Width(100));
            regexReplacement = GUILayout.TextField(regexReplacement);
            GUILayout.EndHorizontal();
        }
        
        GUILayout.Space(20);
        GUILayout.Label("3. 选项设置", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        // 预览选项
        showPreview = GUILayout.Toggle(showPreview, "显示修改预览");
        
        GUILayout.Space(20);
        GUILayout.Label("4. 执行操作", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("生成预览", GUILayout.Height(35)))
        {
            GeneratePreview();
        }
        
        if (GUILayout.Button("执行修改", GUILayout.Height(35)))
        {
            ExecuteRename();
        }
        GUILayout.EndHorizontal();
        
        // 显示预览信息
        if (showPreview && previewChanges.Count > 0)
        {
            GUILayout.Space(20);
            GUILayout.Label("修改预览:", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            foreach (var kvp in previewChanges)
            {
                GUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label(Path.GetFileName(kvp.Key), EditorStyles.boldLabel);
                
                foreach (var clipChange in kvp.Value)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("  " + clipChange.Key, GUILayout.Width(200));
                    GUILayout.Label(" → ", GUILayout.Width(30));
                    GUILayout.Label(clipChange.Value, EditorStyles.boldLabel);
                    GUILayout.EndHorizontal();
                }
                
                GUILayout.EndVertical();
            }
        }
        
        GUILayout.EndScrollView();
    }
    
    /// <summary>
    /// 手动刷新选择文件
    /// </summary>
    private void SelectFbxFiles()
    {
        CollectAndRefresh();
        
        if (selectedFbxPaths.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "请选择至少一个FBX文件！", "确定");
        }
    }
    
    /// <summary>
    /// 刷新动画片段信息
    /// </summary>
    private void RefreshAnimationClips()
    {
        // 开始刷新
        isRefreshing = true;
        refreshProgress = 0f;
        refreshStatus = "开始刷新...";
        
        // 立即刷新UI，显示刷新状态
        Repaint();
        
        // 清除之前的数据
        fbxAnimationClips.Clear();
        previewChanges.Clear();
        
        // 如果没有选择文件，直接结束
        if (selectedFbxPaths == null || selectedFbxPaths.Length == 0)
        {
            refreshStatus = "就绪";
            isRefreshing = false;
            Repaint();
            return;
        }
        
        // 直接在主线程刷新，确保实时性
        try
        {
            int totalFiles = selectedFbxPaths.Length;
            for (int i = 0; i < totalFiles; i++)
            {
                string fbxPath = selectedFbxPaths[i];
                refreshStatus = string.Format("正在处理: {0}", Path.GetFileName(fbxPath));
                refreshProgress = (float)(i + 1) / totalFiles;
                
                // 立即更新UI，显示进度
                Repaint();
                
                // 检查文件格式兼容性
                string extension = Path.GetExtension(fbxPath).ToLower();
                if (extension == ".fbx")
                {
                    try
                    {
                        // 获取FBX文件中的所有动画片段
                        AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<AnimationClip>().ToArray();
                        if (clips.Length > 0)
                        {
                            fbxAnimationClips.Add(fbxPath, new List<AnimationClip>(clips));
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning(string.Format("解析FBX文件失败: {0}\n{1}", Path.GetFileName(fbxPath), e.Message));
                    }
                }
                else
                {
                    Debug.LogWarning(string.Format("不支持的文件格式: {0}", extension));
                }
            }
            
            refreshStatus = string.Format("刷新完成！共处理 {0} 个文件", totalFiles);
        }
        catch (Exception e)
        {
            refreshStatus = string.Format("刷新失败: {0}", e.Message);
            Debug.LogError(string.Format("刷新动画片段失败: {0}", e.Message));
        }
        finally
        {
            // 结束刷新
            isRefreshing = false;
            refreshProgress = 0f;
            // 立即更新UI，显示最终状态
            Repaint();
        }
    }
    
    /// <summary>
    /// 生成新的动画片段名称
    /// </summary>
    private string GenerateNewClipName(string oldName)
    {
        string newName = oldName;
        
        // 替换功能
        if (!string.IsNullOrEmpty(replaceFrom))
        {
            if (useRegex)
            {
                try
                {
                    newName = System.Text.RegularExpressions.Regex.Replace(newName, regexPattern, regexReplacement);
                }
                catch (Exception e)
                {
                    EditorUtility.DisplayDialog("错误", string.Format("正则表达式错误: {0}", e.Message), "确定");
                    return oldName;
                }
            }
            else
            {
                newName = newName.Replace(replaceFrom, replaceTo);
            }
        }
        
        // 添加前缀和后缀
        newName = prefix + newName + suffix;
        
        return newName;
    }
    
    /// <summary>
    /// 生成修改预览
    /// </summary>
    private void GeneratePreview()
    {
        if (selectedFbxPaths == null || selectedFbxPaths.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "请先选择FBX文件！", "确定");
            return;
        }
        
        previewChanges.Clear();
        
        foreach (var kvp in fbxAnimationClips)
        {
            string fbxPath = kvp.Key;
            Dictionary<string, string> clipChanges = new Dictionary<string, string>();
            
            foreach (var clip in kvp.Value)
            {
                string oldName = clip.name;
                string newName = GenerateNewClipName(oldName);
                
                if (oldName != newName)
                {
                    clipChanges.Add(oldName, newName);
                }
            }
            
            if (clipChanges.Count > 0)
            {
                previewChanges.Add(fbxPath, clipChanges);
            }
        }
        
        if (previewChanges.Count == 0)
        {
            EditorUtility.DisplayDialog("提示", "没有需要修改的动画片段名称！", "确定");
        }
    }
    
    /// <summary>
    /// 执行动画片段重命名
    /// </summary>
    private void ExecuteRename()
    {
        if (selectedFbxPaths == null || selectedFbxPaths.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "请先选择FBX文件！", "确定");
            return;
        }
        
        // 直接执行，去掉确认弹窗
        
        int successCount = 0;
        int failureCount = 0;
        string logMessage = "批量修改动画片段名称报告\n\n";
        
        // 遍历所有选中的FBX文件
        foreach (string fbxPath in selectedFbxPaths)
        {
            logMessage += string.Format("文件: {0}\n", Path.GetFileName(fbxPath));
            
            try
            {
                // 获取FBX导入设置
                UnityEditor.ModelImporter modelImporter = UnityEditor.AssetImporter.GetAtPath(fbxPath) as UnityEditor.ModelImporter;
                if (modelImporter == null)
                {
                    logMessage += "  ✗ 无法获取FBX导入设置\n\n";
                    failureCount++;
                    continue;
                }
                
                // 获取Unity自动生成的动画片段
                AnimationClip[] autoClips = AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<AnimationClip>().ToArray();
                if (autoClips.Length == 0)
                {
                    logMessage += "  ✗ 该FBX文件中没有动画片段\n\n";
                    failureCount++;
                    continue;
                }
                
                // 获取当前动画片段设置
                UnityEditor.ModelImporterClipAnimation[] clipAnimations = modelImporter.clipAnimations;
                
                // 创建动画片段字典
                Dictionary<string, UnityEditor.ModelImporterClipAnimation> clipDict = new Dictionary<string, UnityEditor.ModelImporterClipAnimation>();
                
                // 如果用户没有手动编辑过动画片段设置，clipAnimations可能为空
                // 此时需要创建新的clipAnimations数组
                if (clipAnimations == null || clipAnimations.Length == 0)
                {
                    // 创建新的clipAnimations数组，与Unity自动生成的动画片段对应
                    clipAnimations = new UnityEditor.ModelImporterClipAnimation[autoClips.Length];
                    for (int i = 0; i < autoClips.Length; i++)
                    {
                        clipAnimations[i] = new UnityEditor.ModelImporterClipAnimation();
                        clipAnimations[i].name = autoClips[i].name;
                        clipDict[clipAnimations[i].name] = clipAnimations[i];
                    }
                }
                else
                {
                    // 用户已经手动编辑过动画片段设置，直接使用
                    foreach (var clip in clipAnimations)
                    {
                        clipDict[clip.name] = clip;
                    }
                }
                
                // 记录原始动画片段信息
                logMessage += string.Format("  原始动画片段: {0}\n", string.Join(", ", clipDict.Keys.ToArray()));
                
                // 应用重命名
                int fileSuccessCount = 0;
                foreach (var clip in clipDict.Values.ToArray())
                {
                    string oldName = clip.name;
                    string newName = GenerateNewClipName(oldName);
                    
                    if (oldName != newName)
                    {
                        // 修改动画片段名称
                        clip.name = newName;
                        
                        // 更新字典
                        clipDict.Remove(oldName);
                        clipDict[newName] = clip;
                        
                        logMessage += string.Format("  ✓ {0} → {1}\n", oldName, newName);
                        successCount++;
                        fileSuccessCount++;
                    }
                }
                
                // 更新ModelImporter的clipAnimations
                modelImporter.clipAnimations = clipDict.Values.ToArray();
                modelImporter.SaveAndReimport();
                
                logMessage += string.Format("  ✓ 成功修改 {0} 个动画片段\n\n", fileSuccessCount);
            }
            catch (Exception e)
            {
                logMessage += string.Format("  ✗ 错误: {0}\n\n", e.Message);
                failureCount++;
            }
        }
        
        // 显示结果
        logMessage += string.Format("总计: 成功 {0} 个, 失败 {1} 个\n", successCount, failureCount);
        
        // 保存日志文件
        string logPath = Path.Combine(Application.dataPath, "AnimationRenameLog.txt");
        File.WriteAllText(logPath, logMessage);
        
        EditorUtility.DisplayDialog("完成", string.Format("批量修改完成！\n成功: {0} 个\n失败: {1} 个\n\n日志文件已保存到: {2}", 
            successCount, failureCount, logPath), "确定");
        
        // 刷新AssetDatabase
        AssetDatabase.Refresh();
        // 刷新动画片段列表
        RefreshAnimationClips();
    }
}