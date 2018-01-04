using UnityEngine;
using System.Collections;
using MH;
using ExtMethods;

namespace MH
{
    /// <summary>
    /// attach on head, control head rotation, 
    /// use LMB to control right hand with IK;
    /// </summary>
    public class CCDemo0_HeadCtrl : MonoBehaviour
    {
        #region "configurable data"
        // configurable data

        public CutsceneController m_CC;

        public Collider m_ColliderQuad;

        // head rotation
        public float m_RotSpeed = 120f; //head rotation angle speed
        public float m_RotThres = 30f; //the rotation angle threshold

        // IK for right hand
        public float m_IKWeight = 0f;
        public float m_IKWeightIncSpeed = 3f;
        public Transform m_PlayerRightHand;
        public bool m_AllowManualControl = true;

        //throw
        public Transform m_ThrowRefTr;
        public float m_XVar = 1f;
        public float m_YVar = 1f;
        public float m_ThrowTime = 1f; //the time cube fly in the sky

        //GUI
        public Rect m_RectUI = new Rect(0, 0, 250, 100);
        public Texture2D m_WSADLMB;

        #endregion "configurable data"

        #region "data"
        // data

        private Transform m_Tr;
        private IKRTSolver m_Solver;
        private Vector3 m_StartDir; //used to constrain the head rotation
        private Transform m_CubeTr = null;
        private Parabola m_Parabola = new Parabola();

        private XformData m_TransformBackup = new XformData();
        private XformData m_StartTrBackup = new XformData();

        private bool m_bCubeThrown = false; //flag for cube is thrown

        #endregion "data"

        #region "unity event handlers"
        // unity event handlers

        void Start()
        {
            m_Tr = transform;
            m_Solver = new IKRTSolver();
            m_Solver.SetBones(m_PlayerRightHand, 2);

            m_StartDir = m_Tr.parent.InverseTransformDirection(m_Tr.up).normalized;
            m_TransformBackup.CopyFrom(m_Tr);
            m_StartTrBackup.CopyFrom(m_Tr);
        }

        void Update()
        {
            m_PlayerRightHand.LookAt(m_PlayerRightHand.position + Vector3.left, Vector3.back); //keep right hand upward

            if (m_Parabola.isRunning)
            {
                m_CubeTr.position = m_Parabola.Update(Time.deltaTime);
                //Dbg.Log("{2}, parabola update: {0}, {1}", m_CubeTr.position, m_CubeTr.rigidbody.velocity, Time.frameCount);
            }
            else
            {
                if (m_bCubeThrown)
                {
                    Msg_CubeDropped();
                }
            }
        }

        void OnGUI()
        {
            if (m_AllowManualControl)
            {
                GUI.DrawTexture(m_RectUI, m_WSADLMB);
            }
        }

        void LateUpdate()
        {
            // head control
            if (m_AllowManualControl)
            {
                float XRotDelta = -Input.GetAxis("Horizontal") * Time.deltaTime * m_RotSpeed;
                float ZRotDelta = Input.GetAxis("Vertical") * Time.deltaTime * m_RotSpeed;

                XformData backup = m_TransformBackup;
                backup.Apply(m_Tr);
                m_Tr.Rotate(XRotDelta, 0, ZRotDelta, Space.Self);

                Vector3 currentUp = m_Tr.parent.InverseTransformDirection(m_Tr.up).normalized;
                float cosVal = Vector3.Dot(currentUp, m_StartDir);
                float costhres = Mathf.Cos(m_RotThres * Mathf.Deg2Rad);

                if (cosVal < costhres)
                {
                    backup.Apply(m_Tr);
                }
                else
                {
                    m_Tr.LookAtYX(m_Tr.up + m_Tr.position, Vector3.down); // ensure head upward
                    backup.CopyFrom(m_Tr);
                }
            }
            else
            {
                if (m_IKWeight > 0)
                {
                    m_Tr.localRotation = Quaternion.Lerp(m_Tr.localRotation, m_TransformBackup.rot, m_IKWeight);
                }
                else
                {
                    m_TransformBackup.CopyFrom(m_StartTrBackup); //reset the data
                }
            }

            // hand IK control
            if (Input.GetMouseButton(0) && m_AllowManualControl)
            {
                m_IKWeight = Mathf.Clamp01(m_IKWeight + m_IKWeightIncSpeed * Time.deltaTime);

                RaycastHit hit;
                Ray ray = new Ray(m_Tr.position, m_Tr.up);
                if (m_ColliderQuad.Raycast(ray, out hit, float.PositiveInfinity))
                {
                    Vector3 hitpos = hit.point;
                    m_Solver.Target = hitpos;
                    m_Solver.Execute(m_IKWeight);
                }
            }
            else
            {
                if (m_IKWeight > 0)
                {
                    m_IKWeight = Mathf.Clamp01(m_IKWeight - m_IKWeightIncSpeed * Time.deltaTime);
                    m_Solver.Execute(m_IKWeight);
                }
            }

        }

        #endregion "unity event handlers"

        #region "public method"
        // public method

        #endregion "public method"

        #region "private method"
        // private method

        #region "Message Functions"
        // "Message Functions" 

        private void Msg_Throw(CC_MsgParam param)
        {
            GameObject cubeGO = param.m_Object as GameObject;
            Transform cubeTr = m_CubeTr = cubeGO.transform;

            Vector3 curPos = cubeTr.position;
            Vector3 dstPos = _GetThrowDestPos();

            m_Parabola.Init(curPos, dstPos, m_ThrowTime, 3 * 9.8f);

            m_bCubeThrown = true;
        }

        private void Msg_CaughtTheCube()
        {
            m_Parabola.Stop();

            Misc.AddChild(m_PlayerRightHand, m_CubeTr);

            Dbg.Log("Caught cube");

            m_bCubeThrown = false;

            CutsceneController.JumpToTimeTag(m_CC, "GotCube");
        }

        private void Msg_CubeDropped()
        {
            Dbg.Log("DropCube");

            m_bCubeThrown = false;

            CutsceneController.JumpToTimeTag(m_CC, "DropCube");
        }

        #endregion "Message Functions"

        private Vector3 _GetThrowDestPos()
        {
            Vector3 pos = m_ThrowRefTr.position;
            pos.x += Random.Range(-m_XVar, m_XVar);
            pos.y += Random.Range(-m_YVar, m_YVar);

            return pos;
        }

        #endregion "private method"

        #region "constant data"
        // constant data

        #endregion "constant data"
    }
}