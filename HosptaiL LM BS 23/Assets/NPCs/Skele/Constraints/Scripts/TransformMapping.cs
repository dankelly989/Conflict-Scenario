using System;
using System.Collections.Generic;
using UnityEngine;
using ExtMethods;

namespace MH.Constraints
{
    public class TransformMapping : BaseConstraint
    {
        #region "configurable data"

        [SerializeField][Tooltip("the target transform")]
        private Transform m_target;
        [SerializeField][Tooltip("the source data")]
        private ETransformData m_srcDataType = ETransformData.Position;
        [SerializeField][Tooltip("the destination data")]
        private ETransformData m_dstDataType = ETransformData.Position;
        [SerializeField][Tooltip("specify how to map axis")]
        private EAxis[] m_mapping = new EAxis[] { EAxis.X, EAxis.Y, EAxis.Z };
        [SerializeField][Tooltip("whether should do extrapolate")]
        private bool m_extrapolate = false;

        [SerializeField][Tooltip("source mapping data: from")]
        private Vector3 m_srcFrom = Vector3.zero;
        [SerializeField][Tooltip("source mapping data: to")]
        private Vector3 m_srcTo = Vector3.one;
        [SerializeField][Tooltip("destination mapping data")]
        private Vector3 m_dstFrom = Vector3.zero;
        [SerializeField][Tooltip("destination mapping data: to")]
        private Vector3 m_dstTo = Vector3.one;

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

        public MH.Constraints.ETransformData SrcDataType
        {
            get { return m_srcDataType; }
            set { m_srcDataType = value; }
        }
        public MH.Constraints.ETransformData DstDataType
        {
            get { return m_dstDataType; }
            set { m_dstDataType = value; }
        }
        public EAxis[] Mapping
        {
            get { return m_mapping; }
            set { m_mapping = value; }
        }
        public bool Extrapolate
        {
            get { return m_extrapolate; }
            set { m_extrapolate = value; }
        }
        public UnityEngine.Vector3 SrcFrom
        {
            get { return m_srcFrom; }
            set { m_srcFrom = value; }
        }
        public UnityEngine.Vector3 SrcTo
        {
            get { return m_srcTo; }
            set { m_srcTo = value; }
        }
        public UnityEngine.Vector3 DstFrom
        {
            get { return m_dstFrom; }
            set { m_dstFrom = value; }
        }
        public UnityEngine.Vector3 DstTo
        {
            get { return m_dstTo; }
            set { m_dstTo = value; }
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

        public override bool HasGizmos
        {
            get{ return false; }
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

            Vector3 srcData = _GetSourceData();
            Vector3 dstData = _GetDestData();

            _DoMapping(srcData, ref dstData);

            if (!Mathf.Approximately(m_influence, 1f))
            {
                dstData = Misc.Lerp(srcData, dstData, m_influence);
            }

            _SetDestinationData(dstData);
        }

        //public override void DoDrawGizmos()
        //{
        //    base.DoDrawGizmos();
        //}

        #endregion "public method"

        #region "private method"

        private void _SetDestinationData(Vector3 dstData)
        {
            switch (m_dstDataType)
            {
                case ETransformData.Position: m_tr.SetPosition(dstData, m_ownerSpace); break;
                case ETransformData.Rotation: m_tr.SetEuler(dstData, m_ownerSpace); break;
                case ETransformData.Scale: m_tr.SetScale(dstData, m_ownerSpace); break;
                default: Dbg.LogErr("TransformMapping._SetDestinationData: unexpected dstDataType: {0}", m_dstDataType); break;
            }
        }

        private void _DoMapping(Vector3 srcData, ref Vector3 dstData)
        {
            for (int i = 0; i < 3; ++i)
            {
                EAxis fromMap = m_mapping[i];
                if (fromMap == EAxis.None)
                    continue;

                float t = 0;
                switch (fromMap)
                {
                    case EAxis.X: t = Misc.InverseLerp(m_srcFrom.x, m_srcTo.x, srcData.x); break;
                    case EAxis.Y: t = Misc.InverseLerp(m_srcFrom.y, m_srcTo.y, srcData.y); break;
                    case EAxis.Z: t = Misc.InverseLerp(m_srcFrom.z, m_srcTo.z, srcData.z); break;
                    default: Dbg.LogErr("TransformMapping._DoMapping: unexpected fromMap: {0}", fromMap); break;
                }

                if (!m_extrapolate)
                    t = Mathf.Clamp01(t);

                switch (i)
                {
                    case 0: dstData.x = Misc.Lerp(m_dstFrom.x, m_dstTo.x, t); break;
                    case 1: dstData.y = Misc.Lerp(m_dstFrom.y, m_dstTo.y, t); break;
                    case 2: dstData.z = Misc.Lerp(m_dstFrom.z, m_dstTo.z, t); break;
                }
            }
        }

        private Vector3 _GetSourceData()
        {
            switch (m_srcDataType)
            {
                case ETransformData.Position: return m_target.GetPosition(m_targetSpace);
                case ETransformData.Rotation: return m_target.GetEuler(m_targetSpace);
                case ETransformData.Scale: return m_target.GetScale(m_targetSpace);
                default: Dbg.LogErr("TransformMapping._GetSourceData: unexpected srcDataType: {0}", m_srcDataType); return Vector3.zero;
            }
        }

        private Vector3 _GetDestData()
        {
            switch (m_dstDataType)
            {
                case ETransformData.Position: return m_tr.GetPosition(m_ownerSpace);
                case ETransformData.Rotation: return m_tr.GetEuler(m_ownerSpace);
                case ETransformData.Scale: return m_tr.GetScale(m_ownerSpace);
                default: Dbg.LogErr("TransformMapping._GetDestData: unexpected DstDataType: {0}", m_dstDataType); return Vector3.zero;
            }
        }

        // private method

        #endregion "private method"

        #region "constant data"
        // constant data

        #endregion "constant data"
    }
}
