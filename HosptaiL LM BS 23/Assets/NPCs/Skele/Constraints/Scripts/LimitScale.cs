using System;
using System.Collections.Generic;
using UnityEngine;
using ExtMethods;

namespace MH.Constraints
{
    public class LimitScale : BaseConstraint
    {
        #region "configurable data"

        [SerializeField][Tooltip("Limit what fields")]
        private ELimitAffect m_eLimitAffect = ELimitAffect.None;
        [SerializeField][Tooltip("the space owner is evaluated in?")]
        private ESpace m_ownerSpace = ESpace.World;
        [SerializeField][Tooltip("the min limits")]
        private Vector3 m_limitMin = Vector3.one;
        [SerializeField][Tooltip("the max limits")]
        private Vector3 m_limitMax = Vector3.one;
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

        public MH.Constraints.ELimitAffect LimitAffect
        {
            get { return m_eLimitAffect; }
            set { m_eLimitAffect = value; }
        }

        public bool ModifyInternalData
        {
            get { return m_modifyInitInfo; }
            set { m_modifyInitInfo = value; }
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

        public override bool HasGizmos
        {
            get
            {
                return false;
            }
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

            Vector3 selfScale = m_tr.GetScale(m_ownerSpace);
            Vector3 endScale = selfScale;

            // apply effect
            if ((m_eLimitAffect & ELimitAffect.MinX) != 0)
            {
                endScale.x = Mathf.Max(m_limitMin.x, endScale.x);
            }
            if ((m_eLimitAffect & ELimitAffect.MaxX) != 0)
            {
                endScale.x = Mathf.Min(m_limitMax.x, endScale.x);
            }
            if ((m_eLimitAffect & ELimitAffect.MinY) != 0)
            {
                endScale.y = Mathf.Max(m_limitMin.y, endScale.y);
            }
            if ((m_eLimitAffect & ELimitAffect.MaxY) != 0)
            {
                endScale.y = Mathf.Min(m_limitMax.y, endScale.y);
            }
            if ((m_eLimitAffect & ELimitAffect.MinZ) != 0)
            {
                endScale.z = Mathf.Max(m_limitMin.z, endScale.z);
            }
            if ((m_eLimitAffect & ELimitAffect.MaxZ) != 0)
            {
                endScale.z = Mathf.Min(m_limitMax.z, endScale.z);
            }

            if (!Mathf.Approximately(m_influence, 1f))
            {
                endScale = Misc.Lerp(selfScale, endScale, m_influence);
            }

            m_tr.SetScale(endScale, m_ownerSpace);
            if (m_modifyInitInfo)
                m_cstack.SetInitLocScale(m_tr.localScale);
        }

        //public override void DoDrawGizmos()
        //{
        //    base.DoDrawGizmos();
        //}

        #endregion "public method"

        #region "private method"
        // private method

        #endregion "private method"

        #region "constant data"
        // constant data



        #endregion "constant data"
    }
}
