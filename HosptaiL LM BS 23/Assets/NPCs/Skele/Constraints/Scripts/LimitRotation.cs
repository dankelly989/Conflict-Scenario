using System;
using System.Collections.Generic;
using UnityEngine;
using ExtMethods;

namespace MH.Constraints
{
    public class LimitRotation : BaseConstraint
    {
        #region "configurable data"

        [SerializeField][Tooltip("Limit what fields")]
        private ELimitEuler m_eLimitEuler = ELimitEuler.None;
        [SerializeField][Tooltip("the space owner is evaluated in?")]
        private ESpace m_ownerSpace = ESpace.Self;
        [SerializeField][Tooltip("the min limits")]
        private Vector3 m_limitMin = Vector3.zero;
        [SerializeField][Tooltip("the max limits")]
        private Vector3 m_limitMax = Vector3.zero;
        [SerializeField][Tooltip("write the result back to constraintStack's initInfo")]
        private bool m_modifyInitInfo = false;
        [SerializeField][Tooltip("the weight of constraints")]
        private float m_influence = 1f;

        #endregion "configurable data"

        #region "data"
        // data

        #endregion "data"

        #region "unity event handlers"

        #endregion "unity event handlers"

        #region "props"

        public override bool HasGizmos
        {
            get { return false; }
        }

        public bool ModifyInternalData
        {
            get { return m_modifyInitInfo; }
            set { m_modifyInitInfo = value; }
        }

        public ELimitEuler LimitEuler
        {
            get { return m_eLimitEuler; }
            set { m_eLimitEuler = value; }
        }

        public Vector3 LimitMin
        {
            get { return m_limitMin; }
            set { m_limitMin = value; }
        }

        public Vector3 LimitMax
        {
            get { return m_limitMax; }
            set { m_limitMax = value; }
        }
        public MH.ESpace OwnerSpace
        {
            get { return m_ownerSpace; }
            set { m_ownerSpace = value; }
        }

        public override float Influence
        {
            get { return m_influence; }
            set { m_influence = value; }
        }

        #endregion "props"

        #region "public method"
        // public method

        public override void DoAwake()
        {
            base.DoAwake();
        }

        public override void DoUpdate()
        {
            base.DoUpdate();

            Vector3 selfEuler = m_tr.GetEuler(m_ownerSpace);
            Vector3 endEuler = selfEuler;

            // apply effect
            if ((m_eLimitEuler & ELimitEuler.X) != 0)
            {
                endEuler.x = Mathf.Clamp(endEuler.x, m_limitMin.x, m_limitMax.x);
            }
            if ((m_eLimitEuler & ELimitEuler.Y) != 0)
            {
                endEuler.y = Mathf.Clamp(endEuler.y, m_limitMin.y, m_limitMax.y);
            }
            if ((m_eLimitEuler & ELimitEuler.Z) != 0)
            {
                endEuler.z = Mathf.Clamp(endEuler.z, m_limitMin.z, m_limitMax.z);
            }

            if (!Mathf.Approximately(m_influence, 1f))
            {
                endEuler = Misc.Lerp(selfEuler, endEuler, m_influence);
            }

            m_tr.SetEuler(endEuler, m_ownerSpace);
            if (m_modifyInitInfo)
                m_cstack.SetInitLocRot(m_tr.localEulerAngles);
        }

        public override void DoDrawGizmos()
        {
            base.DoDrawGizmos();
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
