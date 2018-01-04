using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;

namespace MH
{
	public class AnimatorDebugger : EditorWindow
	{
	    #region "configurable data"
        // configurable data

        #endregion "configurable data"

	    #region "data"
        // data

        private Animator m_CurAnimator;

        private AnimBool m_transBool = new AnimBool();

        private string m_paramName = string.Empty;
        private PType m_type;
        private bool m_bool;
        private int m_int;
        private float m_float;

        private AnimBool m_jumpToggle = new AnimBool();
        private string m_stateName = string.Empty;
        private float m_fadeTime = 0;

        private float m_step = ONE_FRAME;

        #endregion "data"

	    #region "unity event handlers"
        // unity event handlers

        [MenuItem("Window/Skele/AnimatorDebugger")]
        public static void OpenWindow()
        {
            var wnd = GetWindow(typeof(AnimatorDebugger)) as AnimatorDebugger;
            EUtil.SetEditorWindowTitle(wnd, "AnimDebugger");
            wnd.autoRepaintOnSceneChange = true;
            wnd.OnSelectionChange();
            wnd.minSize = new Vector2(300f, 300f);
        }

        void OnEnable()
        {
            m_transBool.valueChanged.RemoveListener( Repaint );
            m_transBool = new AnimBool();
            m_transBool.valueChanged.AddListener( Repaint );

            m_jumpToggle.valueChanged.RemoveListener(Repaint);
            m_jumpToggle = new AnimBool();
            m_jumpToggle.valueChanged.AddListener(Repaint);
        }

        void OnDisable()
        {
            if (m_transBool != null)
                m_transBool.valueChanged.RemoveAllListeners();
            if (m_jumpToggle != null)
                m_jumpToggle.valueChanged.RemoveAllListeners();
        }

        void OnGUI()
        {
            if( m_CurAnimator == null )
            {
                GUILayout.Label("Select animator gameobject first !");
                return;
            }
            if (!EUtil.HasAnimatorController(m_CurAnimator))
            {
                GUILayout.Label("The Animator doesn't have AnimatorController set!");
                return;
            }

            var stateInfo = m_CurAnimator.GetCurrentAnimatorStateInfo(0);
            GUILayout.Label(string.Format("Cur: {0}, state: {1}", m_CurAnimator.name, EUtil.GetStateNameHash(stateInfo)));
            float nt = stateInfo.normalizedTime;
            float len = stateInfo.length;
            float t = nt * len;
            GUILayout.Label(string.Format("Cur time: nt:{0}, t:{1}", nt, t));

            Event e = Event.current;
            if( e.type == EventType.KeyDown )
            {
                float delta = 0;
                if (e.keyCode == KeyCode.RightArrow)
                {
                    delta = m_step;
                }
                else if (e.keyCode == KeyCode.LeftArrow)
                {
                    delta = -m_step;
                }
                else if (e.keyCode == KeyCode.Home)
                {
                    delta = -t;
                }

                if (!Mathf.Approximately(0, delta))
                {
                    m_CurAnimator.Update(delta);
                    Repaint();
                }
            }

            m_transBool.target = EditorGUILayout.ToggleLeft("Set Param", m_transBool.target);
            if (EditorGUILayout.BeginFadeGroup(m_transBool.faded))
            {
                var allParamNames = new List<string>();
                EUtil.GetAllParameterNames(allParamNames, m_CurAnimator);
                int idx = Mathf.Max(0, allParamNames.IndexOf(m_paramName));
                idx = EditorGUILayout.Popup("paramName", idx, allParamNames.ToArray());
                if (allParamNames.Count > 0)
                    m_paramName = allParamNames[idx];

                m_type = (PType)EditorGUILayout.EnumPopup("type", m_type);
                switch (m_type)
                {
                    case PType.Bool: m_bool = EditorGUILayout.Toggle("bool", m_bool); break;
                    case PType.Float: m_float = EditorGUILayout.FloatField("float", m_float); break;
                    case PType.Int: m_int = EditorGUILayout.IntField("int", m_int); break;
                }
                EUtil.PushGUIEnable(!string.IsNullOrEmpty(m_paramName));
                if (GUILayout.Button("execute"))
                {
                    switch (m_type)
                    {
                        case PType.Bool: m_CurAnimator.SetBool(m_paramName, m_bool); break;
                        case PType.Float: m_CurAnimator.SetFloat(m_paramName, m_float); break;
                        case PType.Int: m_CurAnimator.SetInteger(m_paramName, m_int); break;
                        case PType.Trigger: m_CurAnimator.SetTrigger(m_paramName); break;
                    }
                }
                EUtil.PopGUIEnable();
            }
            EditorGUILayout.EndFadeGroup();

            List<string> allNames = new List<string>();
            EUtil.GetAllStateNames(allNames, m_CurAnimator);
            m_jumpToggle.target = EditorGUILayout.ToggleLeft("jump to", m_jumpToggle.target);
            if (EditorGUILayout.BeginFadeGroup(m_jumpToggle.faded))
            {
                int idx = Mathf.Max(0, allNames.IndexOf(m_stateName));
                idx = EditorGUILayout.Popup("stateName", idx, allNames.ToArray());
                if (allNames.Count > 0)
                    m_stateName = allNames[idx];

                m_fadeTime = Mathf.Max(0, EditorGUILayout.FloatField("fade time", m_fadeTime));
                if (GUILayout.Button("execute"))
                {
                    if (!EUtil.HasState(m_CurAnimator, m_stateName))
                        Dbg.Log("unknown stateName: {0}", m_stateName);
                    else
                    {
                        if (m_fadeTime > 0)
                            m_CurAnimator.CrossFade(m_stateName, m_fadeTime);
                        else
                            m_CurAnimator.Play(m_stateName);
                    }                    
                }
            }
            EditorGUILayout.EndFadeGroup();

            EUtil.DrawSplitter();
            EUtil.DrawSplitter();

            m_step = EditorGUILayout.Slider("Step", m_step, 0, 1f);

            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("<-", GUILayout.Height(40f)))
                {
                    m_CurAnimator.Update(-m_step);
                }
                GUILayout.Space(5f);
                if (GUILayout.Button("->", GUILayout.Height(40f)))
                {
                    m_CurAnimator.Update(m_step);
                }
            }
            GUILayout.EndHorizontal();

            if (EUtil.Button("Rebind", Color.red))
            {
                m_CurAnimator.Rebind();
                m_CurAnimator.Update(0);
            }
        }

        void OnSelectionChange()
        {
            m_CurAnimator = null;
            if( Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<Animator>() != null )
            {
                m_CurAnimator = Selection.activeGameObject.GetComponent<Animator>();
            }
            Repaint();
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

        public const float ONE_FRAME = 0.016f;

        public enum PType
        {
            Bool,
            Int,
            Float,
            Trigger,
        }

        #endregion "constant data"

	}
}
