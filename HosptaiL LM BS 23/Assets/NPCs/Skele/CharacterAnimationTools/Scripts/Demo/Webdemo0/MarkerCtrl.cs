using UnityEngine;
using System;
using System.Collections.Generic;

using MH.Constraints;

/// <summary>
/// 1. place marker on the collider
/// 2. execute IK solver
/// 
/// how to work with IK base solver?
/// 1. call SetBones(endEffector, len);
/// 2. set solver.Target whenever you want to change it;
/// 3. call solver.Execute(weight) whenever you want it to work, 
/// 
/// That's all. Be noted that there is no overhead if don't call Execute()
/// 
/// The IKRTSolver doesn't utilize the ConstraintInfo like LimbSolver, so in some circumstances it will have problems of 
/// flipped mid-joint and overly-rotate IKRoot joint
/// 
/// </summary>
namespace MH
{
    public class MarkerCtrl : MonoBehaviour
    {
        #region "data"
        // data

        public Transform m_Marker;
        public Transform m_Collider;
        public Transform m_EndEffector;

        public float m_ColliderScaleSpeed = 5.0f;
        public float m_WeightSpeed = 1.0f;

        private float m_Weight = 0f;

        private ISolver m_IK_0; //the main IK solver
        private ISolver m_IK_1; //used to demonstrate multiple IK; 

        #endregion "data"

        #region "unity event handlers"
        // unity event handlers

        void Start()
        {
            SetEndEffector(m_EndEffector);
        }

        void Update()
        {
            // marker place
            Ray ray = GetComponent<Camera>().ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, float.PositiveInfinity, 1 << LayerMask.NameToLayer(COLLIDER_LAYER)))
            {
                m_Marker.position = hit.point;
            }

            // control collider scale
            if (Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.X))
            {
                float dir = Input.GetKey(KeyCode.Z) ? -1 : 1;
                Vector3 scale = m_Collider.localScale;
                float curScale = scale.x;
                curScale += dir * m_ColliderScaleSpeed * Time.deltaTime;
                curScale = Mathf.Clamp(curScale, 10.0f, 30.0f);
                scale.Set(curScale, curScale, curScale);
                m_Collider.localScale = scale;
            }
        }

        void LateUpdate()
        {
            // control weight
            if (Input.GetMouseButton(0))
            {
                m_Weight += m_WeightSpeed * Time.deltaTime;
                m_Weight = Mathf.Clamp01(m_Weight);
            }
            else
            {
                m_Weight -= m_WeightSpeed * Time.deltaTime;
                m_Weight = Mathf.Clamp01(m_Weight);
            }

            // execute IK solver, 
            // must ensure that be called after Animator update, if don't play animation, should be ok to call it anywhere
            if (m_IK_0 != null)
            {
                m_IK_0.Target = m_Marker.position;
                m_IK_0.Execute(m_Weight);
            }            

            if (m_IK_1 != null && m_IK_1.Count > 0)
            {
                m_IK_1.Target = m_Marker.position;
                m_IK_1.Execute(m_Weight);
            }
        }

        #endregion "unity event handlers"

        #region "public method"
        // public method

        /// <summary>
        /// IK weight
        /// </summary>
        public float Weight
        {
            get { return m_Weight; }
        }

        /// <summary>
        /// set a new EndEffector
        /// </summary>
        public void SetEndEffector(Transform tr)
        {
            var mb = tr.GetComponent<CCDSolverMB>();
            Dbg.Assert(mb != null, "MarkerCtrl.SetEndEffector: failed to get CCDSolverMB: {0}", tr.name);
            m_IK_0 = mb.GetSolver();
            m_IK_0.SetBones(tr, 2);
            m_Collider.position = m_IK_0.GetJoints()[0].position;
            m_Marker.position = m_Collider.position;
        }

        /// <summary>
        /// set EndEffector for the second IK solver,
        /// used to demonstrate the multiple IK control function,
        /// </summary>
        public void SetSecondEndEffector(Transform tr)
        {
            if (tr == null)
            {
                m_IK_1 = null;
            }
            else
            {
                var mb = tr.GetComponent<CCDSolverMB>();
                Dbg.Assert(mb != null, "MarkerCtrl.SetSecondEndEffector: failed to get CCDSolverMB: {0}", tr.name);
                m_IK_1 = mb.GetSolver();
                m_IK_1.SetBones(tr, 2);
            }
        }

        #endregion "public method"

        #region "private method"
        // private method

        #endregion "private method"

        #region "constant data"
        // constant data

        public const string COLLIDER_LAYER = "Water";

        #endregion "constant data"
    }
}

