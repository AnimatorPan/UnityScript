using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector.Editor;
[UnityEditor.CustomEditor(typeof(HumanIK))]
public class HumanIKEditor : OdinEditor
{
    private void OnEnable()
    {
        var humanIK = target as HumanIK;
        if (humanIK != null)
            humanIK.OnInit();
    }
}
