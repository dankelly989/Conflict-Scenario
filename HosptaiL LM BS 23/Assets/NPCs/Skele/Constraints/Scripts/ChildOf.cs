using System;
using System.Collections.Generic;
using UnityEngine;
using ExtMethods;

namespace MH.Constraints
{
    /// <summary>
    /// make the owner move like a child transform of target object
    /// </summary>
    public class ChildOf : BaseConstraint
    {
        #region "configurable data"

        [SerializeField][Tooltip("the target transform")]
        private Transform m_target;
        [SerializeField][Tooltip("position")]
        private EAxis m_affectPos = EAxis.X | EAxis.Y | EAxis.Z;
        [SerializeField][Tooltip("rotation")]
        private EAxis m_affectRot = EAxis.X | EAxis.Y | EAxis.Z;
        [SerializeField][Tooltip("scale")]
        private EAxis m_affectSca = EAxis.X | EAxis.Y | EAxis.Z;
        [SerializeField][Tooltip("how to do the calculation")]
        private EChildOfMode m_workMode = EChildOfMode.FixedInitInfo;
        
        [SerializeField][Tooltip("the weight of constraints")]
        private float m_influence = 1f;

        [SerializeField][Tooltip("")]
        private XformData m_pseudoLocTr = new XformData();

        #endregion "configurable data"

        #region "data"

        #endregion "data"

        #region "unity event handlers"

        #endregion "unity event handlers"

        #region "props"

        public UnityEngine.Transform Target
        {
            get { return m_target; }
            set {
                if( m_target != value )
                    _OnChangeTarget(value);
            }
        }

        public EAxis AffectPos
        {
            get { return m_affectPos; }
            set { m_affectPos = value; }
        }
        public EAxis AffectRot
        {
            get { return m_affectRot; }
            set { m_affectRot = value; }
        }
        public EAxis AffectSca
        {
            get { return m_affectSca; }
            set { m_affectSca = value; }
        }
        public EChildOfMode WorkMode
        {
            get { return m_workMode; }
            set { m_workMode = value; }
        }
        public XformData PseudoLocTr
        {
            get { return m_pseudoLocTr; }
            set { m_pseudoLocTr = value; }
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

            Vector3 startPos = m_tr.position;
            Vector3 startEul = m_tr.eulerAngles;
            Vector3 startSca = m_tr.lossyScale;
            Vector3 endPos = startPos;
            Vector3 endEul = startEul;
            Vector3 endSca = startSca;

            switch (m_workMode)
            {
                case EChildOfMode.FixedInitInfo: _Update_Fixed(ref endPos, ref endEul, ref endSca); break;
                case EChildOfMode.UpdatedInitInfo: _Update_Updated(ref endPos, ref endEul, ref endSca); break;
                default: Dbg.LogErr("ChildOf.DoUpdate: unexpected workMode: {0}", m_workMode); break;
            }

            //pos
            if ((m_affectPos & EAxis.X) == 0) endPos.x = startPos.x;
            if ((m_affectPos & EAxis.Y) == 0) endPos.y = startPos.y;
            if ((m_affectPos & EAxis.Z) == 0) endPos.z = startPos.z;
            //rot
            if ((m_affectRot & EAxis.X) == 0) endEul.x = startEul.x;
            if ((m_affectRot & EAxis.Y) == 0) endEul.y = startEul.y;
            if ((m_affectRot & EAxis.Z) == 0) endEul.z = startEul.z;
            //scale
            if ((m_affectSca & EAxis.X) == 0) endSca.x = startSca.x;
            if ((m_affectSca & EAxis.Y) == 0) endSca.y = startSca.y;
            if ((m_affectSca & EAxis.Z) == 0) endSca.z = startSca.z;

            if (!Mathf.Approximately(m_influence, 1f))
            {
                endPos = Misc.Lerp(startPos, endPos, m_influence);
                endEul = Misc.EulerSlerp(startEul, endEul, m_influence);
                endSca = Misc.Lerp(startSca, endSca, m_influence);
            }

            m_tr.position = endPos;
            m_tr.eulerAngles = endEul;
            m_tr.SetScale(endSca, ESpace.World);
        }

        private void _Update_Updated(ref Vector3 endPos, ref Vector3 endEul, ref Vector3 endSca)
        {
            throw new NotImplementedException();
        }

        private void _Update_Fixed(ref Vector3 endPos, ref Vector3 endEul, ref Vector3 endSca)
        {
            endPos = m_target.TransformPoint(m_pseudoLocTr.pos);
            Quaternion endRot = m_target.rotation * m_pseudoLocTr.rot;
            endEul = endRot.eulerAngles;
            endSca = Vector3.Scale(m_target.lossyScale, m_pseudoLocTr.scale);
        }

        public override void DoDrawGizmos()
        {
            base.DoDrawGizmos();

            if (m_target)
            {
                _DrawLine(m_tr, m_target);
            }
        }

        public void RecalcPseudoLocalTransformData()
        {
            _RecordInitInfo();
        }

        #endregion "public method"

        #region "private method"

        private void _OnChangeTarget(Transform newTr)
        {
            m_target = newTr;
            if (!m_target) return;

            _RecordInitInfo();
        }

        private void _RecordInitInfo()
        {
            Vector3 selfWorldPos = m_tr.position;
            Quaternion selfWorldRot = m_tr.rotation;
            Vector3 selfWorldSca = m_tr.lossyScale;

            Vector3 newLocPos = m_target.InverseTransformPoint(selfWorldPos);
            Quaternion newLocRot = Quaternion.Inverse(m_target.rotation) * selfWorldRot;
            Vector3 newLocSca = V3Ext.DivideComp(selfWorldSca, m_target.lossyScale);

            m_pseudoLocTr.pos = newLocPos;
            m_pseudoLocTr.rot = newLocRot;
            m_pseudoLocTr.scale = newLocSca;
        }

        #endregion "private method"

        #region "constant data"
        // constant data

        public enum EChildOfMode
        {
            //record a xformData when target is set/changed, and use the xform data to calculate 
            //(and the data is not updated later, so the owner is moved by target only)
            FixedInitInfo, 
            //the xformData is recorded when target is set/changed, and is updated incrementally every iteration.
            UpdatedInitInfo,
        }


        #endregion "constant data"
    }
}
