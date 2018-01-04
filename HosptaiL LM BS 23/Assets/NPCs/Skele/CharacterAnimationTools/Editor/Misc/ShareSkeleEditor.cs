using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using MH;

public class ShareSkeleEditor : EditorWindow
{
    private SkinnedMeshRenderer m_targetSMR;
    private SkinnedMeshRenderer m_fromSMR;

    [MenuItem("Window/Skele/Share_Skeleton")]
    public static void OpenWindow()
    {
        GetWindow(typeof(ShareSkeleEditor));
    }

    public void OnGUI()
    {
        m_targetSMR = EditorGUILayout.ObjectField("Main SMR:", m_targetSMR, typeof(SkinnedMeshRenderer), true) as SkinnedMeshRenderer;
        m_fromSMR = EditorGUILayout.ObjectField("Extra SMR:", m_fromSMR, typeof(SkinnedMeshRenderer), true) as SkinnedMeshRenderer;

        bool bAllSet = (m_targetSMR != null && m_fromSMR != null);
        Color c =  bAllSet ? Color.green : Color.red;
        EUtil.PushGUIEnable(bAllSet);
        if( EUtil.Button("Share_Skele", c) )
        {
            Undo.RecordObject(m_fromSMR, "ShareSkele");
            ShareSkeleton.ShareSkele(m_targetSMR, m_fromSMR);
        }
        EUtil.PopGUIEnable();
    }

}
