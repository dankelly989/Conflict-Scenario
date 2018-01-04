using System;
using System.Collections.Generic;
using UnityEngine;

namespace MH
{
    public class CCDemo3_EnemyProp : MonoBehaviour
    {
        #region "configurable data"
        // configurable data

        public float m_HP = 100f;
        public float m_AtkPwr = 35f;
        public float m_RotateSpeed = 2f;
        public float m_AnimatorSpeed = 1.0f;
        public float m_timeUntilNextThink = 0f;

        #endregion "configurable data"

        #region "data"
        // data

        #endregion "data"

        #region "unity event handlers"
        // unity event handlers

        #endregion "unity event handlers"

        #region "public method"
        // public method

        public float HP
        {
            get { return m_HP; }
            set { m_HP = value; }
        }

        public float AtkPwr
        {
            get { return m_AtkPwr; }
            set { m_AtkPwr = value; }
        }

        public float RotateSpeed
        {
            get { return m_RotateSpeed; }
            set { m_RotateSpeed = value; }
        }

        public float AnimatorSpeed
        {
            get { return m_AnimatorSpeed; }
            set { m_AnimatorSpeed = value; }
        }

        public float TimeUntilNextThink
        {
            get { return m_timeUntilNextThink; }
            set { m_timeUntilNextThink = value; }
        }

        #endregion "public method"

        #region "private method"
        // private method

        #endregion "private method"

        #region "constant data"
        // constant data

        #endregion "constant data"
    }
}