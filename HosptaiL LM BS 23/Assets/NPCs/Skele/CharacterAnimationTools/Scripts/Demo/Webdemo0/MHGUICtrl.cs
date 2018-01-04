using UnityEngine;
using System.Collections;

namespace MH
{

    /// <summary>
    /// the Demo scene GUI 
    /// </summary>
    public class MHGUICtrl : MonoBehaviour
    {

        #region "configurable data"
        // configurable data

        public MarkerCtrl m_MarkerCtrl;

        public Transform m_R_Wrist;
        public Transform m_R_Ankle;
        public Transform m_L_Wrist;
        public Transform m_L_Ankle;
        public Transform m_Head;

        public Animator m_Animator;

        #endregion "configurable data"

        #region "data"
        // data

        private Rect m_IntroRect;
        private Rect m_PanelRect;

        #endregion "data"

        #region "unity event handlers"
        // unity event handlers

        void Start()
        {
            m_IntroRect = new Rect(Screen.width - 400, 0, 400, 300);
            m_PanelRect = new Rect(Screen.width - 400, Screen.height - 50, 400, 50);
        }

        void OnGUI()
        {
            GUILayout.BeginArea(m_IntroRect);
            {
                GUI.enabled = false;
                GUILayout.TextArea(
                    "Camera Movement: \n" +
                    "   W:Forward, S:Backward, A:StrafeLeft, D:StrafeRight\n" +
                    "   E:Upward, Q:Downward\n" +
                    "Hold Mouse LB: Raise IK Weight\n" +
                    "Release Mouse LB: Lower IK Weight\n" +
                    "Z: Shrink the Collider-Sphere\n" +
                    "X: Enlarge the Collider-Sphere\n" +
                    "Alt: Show Cursor"
                    );
                GUI.enabled = true;
                GUILayout.Label("Weight: " + m_MarkerCtrl.Weight);
            }
            GUILayout.EndArea();

            GUILayout.BeginArea(m_PanelRect);
            {
                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("R_Wrist"))
                    {
                        m_MarkerCtrl.SetEndEffector(m_R_Wrist);
                        m_MarkerCtrl.SetSecondEndEffector(null);
                    }
                    if (GUILayout.Button("R_Ankle"))
                    {
                        m_MarkerCtrl.SetEndEffector(m_R_Ankle);
                        m_MarkerCtrl.SetSecondEndEffector(null);
                    }
                    if (GUILayout.Button("L_Wrist"))
                    {
                        m_MarkerCtrl.SetEndEffector(m_L_Wrist);
                        m_MarkerCtrl.SetSecondEndEffector(null);
                    }
                    if (GUILayout.Button("L_Ankle"))
                    {
                        m_MarkerCtrl.SetEndEffector(m_L_Ankle);
                        m_MarkerCtrl.SetSecondEndEffector(null);
                    }
                    if (GUILayout.Button("Head"))
                    {
                        m_MarkerCtrl.SetEndEffector(m_Head);
                        m_MarkerCtrl.SetSecondEndEffector(null);
                    }
                    if (GUILayout.Button("Double_Hand"))
                    {
                        m_MarkerCtrl.SetEndEffector(m_L_Wrist);
                        m_MarkerCtrl.SetSecondEndEffector(m_R_Wrist);
                    }
                }
                GUILayout.EndHorizontal();

                if (GUILayout.Button("Toggle Animation"))
                {
                    bool bCurState = m_Animator.GetBool(IDLING_ID);
                    m_Animator.SetBool(IDLING_ID, !bCurState);
                }

            }
            GUILayout.EndArea();

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

        private static int IDLING_ID = Animator.StringToHash("Idling");

        #endregion "constant data"

    }
}