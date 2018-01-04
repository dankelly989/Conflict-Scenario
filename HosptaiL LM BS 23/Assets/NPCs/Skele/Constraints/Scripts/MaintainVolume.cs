using System;
using System.Collections.Generic;
using UnityEngine;
using ExtMethods;

namespace MH.Constraints
{
    public class MaintainVolume : BaseConstraint
    {
        #region "configurable data"

        [SerializeField][Tooltip("Limit what fields")]
        private EAxis m_eFreeAxis = EAxis.Y;
        [SerializeField][Tooltip("the space owner is evaluated in?")]
        private ESpace m_ownerSpace = ESpace.Self;
        [SerializeField][Tooltip("the multiplies for volume")]
        private float m_volumeMulti = 1f;
        //[SerializeField][Tooltip("write the result back to constraintStack's initInfo")]
        //private bool m_modifyInitInfo = false;
        [SerializeField][Tooltip("the weight of constraints")]
        private float m_influence = 1f;

        [SerializeField][Tooltip("the base volume")]
        private float m_baseVolume = float.NaN;
        //[SerializeField][Tooltip("the base scale")]
        //private Vector3 m_baseScale;
        #endregion "configurable data"

        #region "data"
        // data

        #endregion "data"

        #region "unity event handlers"

        #endregion "unity event handlers"

        #region "props"

        public EAxis FreeAxis
        {
            get { return m_eFreeAxis; }
            set { m_eFreeAxis = value; }
        }

        //public bool ModifyInternalData
        //{
        //    get { return m_modifyInitInfo; }
        //    set { m_modifyInitInfo = value; }
        //}

        public float BaseVolume
        {
            get { return m_baseVolume; }
            set { m_baseVolume = value; }
        }

        public float VolMul
        {
            get { return m_volumeMulti; }
            set { m_volumeMulti = value; }
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

            if (float.IsNaN(m_baseVolume))
            {
                Vector3 s /*= m_baseScale*/ = V3Ext.FixZeroComponent(m_tr.localScale);
                m_baseVolume = s.x * s.y * s.z;
            }
        }

        public override void DoRemove()
        {
            base.DoRemove();

            //m_tr.SetScale(m_baseScale, m_ownerSpace);
        }

        public override void DoUpdate()
        {
            base.DoUpdate();

            Vector3 selfScale = m_tr.GetScale(m_ownerSpace);
            selfScale = V3Ext.FixZeroComponent(selfScale); //ensure no 0 component
            Vector3 endScale = selfScale;
            float totalVolume = m_baseVolume * m_volumeMulti;

            // apply effect
            switch (m_eFreeAxis)
            {
                case EAxis.X:
                    {
                        float vol = Mathf.Abs(totalVolume / selfScale.x);
                        float sqrt = Mathf.Sqrt(vol);

                        endScale.y = endScale.z = sqrt;
                    } 
                    break;
                case EAxis.Y:
                    {
                        float vol = Mathf.Abs(totalVolume / selfScale.y);
                        float sqrt = Mathf.Sqrt(vol);

                        endScale.x = endScale.z = sqrt;
                    } 
                    break;
                case EAxis.Z: 
                    {
                        float vol = Mathf.Abs(totalVolume / selfScale.z);
                        float sqrt = Mathf.Sqrt(vol);

                        endScale.x = endScale.y = sqrt;
                    }
                    break;
            }

            // influence
            if (!Mathf.Approximately(m_influence, 1f))
            {
                endScale = Misc.Lerp(selfScale, endScale, m_influence);
            }

            m_tr.SetScale(endScale, m_ownerSpace);
            //if (m_modifyInitInfo)
            //    m_cstack.SetInitLocScale(m_tr.localScale);
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
