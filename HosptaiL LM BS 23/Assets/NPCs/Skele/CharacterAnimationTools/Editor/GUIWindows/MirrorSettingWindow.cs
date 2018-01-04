using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH
{

	public class MirrorSettingWindow : GUIWindow
	{
        public delegate void Callback(bool bOK);

        private MirrorCtrl m_MirrorCtrl;

        private Axis m_SymAxis;
        private Axis m_NonSymAxis;

        private Texture2D m_background;

        private Callback m_OnFinish;

        public MirrorSettingWindow(Callback onFinish)
        {
            m_background = SMREditor.GetBG();
            m_MirrorCtrl = SMREditor.GetMirrorCtrl();
            m_MirrorCtrl.AutoDetectAxis();
            m_SymAxis = m_MirrorCtrl.SymBoneAxis;
            m_NonSymAxis = m_MirrorCtrl.NonSymBoneAxis;

            m_OnFinish = onFinish;
        }

        public override EReturn OnGUI()
        {
            EUtil.PushGUIEnable(true);
            Rect rc = new Rect(Screen.width * 0.5f - 150, Screen.height * 0.5f - 50f, 300, 70);

            if (m_background != null)
                GUI.DrawTexture(rc, m_background);

            GUILayout.BeginArea(rc);
            {
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("SymBone:", EditorStyles.boldLabel);
                    m_SymAxis = (Axis)EditorGUILayout.EnumPopup(m_SymAxis);
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("NonSymBone:", EditorStyles.boldLabel);
                    m_NonSymAxis = (Axis)EditorGUILayout.EnumPopup(m_NonSymAxis);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Separator();

                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("OK"))
                    {
                        m_MirrorCtrl.SymBoneAxis = m_SymAxis;
                        m_MirrorCtrl.NonSymBoneAxis = m_NonSymAxis;

                        m_OnFinish(true);

                        return EReturn.STOP;
                    }
                    if (GUILayout.Button("Cancel"))
                    {
                        m_OnFinish(false);
                        return EReturn.STOP;
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndArea();
            

            return EReturn.MODAL;
        }
	}
}
