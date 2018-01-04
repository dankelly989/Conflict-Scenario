using System;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;

namespace MH.Constraints
{
    /// <summary>
    /// used to wrap the owner's origin on to the MeshRenderer's mesh surface, provide methods:
    /// 1. project
    /// 2. nearest vertex
    /// </summary>
    public class ShrinkWrap : BaseConstraint
    {
        #region "configurable data"

        
        [SerializeField][Tooltip("the target's transform")]
        private Transform m_target;
        [SerializeField][Tooltip("")]
        private MeshFilter m_targetMF;
        [SerializeField][Tooltip("")]
        private Collider m_targetCol;
        [SerializeField][Tooltip("could be positive/negative, on the line of owner's origin to the surface point")]
        private float m_distance = 0;
        [SerializeField][Tooltip("the algorithm used to execute shrinkwrap")]
        private EShrinkWrapMethod m_method = EShrinkWrapMethod.Project;
        //[SerializeField][Tooltip("whether overwrite initInfo")]
        //private bool m_modifyInitInfo = false;

        [SerializeField][Tooltip("the weight of constraints")]
        private float m_influence = 1f;

        // Project method
        [SerializeField][Tooltip("")]
        private EAxisD m_projectDir = EAxisD.InvY; //from the target towards owner
        [SerializeField][Tooltip("")]
        private ESpace m_ownerSpace = ESpace.World; //used to decide the projectDir;
        [SerializeField][Tooltip("only project if the distance is less than this value, 0 means no limit")]
        private float m_maxProjectDistance = 0;
        // Nearest-vertex method
        [HideInInspector][SerializeField][Tooltip("the KDtree used to find out the Nearest vertex from mesh")]
        private KDTree m_kdTree = null; // will be handled by ISerializationCallbackReceiver of KDTree



        #endregion "configurable data"

        #region "data"
        // data

        private Vector3 m_initPos;

        #endregion "data"

        #region "unity event handlers"

        #endregion "unity event handlers"

        #region "props"
        public UnityEngine.Transform Target
        {
            get { return m_target; }
            set {
                if (m_target == value)
                    return;
                else if (value == null)
                {
                    m_target = null;
                    m_targetMF = null;
                    m_targetCol = null;
                    m_kdTree.Invalidate(); //clear kdtree
                    return;
                }

                var mf = value.GetComponent<MeshFilter>();
                var col = value.GetComponent<Collider>();

                if (mf != null && col != null)
                {
                    m_target = value;
                    m_targetMF = mf;
                    m_targetCol = col;
                    m_kdTree.Invalidate(); //clear kdtree
                }
            }
        }
        public float Distance
        {
            get { return m_distance; }
            set { m_distance = value; }
        }
        public ShrinkWrap.EShrinkWrapMethod Method
        {
            get { return m_method; }
            set {
                if (m_method != value)
                {
                    m_method = value; 
                }
            }
        }
        //public bool ModifyInitInfo
        //{
        //    get { return m_modifyInitInfo; }
        //    set { m_modifyInitInfo = value; }
        //}
        public override float Influence
        {
            get { return m_influence; }
            set { m_influence = value; }
        }
        public MH.EAxisD ProjectDir
        {
            get { return m_projectDir; }
            set { m_projectDir = value; }
        }
        public MH.ESpace OwnerSpace
        {
            get { return m_ownerSpace; }
            set { m_ownerSpace = value; }
        }
        public float MaxProjectDistance
        {
            get { return m_maxProjectDistance; }
            set { m_maxProjectDistance = value; }
        }
        #endregion "props"

        #region "public method"
        // public method

        public override void DoAwake()
        {
            base.DoAwake();
            m_initPos = m_tr.position;
        }

        public override void DoUpdate()
        {
            base.DoUpdate();

            if (!m_target)
                return; //do nothing if no target is specified
            if (m_method == EShrinkWrapMethod.Project && !m_targetCol)
                return;
            if (m_method == EShrinkWrapMethod.NearestVertex && !m_targetMF)
                return;

            Vector3 initPos = m_initPos = m_tr.position;
            Vector3 endPos = initPos;
            //Vector3 targetPos = m_target.position; 

            if (m_method == EShrinkWrapMethod.Project)
            {
                Ray ray = _GetProjectRay();
                RaycastHit hit;
                if (m_targetCol.Raycast(ray, out hit, float.MaxValue))
                {
                    Vector3 hitPt = hit.point;
                    if (m_maxProjectDistance <= 0 || 
                        (m_maxProjectDistance > 0 && (hitPt - initPos).sqrMagnitude <= m_maxProjectDistance * m_maxProjectDistance)
                        )
                    {
                        endPos = hitPt;

                        if( m_distance > 0 )
                            endPos += m_distance * (initPos - endPos).normalized;
                    }
                }
            }
            else if (m_method == EShrinkWrapMethod.NearestVertex)
            {
                _EnsureKDTree(); //if no KD-tree created, create here
                Vector3 invTrPos = m_target.InverseTransformPoint(m_tr.position);
                endPos = m_kdTree.GetNearest(invTrPos); //NOTE: in target's local coord
                endPos = m_target.TransformPoint(endPos); //transform back to world coord

                if (m_distance > 0)
                    endPos += m_distance * (initPos - endPos).normalized;
            }

            if (!Mathf.Approximately(m_influence, 1f))
            {
                endPos = Misc.Lerp(initPos, endPos, m_influence);
            }

            m_tr.position = endPos;

            //if (m_modifyInitInfo)
            //    m_cstack.SetInitLocPos(endPos);
        }

        private void _EnsureKDTree()
        {
            if (!m_kdTree.IsValid)
            {
                m_kdTree.Build(m_targetMF.sharedMesh);
            }
        }

        private Ray _GetProjectRay()
        {
            Vector3 baseDir = Vector3.zero;
            switch (m_projectDir)
            { 
                case EAxisD.X: baseDir = Vector3.right; break;
                case EAxisD.Y: baseDir = Vector3.up; break;
                case EAxisD.Z: baseDir = Vector3.forward; break;
                case EAxisD.InvX: baseDir = Vector3.left; break;
                case EAxisD.InvY: baseDir = Vector3.down; break;
                case EAxisD.InvZ: baseDir = Vector3.back; break;
                default: Dbg.LogErr("ShrinkWrap._GetProjectRay: unexpected projectDir: {0}", m_projectDir); break;
            }

            Vector3 dir = baseDir;
            switch (m_ownerSpace)
            {
                case ESpace.Self: dir = m_tr.TransformDirection(baseDir); break;
                case ESpace.World: break; //do nothing
            }

            Ray r = new Ray(m_tr.position, dir);
            return r;
        }

        public override void DoDrawGizmos()
        {
            base.DoDrawGizmos();

            if (m_target)
            {
                var oldC = Gizmos.color;
                Gizmos.color = ConUtil.GizmosColor;
                Gizmos.DrawLine(m_tr.position, m_target.position);
                Gizmos.DrawSphere(m_initPos, 0.1f);
                Gizmos.color = oldC;
            }
        }

        #endregion "public method"

        #region "private method"
        // private method

        #endregion "private method"

        #region "constant data"
        // constant data

        public enum EShrinkWrapMethod
        {
            Project,
            NearestVertex,
        }

        #endregion "constant data"
    }
}
