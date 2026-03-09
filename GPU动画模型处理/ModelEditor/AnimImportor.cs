using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class AnimImportor
{

    [MenuItem("Assets/模型处理功能/处理FPS模型的OptimizeGameObject")]
    private static void ProcessExistGameObject()
    {
        Object[] objects = Selection.objects;
        foreach (var o in objects)
        {
            string path = AssetDatabase.GetAssetPath(o);
            ModelImporter modelAnimImporter = AssetImporter.GetAtPath(path) as ModelImporter;
            modelAnimImporter.extraExposedTransformPaths = GetExtractExposedPath(modelAnimImporter);
            modelAnimImporter.optimizeGameObjects = true;
            
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }
    }

    public static string[] GetExtractExposedPath(ModelImporter modelImporter)
    {
        List<string> dummys = new List<string>();
        for (int i = 0; i < (int)DummyPoint.DM_MAX; i++)
        {
            string dumName = ((DummyPoint)i).ToString();
            bool bHave = false;

            foreach (var s in modelImporter.transformPaths)
            {
                if (s.Contains(dumName))
                {
                    bHave = true;
                    break;
                }
            }

            if (bHave)
            {
                dummys.Add(dumName);
            }
        }
        return dummys.ToArray();
    }


    /// <summary>
    /// 设置动画的take信息,第一次导入时会找不到take,所以先创建.
    /// </summary>
    /// <param name="modelAnimImporter">模型动画信息对象</param>
    /// <param name="takeInfo">take信息集合</param>
    /// <returns></returns>
    public static ModelImporterClipAnimation[] SetupDefaultClips(ModelImporter modelAnimImporter, TakeInfo[] takeInfo)
    {
        ModelImporterClipAnimation[] clips;
        if (modelAnimImporter.clipAnimations.Length <= 0)
        {
            clips = new ModelImporterClipAnimation[1];
            ModelImporterClipAnimation mica = new ModelImporterClipAnimation();
            mica.name = takeInfo[0].defaultClipName;
            mica.takeName = takeInfo[0].name;
            mica.firstFrame = (float)((int)Mathf.Round(takeInfo[0].bakeStartTime * takeInfo[0].sampleRate));
            mica.lastFrame = (float)((int)Mathf.Round(takeInfo[0].bakeStopTime * takeInfo[0].sampleRate));
            mica.maskType = ClipAnimationMaskType.CreateFromThisModel;
            //mica.keepOriginalPositionY = true;

            if (mica.name.Contains("idle") || 
                mica.name.Contains("run") || 
                mica.name.Contains("walk") || 
                mica.name.Contains("turn") || 
                mica.name.Contains("loop") ||
                mica.name.Contains("shooting") ||//quickfall
                mica.name.Contains("coma")||
                mica.name.Contains("swimming") ||
                    mica.name.Contains("swim") ||
                mica.name.Contains("wlk") ||
                mica.name.Contains("quickfall")
                ) 
            {
                mica.loopTime = true;
            }
            mica.lockRootRotation = true;
            mica.lockRootHeightY = true;
            mica.keepOriginalPositionY = true;

            clips[0] = mica;
        }
        else
        {
            clips = new ModelImporterClipAnimation[modelAnimImporter.clipAnimations.Length];
            for (int i = 0; i < clips.Length; i++)
            {
                ModelImporterClipAnimation mica = new ModelImporterClipAnimation();
                mica.name = modelAnimImporter.clipAnimations[i].name;//takeInfo[0].defaultClipName;
                mica.takeName = modelAnimImporter.clipAnimations[i].takeName;//takeInfo[0].name;
                mica.firstFrame = modelAnimImporter.clipAnimations[i].firstFrame;//(float)((int)Mathf.Round(takeInfo[0].bakeStartTime * takeInfo[0].sampleRate));
                mica.lastFrame = modelAnimImporter.clipAnimations[i].lastFrame;//(float)((int)Mathf.Round(takeInfo[0].bakeStopTime * takeInfo[0].sampleRate));
                mica.maskType = ClipAnimationMaskType.CreateFromThisModel;
                //mica.keepOriginalPositionY = true;

                if (mica.name.Contains("idle") ||
                    mica.name.Contains("run") ||
                    mica.name.Contains("walk") ||
                    mica.name.Contains("turn") ||
                    mica.name.Contains("loop") ||
                    mica.name.Contains("shooting") ||//swimming
                    mica.name.Contains("coma") ||
                    mica.name.Contains("swimming") ||
                    mica.name.Contains("swim") ||
                    mica.name.Contains("wlk") ||
                mica.name.Contains("quickfall")
                    ) 
                {
                    mica.loopTime = true;
                }
                mica.lockRootRotation = true;
                mica.lockRootHeightY = true;
                mica.keepOriginalPositionY = true;

                clips[i] = mica;
            }
        }

        return clips;
    }
}
