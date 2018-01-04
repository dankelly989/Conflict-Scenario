using System;
using System.Collections.Generic;
using UnityEngine;
using ExtMethods;

namespace MH.Constraints
{
    public class CopyTransform : BaseConstraint
    {
        #region "configurable data"

        [SerializeField][Tooltip("the target transform")]
        private Transform m_target;
        [SerializeField][Tooltip("the space target is evaluated in")]
        private ESpace m_targetSpace = ESpace.World;
        [SerializeField][Tooltip("the space owner is evaluated in")]
        private ESpace m_ownerSpace = ESpace.World;
        [SerializeField][Tooltip("the weight of constraints")]
        private float m_influence = 1f;

        #endregion "configurable data"

        #region "data"
        // data

        #endregion "data"

        #region "unity event handlers"

        #endregion "unity event handlers"

        #region "props"
        public UnityEngine.Transform Target
        {
            get { return m_target; }
            set { m_target = value; }
        }
        public MH.ESpace TargetSpace
        {
            get { return m_targetSpace; }
            set { m_targetSpace = value; }
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

            if (!m_target)
                return; //do nothing if no target is specified

            Vector3 initPos = m_tr.GetPosition(m_ownerSpace);
            Vector3 targetPos = m_target.GetPosition(m_targetSpace);
            Vector3 endPos = targetPos;

            Vector3 initEuler = m_tr.GetEuler(m_ownerSpace);
            Vector3 targetEuler = m_target.GetEuler(m_targetSpace);
            Vector3 endEuler = targetEuler;

            Vector3 initScale = m_tr.GetScale(m_ownerSpace);
            Vector3 targetScale = m_target.GetScale(m_targetSpace);
            Vector3 endScale = targetScale;
            
            if (!Mathf.Approximately(m_influence, 1f))
            {
                endPos = Misc.Lerp(initPos, endPos, m_influence);
                endEuler = Misc.EulerSlerp(initEuler, targetEuler, m_influence);
                endScale = Misc.Lerp(initScale, targetScale, m_influence);
            }

            m_tr.SetPosition(endPos, m_ownerSpace);
            m_tr.SetEuler(endEuler, m_ownerSpace);
            m_tr.SetScale(endScale, m_ownerSpace);
        }

        public override void DoDrawGizmos()
        {
            base.DoDrawGizmos();

            if (m_target)
            {
                _DrawLine(m_tr, m_target);
            }
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
