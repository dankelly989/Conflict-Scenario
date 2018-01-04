using System;
using System.Collections.Generic;
using UnityEngine;
using ExtMethods;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MH.Constraints
{
    /// <summary>
    /// the container for CCDSolver,
    /// 
    /// this MB should be put on the endJoint gameObject, so it can handle different settings under a single tree-structure
    /// </summary>
    public class CCDSolverMB : BaseSolverMB
    {
        #region "configurable data"
        // configurable data

        [SerializeField][Tooltip("the target object")]
        private Transform m_target;

        [SerializeField][Tooltip("[0] is root, [last] is endJoint, if empty then will use boneCount to auto-collect joints")]
        private List<Transform> m_jointList = new List<Transform>();
        [SerializeField][Tooltip("when the endJoint and target are within this distance, the calc is taken as success")]
        private float m_distThreshold = 0.00001f;
        [SerializeField][Tooltip("damp limits the rotate delta in one iteration")]
        private bool m_setUseDamp = true;
        [SerializeField][Tooltip("global damp limit for joints under this solver")]
        private float m_setGlobalDamp = 10f;
        [SerializeField][Tooltip("if target is specified, then apply the rotation of target on endJoint ")]
        private bool m_useTargetRotation = false;
        [SerializeField][Tooltip("the max iteration count")]
        private int m_maxIteration = 20;

        [SerializeField][Tooltip("how to recover if the IK cannot reach reasonable solution to given target")]
        private CCDSolver.RevertOption m_revertOpt = CCDSolver.DEF_RevertOpt;


        [SerializeField][Tooltip("the weight of constraints")]
        private float m_influence = 1f;

        //////////////////////////////////////////////////////////
        //need to prepare for revert when deactivate constraint, record data like
        [SerializeField][Tooltip("TrInitInfo for ANCESTOR bones on the chain, exclude this tr")]
        private List<TrInitInfo> m_initInfos = new List<TrInitInfo>();


        #endregion "configurable data"

        #region "data"
        // data

        private CCDSolver m_solver;

        #endregion "data"

        #region "unity event handlers"

        public override void DoAwake()
        {
            base.DoAwake();
            m_cstack.ExecOrder = Mathf.Max(m_cstack.ExecOrder, 100);

            if (!Application.isPlaying)
            {
                if (m_jointList.Count == 0)
                    _SetJointListByBoneCount(DEF_BoneCount);
            }

            var solver = GetSolver();
            solver.RefreshConstraint();
        }

        public override void DoUpdate()
        {
            base.DoUpdate();

            //_LogTrData("at start");
            //for (int i = 0; i < m_initInfos.Count; ++i) //comment out to fix the IK-jitter 
            //    m_initInfos[i].UpdateInitInfo();
            //for (int i = 0; i < m_initInfos.Count; ++i)
            //    m_initInfos[i].RevertToInitInfo();

            CCDSolver solver = GetSolver();

            if (m_target && m_influence != 0)
            {
                //Vector3 initPos = m_tr.position; 
                Vector3 targetPos = m_target.position;

                //_LogTrData("before execute");
                solver.Target = targetPos;
                solver.Execute(m_influence);
                //_LogTrData("After execute");

                if (m_useTargetRotation)
                    m_tr.rotation = Quaternion.Lerp(m_tr.rotation, m_target.rotation, m_influence);
            }

            //for (int i = 0; i < m_initInfos.Count; ++i)
            //    m_initInfos[i].RecordLastLocInfo();

            //~TODO?: find ConstraintStack on parent bones, and call RecordLastLocInfo on each one
        }

        private System.Text.StringBuilder _sb = new System.Text.StringBuilder();
        private void _LogTrData(string prefix)
        {
            _sb.Remove(0, _sb.Length);
            for(int i=0; i<m_initInfos.Count; ++i)
            {
                _sb.AppendFormat("{0}: {1} | ", m_initInfos[i].tr, m_initInfos[i].tr.localEulerAngles.ToString("F0"));
            }
            Dbg.Log("{0} : {1}", prefix, _sb.ToString());
        }



        //void OnTransformParentChanged()
        //{
        //    if (m_jointList.Count == 0) //only work when not manually specified joints
        //    {
        //        int realLevel = 0;
        //        if (!m_tr.HasParentLevel(m_boneCount, out realLevel))
        //            boneCount = realLevel;
        //        GetSolver(true); //force update solver;
        //    }
        //}

        #endregion "unity event handlers"

        #region "public method"

        public List<Transform> jointList
        {
            get { return m_jointList; }
        }

        /// <summary>
        /// access bone count,
        /// when change, if decrease, revert those taken out bones; if increase, add new TrInitInfo and call ResetInitInfo
        /// </summary>
        public int boneCount
        {
            get { return Mathf.Max(0, m_solver.Count); }
            //set {
            //    int actualLevel = value;
            //    if (m_boneCount != value && Tr.HasParentLevel(value, out actualLevel))
            //    { //ensure there're enough parents up ahead

            //        if (m_jointList.Count == 0)
            //            _RenewSolverOnChangeBoneCount(value);
            //    }
            //}
        }

        public Transform Tr
        {
            get {
                if (m_tr == null)
                    m_tr = transform;
                return m_tr;
            }
        }

        public override IKSolverType solverType
        {
            get { return IKSolverType.CCD; }
        }

        public UnityEngine.Transform Target
        {
            get {
                return m_target;
            }
            set {
                m_target = value;
            }
        }

        public float distThres
        {
            get {
                if (m_solver != null)
                {
                    m_distThreshold = m_solver.distThres;
                }
                return m_distThreshold;
            }
            set {
                m_distThreshold = value;
                if (m_solver != null)
                {
                    m_solver.distThres = m_distThreshold;
                }
            }
        }

        public bool useDamp
        {
            get {
                if (m_solver != null)
                {
                    m_setUseDamp = m_solver.useDamp;
                }
                return m_setUseDamp;
            }
            set {
                m_setUseDamp = value;
                if (m_solver != null)
                {
                    m_solver.useDamp = m_setUseDamp;
                }
            }
        }

        public float globalDamp
        {
            get {
                if (m_solver != null)
                {
                    m_setGlobalDamp = m_solver.globalDamp;
                }
                return m_setGlobalDamp;
            }
            set {
                m_setGlobalDamp = value;
                if (m_solver != null)
                {
                    m_solver.globalDamp = m_setGlobalDamp;
                }
            }
        }

        public bool useTargetRotation
        {
            get { return m_useTargetRotation; }
            set { m_useTargetRotation = value; }
        }

        public int maxIteration
        {
            get { return m_maxIteration; }
            set { m_maxIteration = value; }
        }

        public CCDSolver.RevertOption revertOpt
        {
            get {
                if (m_solver != null)
                {
                    m_revertOpt = m_solver.revertOpt;
                }
                return m_revertOpt;
            }
            set {
                m_revertOpt = value;
                if (m_solver != null)
                {
                    m_solver.revertOpt = m_revertOpt;
                }
            }
        }
        public List<TrInitInfo> InitInfos
        {
            get { return m_initInfos; }
        }
        public override float Influence
        {
            get { return m_influence; }
            set { m_influence = value; }
        }


        public CCDSolver GetSolver(bool force = false)
        {
            if (force || m_solver == null)
            {
                m_solver = null; //clear first

                m_solver = new CCDSolver();
                _ResetSolver();
                _InitInitInfos();
            }

            return m_solver;
        }

        public override void OnConstraintActiveChanged()
        {
            base.OnConstraintActiveChanged();
            _RevertAllAncestorsInitInfos();
        }


        #endregion "public method"

        #region "private method"

        private void _ResetSolver()
        {
            if (m_solver != null)
            {
                m_solver.revertOpt = m_revertOpt;
                m_solver.distThres = m_distThreshold;
                m_solver.Target = Tr.position;
                m_solver.useDamp = m_setUseDamp;
                m_solver.globalDamp = m_setGlobalDamp;
                m_solver.maxIterTimes = m_maxIteration;

                _TidyMBJointList();
                m_solver.SetBones(m_jointList);
            }
        }

        /// <summary>
        /// ensure the m_jointList has no null entry
        /// </summary>
        private void _TidyMBJointList()
        {
            m_jointList.RemoveAll(x => x == null);
        }

        private void _InitInitInfos()
        {
            m_initInfos.Clear();

            Transform[] joints = m_solver.GetJoints();
            for (int i = joints.Length - 2; i >= 0; --i)
            {
                Transform tr = joints[i];
                TrInitInfo newInitInfo = new TrInitInfo(tr);
                newInitInfo.ResetInitInfo();
                m_initInfos.Add(newInitInfo);
            }

            //_InitSolver();
        }

        public void _RenewByCollectJointsWithBontCount(int boneCount)
        {
            // change joint list
            _SetJointListByBoneCount(boneCount);

            // renew
            _RenewInitInfoAndSolver();
        }

        private void _SetJointListByBoneCount(int boneCount)
        {
            // set m_jointList
            m_jointList.Clear();
            Transform tr = transform; //the end joint
            m_jointList.Add(tr);
            for (int i = 0; i < boneCount; ++i)
            {
                tr = tr.parent;
                if (tr == null)
                    break;
                m_jointList.Add(tr);
            }
            m_jointList.Reverse();
        }

        public void _RenewInitInfoAndSolver()
        {
            _ResetSolver();

            for (int i = m_initInfos.Count - 1; i >= 0; --i)
            {
                m_initInfos[i].RevertToInitInfo();
            }
            m_initInfos.Clear();

            for (int i = m_jointList.Count - 2; i >= 0; i--)
            {
                Transform tr = m_jointList[i];
                TrInitInfo newInitInfo = new TrInitInfo(tr);
                newInitInfo.ResetInitInfo();
                m_initInfos.Add(newInitInfo);
            }
        }

        private void _RevertAllAncestorsInitInfos()
        {
            for (int i = 0; i < m_initInfos.Count; ++i)
            {
                m_initInfos[i].RevertToInitInfo();
            }
        }

        #endregion "private method"

        #region "constant data"
        // constant data

        private const int DEF_BoneCount = 2;

        #endregion "constant data"

    }




}
