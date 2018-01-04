using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MH
{

public class ShowBindPose : EditorWindow
{
	#region "data"
    // data

    private SkinnedMeshRenderer m_SMR;
    private bool m_bShowing = false;

    #endregion "data"

	#region "unity event handlers"
    // unity event handlers

    //[MenuItem("Window/ShowBindPose")]
    public static void ShowWindow()
    {
        GetWindow(typeof(ShowBindPose));
    }

	void Start()
	{

    }

    void OnDestroy()
    {
        SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
    }

	void OnGUI()
    {
        m_SMR = EditorGUILayout.ObjectField(m_SMR, typeof(SkinnedMeshRenderer), true) as SkinnedMeshRenderer;

        bool bValid = (m_SMR != null);
        GUIUtil.PushGUIEnable(bValid);
        if( EUtil.Button(m_bShowing ? "Stop!" : "Run!", Color.white))
        {
            m_bShowing = !m_bShowing;
            if( m_bShowing )
                SceneView.onSceneGUIDelegate += this.OnSceneGUI;
            else
                SceneView.onSceneGUIDelegate -= this.OnSceneGUI;

            SceneView.lastActiveSceneView.Repaint();
        }
        GUIUtil.PopGUIEnable();
    }

    void OnSceneGUI(SceneView view)
    {
        Transform animRoot = m_SMR.transform;
        while (animRoot != null)
        {
            if (animRoot.GetComponent<Animation>() != null || animRoot.GetComponent<Animator>() != null)
                break;
            animRoot = animRoot.parent;
        }
        if (animRoot == null)
            return;

        Matrix4x4[] invbinds = m_SMR.sharedMesh.bindposes;
        for(int idx = 0; idx < invbinds.Length; ++idx) 
        {
            var tr = m_SMR.bones[idx];
            var inv = invbinds[idx];
            var m = inv.inverse;
            //m = m * animRoot.localToWorldMatrix;
            //m = m_SMR.rootBone.localToWorldMatrix * m;
            m = animRoot.localToWorldMatrix * m;

            Vector3 pos = m.MultiplyPoint(Vector3.zero);
            Vector3 vZ = m.GetColumn(2);
            Vector3 vY = m.GetColumn(1);
            Quaternion q = Quaternion.LookRotation(vZ, vY);

            Handles.PositionHandle(pos, q);
            Handles.BeginGUI();
            Rect rc = HandleUtility.WorldPointToSizedRect(pos, new GUIContent(tr.name), GUIStyle.none);
            GUI.Label(rc, new GUIContent(tr.name));
            Handles.EndGUI();
        }
    }

    #endregion "unity event handlers"

	#region "public method"
    // public method

    #endregion "public method"

	#region "private method"
    // private method

    #endregion "private method"

	#region "constant data"
    // constant data

    #endregion "constant data"

}

}
