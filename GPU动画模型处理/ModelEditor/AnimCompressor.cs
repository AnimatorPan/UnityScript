using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;

public class AnimCompressor
{
    /// <summary>
    /// 删除多余的关键帧，并且保存为资源
    /// </summary>
    /// <param name="ac"></param>
    /// <param name="pathMy"></param>
    public static void RemoveAnimScaleCurves(AnimationClip ac, string pathMy, Transform model)
    {
        if (ac == null)
        {
            return;
        }
        var curves = AnimationUtility.GetAllCurves(ac);

        var needRemoveMap = new Dictionary<string, bool>();
        var whiteListMap = new Dictionary<string, bool>();

        foreach (AnimationClipCurveData curve in curves)
        {
            var path = GetPropertyName(curve.propertyName);

            var key = curve.path + path;
            if (CheckCanRemoveCurve(curve, model))
            {
                if (!needRemoveMap.ContainsKey(key))
                {
                    needRemoveMap.Add(key, true);
                }
            }
            else
                whiteListMap[key] = true;
        }

        foreach (var kv in needRemoveMap)
        {
            if (kv.Value && !whiteListMap.ContainsKey(kv.Key))
            {
                var propertyName = GetPropertyName(kv.Key);
                var path = string.IsNullOrEmpty(propertyName) ? kv.Key : kv.Key.Remove(kv.Key.IndexOf(propertyName, System.StringComparison.Ordinal));
                if ("m_LocalScale" == propertyName || !ac.name.Equals("Idle", System.StringComparison.CurrentCultureIgnoreCase))
                {
                    ac.SetCurve(path, typeof(Transform), propertyName, null);
                }
            }
        }

        //处理精度
        CutAnimPrecision(pathMy);
    }

    private const float epsilon = 0.0001f;

    private static bool CheckCanRemoveCurve(AnimationClipCurveData curveData, Transform model)
    {
        if (curveData.curve.length <= 2)
            return false;
        var keys = curveData.curve.keys;
        var first = keys[0];
        float fEpsilon = epsilon;
        if (curveData.propertyName.IndexOf("m_LocalRotation") >= 0)
        {
            fEpsilon = Mathf.Epsilon;
        }

        foreach (var keyframe in keys)
        {
            if (Mathf.Abs(first.value - keyframe.value) > fEpsilon)
            {
                return false;
            }
        }

        Keyframe end = keys[curveData.curve.length - 1];
        if (Mathf.Abs(first.value - end.value) > fEpsilon)
        {
            return false;
        }
        float val = curveData.curve.Evaluate(end.time / 2f);
        if (Mathf.Abs(first.value - val) > fEpsilon)
        {
            return false;
        }

        if (model != null)
        {
            //关键帧的位置和默认模型的位置不一样
            Transform find = model.Find(curveData.path);
            if (find != null)
            {
                float value = 0f;
                if (curveData.propertyName == "m_LocalRotation.x")
                {
                    value = find.localRotation.x;
                }
                else if (curveData.propertyName == "m_LocalRotation.y")
                {
                    value = find.localRotation.y;
                }
                else if (curveData.propertyName == "m_LocalRotation.z")
                {
                    value = find.localRotation.z;
                }
                else if (curveData.propertyName == "m_LocalRotation.w")
                {
                    value = find.localRotation.w;
                }
                else if (curveData.propertyName == "m_LocalScale.x")
                {
                    value = find.localScale.x;
                }
                else if (curveData.propertyName == "m_LocalScale.y")
                {
                    value = find.localScale.y;
                }
                else if (curveData.propertyName == "m_LocalScale.z")
                {
                    value = find.localScale.z;
                }
                else if (curveData.propertyName == "m_LocalPosition.x")
                {
                    value = find.localPosition.x;
                }
                else if (curveData.propertyName == "m_LocalPosition.y")
                {
                    value = find.localPosition.y;
                }
                else if (curveData.propertyName == "m_LocalPosition.z")
                {
                    value = find.localPosition.z;
                }
                else
                {
                    //Debug.LogError(curveData.propertyName);
                }


                if (Mathf.Abs(first.value - value) > fEpsilon)
                {
                    return false;
                }
            }
        }
        return true;
    }

    private static string GetPropertyName(string property)
    {
        string ret = "";
        if (property.IndexOf("m_LocalScale", System.StringComparison.Ordinal) != -1)
        {
            ret = "m_LocalScale";
        }
        if (property.IndexOf("m_LocalPosition", System.StringComparison.Ordinal) != -1)
        {
            ret = "m_LocalPosition";
        }
        if (property.IndexOf("m_LocalRotation", System.StringComparison.Ordinal) != -1)
        {
            ret = "m_LocalRotation";
        }
        return ret;
    }

    static int Precision = 5;   //压缩精度
    static string StrPrecision = "f" + Precision.ToString();
    static StringBuilder appendBuff = new StringBuilder();
    static List<string> newResult = new List<string>(200000);
    /// <summary>
    /// Unity5的动画格式，会莫名插入一些换行，所以这儿特殊处理下,把换行的也包括进来
    /// </summary>
    /// <param name="raw"></param>
    /// <returns></returns>
    private static List<string> MergeAnimNewLineString(string[] raw)
    {
        newResult.Clear();
        bool appendNext = false;
        appendBuff.Clear();
        for (int i = 0; i < raw.Length; i++)
        {
            var currRaw = raw[i];
            if (appendNext)
            {
                appendBuff.Append(raw[i]);
                if (currRaw.IndexOf('}') >= 0)
                {
                    appendNext = false;
                    newResult.Add(appendBuff.ToString());
                    appendBuff.Remove(0, appendBuff.Length);
                }
            }
            else
            {
                if (currRaw.IndexOf('{') >= 0 && currRaw.IndexOf('}') < 0)
                {
                    appendNext = true;
                    appendBuff.Remove(0, appendBuff.Length);

                    appendBuff.Append(currRaw);
                }
                else
                {
                    newResult.Add(currRaw);
                }
            }
        }

        if (appendNext)
        {
            newResult.Add(appendBuff.ToString());
        }

        return newResult;
    }
    public static string[] QuickSplitString(string srcStr, char tag)
    {
        var idx = srcStr.IndexOf(tag);
        if (idx > 0 && idx < srcStr.Length - 1)
        {
            var result = new string[2];
            result[0] = srcStr.Substring(0, idx);
            result[1] = srcStr.Substring(idx + 1);
            return result;
        }
        return null;
    }
    static StringBuilder sb = new StringBuilder(2048 * 1024);
    static int GetVectorValueIndex(string str)
    {
        int index = str.IndexOf("value");
        if (index >= 0)
            return index + 5;
        index = str.IndexOf("inSlope");
        if (index >= 0)
            return index + 7;
        index = str.IndexOf("outSlope");
        if (index >= 0)
            return index + 8;
        index = str.IndexOf("inWeight");
        if (index >= 0)
            return index + 8;
        index = str.IndexOf("outWeight");
        if (index >= 0)
            return index + 9;

        return -1;
    }
    //static StringBuilder sbNew = new StringBuilder(256);
    public static void CutAnimPrecision(string path)
    {
        string[] allStrs = File.ReadAllLines(path);
        if (allStrs == null)
        {
            return;
        }

        List<string> strArray = MergeAnimNewLineString(allStrs);
        File.Delete(path);
        FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        StreamWriter sw = new StreamWriter(fs);
        sw.Flush();
        sw.BaseStream.Seek(0, SeekOrigin.Begin);
        sb.Clear();
        var count = strArray.Count;
        for (int j = 0; j < count; j++)
        {
            var strItem = strArray[j];
            if (sb.Length > 1024 * 1024)
            {
                sw.Write(sb.ToString());
                sb.Clear();
            }

            if (strItem.Contains("time"))
            {
                var idx = strItem.IndexOf(':');
                //string[] txts = QuickSplitString(strItem, ':');//strs[j].Split(':');
                if (idx > 0 && idx < strItem.Length - 1)
                {
                    var text1 = strItem.Substring(idx + 1);
                    var dotIndex = text1.IndexOf('.');
                    if (dotIndex >= 0 && (text1.Length - dotIndex - 1) >= Precision)
                    {
                        float f = float.Parse(text1);
                        if (f <= 0.000005f && f >= -0.000005f)
                        {
                            text1 = "0";
                        }
                        else
                        {
                            text1 = f.ToString(StrPrecision);
                        }
                    }
                    sb.Append(strItem, 0, idx + 1);  //txts[0] + ": " + txts[1];
                    sb.Append(" ");
                    sb.AppendLine(text1);
                }
                else
                {
                    sb.AppendLine(strItem);
                }
            }
            else
            {
                var startIndex = GetVectorValueIndex(strItem);
                if (startIndex >= 0)
                {
                    //strs[j] = Trim(strs[j]);
                    int frontindex = strItem.IndexOf('{', startIndex);
                    if (frontindex == -1)
                    {
                        if (!strItem.EndsWith(": 0"))
                        {
                            sb.AppendLine(strItem);
                        }
                        //strArray[j] = str;
                        continue;
                    }
                    int behindindex = strItem.IndexOf('}', frontindex);

                    string str = strItem.Substring(frontindex + 1, behindindex - frontindex - 1);
                    if (str != null)
                    {
                        string[] txts = str.Split(',');
                        if (txts != null)
                        {
                            sb.Append(strItem, 0, frontindex + 1);
                            //string tt_new = null;
                            for (int k = 0; k < txts.Length; k++)
                            {
                                var index = txts[k].IndexOf(":");
                                var newStr1 = txts[k].Substring(index + 1);
                                //string[] newstr = QuickSplitString(txts[k], ':');// txts[k].Split(':');
                                var dotIndex = newStr1.IndexOf('.');
                                if (dotIndex >= 0 && (newStr1.Length - dotIndex - 1) >= Precision)
                                {
                                    float f = float.Parse(newStr1);
                                    if (f <= 0.000005f && f >= -0.000005f)
                                    {
                                        newStr1 = "0";
                                    }
                                    else
                                    {
                                        newStr1 = f.ToString(StrPrecision);
                                    }
                                }
                                sb.Append(txts[k], 0, index);
                                sb.Append(": ");
                                sb.Append(newStr1);
                                sb.Append(k == txts.Length - 1 ? "" : ",");
                            }
                            sb.AppendLine("}");
                        }
                    }
                }
                else
                {
                    sb.AppendLine(strArray[j]);
                }
            }
        }

        sw.Write(sb.ToString());
        sw.Flush();
        sw.Close();
    }
}

