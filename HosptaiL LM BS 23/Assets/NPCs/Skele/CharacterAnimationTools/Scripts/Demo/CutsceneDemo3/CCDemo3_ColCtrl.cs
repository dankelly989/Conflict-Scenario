using System;
using System.Collections.Generic;
using UnityEngine;

namespace MH
{
    /// <summary>
    /// collider control, 
    /// disable collider when not in atk state
    /// but enable must wait for the anim event
    /// </summary>
    public class CCDemo3_ColCtrl : MonoBehaviour
    {
        #region "configurable data"
        // configurable data

        #endregion "configurable data"

        #region "data"
        // data

        private Animator m_Animator;
        private Collider m_LightPunchCollider;
        private Collider m_HeavyPunchCollider;
        private CCDemo3_MainCtrl m_MainCtrl;

        #endregion "data"

        #region "unity event handlers"
        // unity event handlers

        void Start()
        {
            m_MainCtrl = GameObject.Find("GlobalScript").GetComponent<CCDemo3_MainCtrl>();
            m_Animator = GetComponent<Animator>();

            Transform tr = transform;
            m_LightPunchCollider = tr.Find("Hips/Spine/Spine1/Spine2/LeftShoulder/LeftArm/LeftForeArm/LeftHand").GetComponent<Collider>();
            m_HeavyPunchCollider = tr.Find("Hips/Spine/Spine1/Spine2/RightShoulder/RightArm/RightForeArm/RightHand").GetComponent<Collider>();
        }

        void Update()
        {
            int curState = CCDemo3_Helper.GetAnimatorStateHash(m_Animator.GetCurrentAnimatorStateInfo(0));
            AnimatorStateInfo nextStateInfo = m_Animator.GetNextAnimatorStateInfo(0);
            int nextState = CCDemo3_Helper.GetAnimatorStateHash(nextStateInfo);

            if (curState != CCDemo3_MainCtrl.LIGHT_PUNCH_STATE
                && (nextState != CCDemo3_MainCtrl.LIGHT_PUNCH_STATE))
            {
                //if( m_LightPunchCollider.enabled )
                //{
                //    //Dbg.Log("disable LP {0}", Time.frameCount);
                //}
                m_LightPunchCollider.enabled = false;
            }

            if (curState != CCDemo3_MainCtrl.HEAVY_PUNCH_STATE &&
                (nextState != CCDemo3_MainCtrl.HEAVY_PUNCH_STATE))
            {
                m_HeavyPunchCollider.enabled = false;
            }
        }

        void Msg_EnableHeavyPunch()
        {
            m_HeavyPunchCollider.enabled = true;
        }

        void Msg_EnableLightPunch()
        {
            //Dbg.Log("enable LP {0}", Time.frameCount);
            m_LightPunchCollider.enabled = true;
        }

        void Msg_EnableParry()
        {
            m_MainCtrl.EnableParry = true;
        }

        void Msg_DisableParry()
        {
            m_MainCtrl.EnableParry = false;
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