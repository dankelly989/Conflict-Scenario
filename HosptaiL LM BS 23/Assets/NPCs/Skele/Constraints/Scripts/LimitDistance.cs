using System;
using System.Collections.Generic;
using UnityEngine;
using ExtMethods;

namespace MH.Constraints
{
    public class LimitDistance : BaseConstraint
    {
        #region "configurable data"

        [SerializeField][Tooltip("the target transform")]
        private Transform m_target;
        [SerializeField][Tooltip("the distance")]
        private float m_distance;
        [SerializeField][Tooltip("inside/outside/onSurface?")]
        private EClampRegion m_clampRegion = EClampRegion.Outside;
        [SerializeField][Tooltip("the space target is evaluated in")]
        private ESpace m_targetSpace = ESpace.World;
        [SerializeField][Tooltip("the space owner is evaluated in")]
        private ESpace m_ownerSpace = ESpace.World;
        [SerializeField][Tooltip("the weight of constraints")]
        private float m_influence = 1f;

        #endregion "configurable data"

        #region "data"
        // data

        private Vector3 m_prevDiff = Vector3.forward;

        #endregion "data"

        #region "unity event handlers"

        #endregion "unity event handlers"

        #region "props"
        public UnityEngine.Transform Target
        {
            get { return m_target; }
            set { m_target = value; }
        }

        public float Distance
        {
            get { return m_distance; }
            set { m_distance = value; }
        }

        public EClampRegion ClampRegion
        {
            get { return m_clampRegion; }
            set { m_clampRegion = value; }
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

            Vector3 selfPos = m_tr.GetPosition(m_ownerSpace);
            Vector3 endPos = selfPos;
            Vector3 targetPos = m_target.GetPosition(m_targetSpace);

            Vector3 diff = selfPos - targetPos;
            float dist = diff.magnitude;

            switch (m_clampRegion)
            {
                case EClampRegion.Inside:
                    {
                        if (dist > m_distance)
                        {
                            diff = Vector3.ClampMagnitude(diff, m_distance);
                            endPos = targetPos + diff;
                        }
                    }
                    break;
                case EClampRegion.OnSurface:
                    {
                        if( !Mathf.Approximately(dist, m_distance) )
                        {
                            diff = diff.normalized * m_distance;
                            endPos = targetPos + diff;
                        }
                    }
                    break;
                case EClampRegion.Outside:
                    {
                        if( dist < m_distance )
                        {
                            Vector3 dn = diff.normalized;
                            if (dn == Vector3.zero) //if is zero vector, use forward
                                dn = m_prevDiff.normalized;
                            diff = dn * m_distance;
                            endPos = targetPos + diff;
                        }
                    }
                    break;
                default:
                    Dbg.LogErr("LimitDistance.DoUpdate: unexpected clampRegion: {0}", m_clampRegion);
                    break;
            }

            m_prevDiff = diff;

            if (!Mathf.Approximately(m_influence, 1f))
            {
                endPos = Misc.Lerp(selfPos, endPos, m_influence);
            }

            //Dbg.Log("selfPos: {0:F2}, endPos: {1:F2}", selfPos, endPos);
            m_tr.SetPosition(endPos, m_ownerSpace);
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
