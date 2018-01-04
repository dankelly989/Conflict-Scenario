using System;
using System.Collections.Generic;
using UnityEngine;
using ExtMethods;
using System.Collections;

using MH.IKConstraint;
using Random = UnityEngine.Random;

namespace MH
{

    using Joints = List<CCDSolver.JointData>;
    using System.Text;
    /// <summary>
    /// used CCD algorithm to solve IK
    /// </summary>
    public class CCDSolver : ISolver
    {
		#region "data"
	    // data

        private Joints m_joints; //the joints
        private Vector3 m_targetPos; //target position
        private Transform[] m_jointArr; //array of joints

        private bool m_useDamp = true;
        private float m_globalDamp = 10f; // degree
        private float m_distThres = 1e-5f; // the threshold for position error
        private int m_maxIter = 100;
        private float m_highDistThres = 100f; 
        private float m_highDistThresRatio = 0.2f; // multiple by the total length of bones, if endJoint too far from target, don't apply result
        private float m_totalBoneLength = 0; //all bones length sum up

        //used to store the start xform data before start Ik iterations, 
        //used to revert xforms when cannot reach the target within a reasonable distance
        private List<XformData> m_backupData = new List<XformData>();
        private RevertOption m_revertOpt = DEF_RevertOpt;
                                              
	
	    #endregion "data"
	
		#region "public method"
	    // public method

        public CCDSolver()
        {
            m_joints = new Joints();
            m_targetPos = Vector3.zero;
        }

        public int maxIterTimes
        {
            get { return m_maxIter; }
            set { m_maxIter = value; }
        }

        public float distThres
        {
            get { return m_distThres; }
            set { m_distThres = value; }
        }

        public bool useDamp
        {
            get { return m_useDamp; }
            set { m_useDamp = value; }
        }

        public float globalDamp
        {
            get { return m_globalDamp; }
            set { 
                float v = value;
                Dbg.Assert(v >= 0, "CCDSolver.globalDamp: v: {0}, should >= 0", v);
                m_globalDamp = v;
            }
        }

        public RevertOption revertOpt
        {
            get { return m_revertOpt; }
            set { m_revertOpt = value; }
        }

        public void RefreshConstraint()
        {
            for (int i = m_joints.Count - 2; i >= 0; --i)
            {
                var info = m_joints[i];
                info.constraints.Clear();
                info.joint.GetComponents<IKConstraintMB>(info.constraints);
            }
        }

        public List<IKConstraintMB> GetConstraint(int jointIdx)
        {
            return m_joints[jointIdx].constraints;
        }

		#region "ISolver impl."
        // "ISolver impl." 

        public IKSolverType Type { get { return IKSolverType.CCD; } }
        public int Count { get { return Mathf.Max(m_joints.Count-1, 0); } } //get the bones count in the link, not joints
        public Vector3 Target { 
            get { return m_targetPos; }
            set { m_targetPos = value; } 
        }

        public Transform[] GetJoints() { return m_jointArr; } //get all joints, including the end-effector

        /// <summary>
        /// [0] is the root, [last] is the endJoint
        /// NOTE: the transforms in the list could be NON-continuous
        /// </summary>
        public void SetBones(List<Transform> _joints)
        {
            Dbg.Assert(_joints.Count > /*1*/ 0, "CCDSolver.SetBones: the joints list length must be larger than 0");

            _ClearBones();

            m_joints.Resize(_joints.Count);

            // the endJoint is the joint user moves
            int len = _joints.Count-1; //len is the bone count, not joint count
            Transform endJoint = _joints.Last();

            // add the endJoint 
            JointData endInfo = new JointData();
            endInfo.joint = endJoint;
            m_joints[len] = (endInfo);

            // add joint from the end to the head
            for (int idx = len - 1; idx >= 0; --idx)
            {
                Transform j = _joints[idx];

                JointData info = new JointData();
                info.joint = j;
                j.GetComponents<IKConstraintMB>(info.constraints);

                m_joints[idx] = info;
            }

            Array.Resize(ref m_jointArr, len + 1);
            for (int i = 0; i < m_jointArr.Length; ++i)
            {
                m_jointArr[i] = m_joints[i].joint;
            }

            // notify init constraints
            for (int idx = len - 1; idx >= 0; --idx)
            {
                var info = m_joints[idx];
                foreach (var mb in info.constraints)
                {
                    mb.Init(this, idx);
                }
            }

            //calc total bone length & highDistThres
            float totalLen = 0;
            for (int i = 0; i < len; ++i)
            {
                var j0 = m_jointArr[i];
                var j1 = m_jointArr[i + 1];
                totalLen += (j0.position - j1.position).magnitude;
            }
            m_totalBoneLength = totalLen;
            m_highDistThres = m_totalBoneLength * m_highDistThresRatio;
        }

        public void SetBones(Transform endJoint, int len) 
        {
            //Dbg.Assert(len > 0, "CCDSolver.SetBones: the link length must be larger than 0");

            _ClearBones();

            m_joints.Resize(len+1); //+1 for the endJoint

            // the endJoint is the joint user moves, the link's last joint is its parent
            Transform joint = endJoint;

            // add the endJoint 
            JointData endInfo = new JointData();
            endInfo.joint = endJoint;
            m_joints[len] = (endInfo);

            // add joint from the end to the head
            for (int idx = len-1; idx >= 0; --idx)
            {
                Transform parentJoint = joint.parent;
                Dbg.Assert(parentJoint != null, "CCDSolver.SetBones: the link length is too big, there is already no parent joint for: {0}", joint);

                JointData info = new JointData();
                info.joint = parentJoint;
                parentJoint.GetComponents<IKConstraintMB>(info.constraints);

                m_joints[idx] = info;
                joint = parentJoint;
            }

            Array.Resize(ref m_jointArr, len+1);
            for (int i = 0; i < m_jointArr.Length; ++i)
            {
                m_jointArr[i] = m_joints[i].joint;
            }

            // notify init constraints
            for (int idx = len - 1; idx >= 0; --idx)
            {
                var info = m_joints[idx];
                foreach (var mb in info.constraints)
                {
                    mb.Init(this, idx);
                }
            }

            //calc total bone length & highDistThres
            float totalLen = 0;
            for (int i = 0; i < len; ++i)
            {
                var j0 = m_jointArr[i];
                var j1 = m_jointArr[i + 1];
                totalLen += (j0.position - j1.position).magnitude;
            }
            m_totalBoneLength = totalLen;
            m_highDistThres = m_totalBoneLength * m_highDistThresRatio;

        }

        public IKExecRes Execute(float weight = 1.0f ) 
        {
            if( m_revertOpt == RevertOption.RevertToStartState || m_backupData.Count == 0)
                _RecordIKChainState();

            Vector3 curTargetPos = Vector3.Lerp(m_joints.Last().joint.position, m_targetPos, weight);

            Transform endJoint = m_joints.Last().joint;

            bool bSuccess = false;
            for (int iter = 0; 
                iter < m_maxIter; 
                ++iter)
            {
                //bool hasJointChanged = false; //in this iter, if any joint has changed the joint rotation

                for (int idx = m_joints.Count - 2; idx >= 0; --idx)
                {
                    var info = m_joints[idx];
                    var joint = info.joint;
                    Vector3 jpos = joint.position;
                    Vector3 ejPos = endJoint.position;

                    Vector3 curDir = ejPos - jpos; //from this joint to endJoint;
                    Vector3 tgtDir = curTargetPos - jpos; //from this joint to targetPos

                    //Vector3 eulerBeforeRotate = joint.localEulerAngles;

                    Vector3 rotAxis = Vector3.Cross(curDir, tgtDir).normalized;
                    float rotDeg = _GetRotationDegree(curDir, tgtDir);

                    // notify constraints before rotation
                    _BeforeRotate(idx);

                    // straight line process (special case)
                    if (idx == m_joints.Count - 2 && rotAxis == Vector3.zero) // for the second last joint only, if straight line, check if the target is within the bone
                    {
                        float dot = Misc.VecDot(jpos, ejPos, ejPos, curTargetPos);
                        if (Mathf.Approximately(dot, -1f)) //the target is in opposite direction of the last bone primAxis
                        {
                            if (_AllBonesInLine())// check if all bones are in a straight line, no matter if same direction
                            { //if so, rotate the end bone randomly
                                rotDeg = Random.Range(15f, 45f);
                                rotAxis = Random.onUnitSphere;
                            }
                        }
                    }

                    // rotate!
                    joint.Rotate(rotAxis, rotDeg, Space.World);

                    // apply constraints
                    _ApplyConstraints(idx);

                    //// check if joint is changed
                    //Vector3 eulerAfterRotate = joint.localEulerAngles;
                    //if (eulerBeforeRotate != eulerAfterRotate)
                    //    hasJointChanged = true;

                    // check success condition
                    if (Count > 1)
                    {
                        if (Vector3.Distance(endJoint.position, curTargetPos) < m_distThres)
                        { //if reach target, then break out
                            bSuccess = true;
                            break;
                        }
                    }
                    else if( Count == 1)
                    { //if [1] - [0] same dir with target-[0], then success
                        Vector3 dir0 = (m_jointArr[1].position - m_jointArr[0].position).normalized;
                        Vector3 dir1 = (m_targetPos - m_jointArr[0].position).normalized;
                        if (Mathf.Approximately(Vector3.Dot(dir0, dir1), 1f))
                        {
                            bSuccess = true;
                            break;
                        }
                    }
                }


                if (bSuccess)
                {
                    //Dbg.Log("ReachTarget: Early quit: {0}", iter);
                    break;
                }

                { //check: out-of max reach and all bones are towards target
                    bool bStretched = true;
                    // check if all bones are aligned to target pos, and the targetPos is out-of the max reach
                    for (int idx = 0; idx < Count; ++idx)
                    {
                        var info = m_joints[idx];
                        var joint = info.joint;
                        Vector3 jpos = joint.position;
                        Vector3 nextJpos = m_joints[idx + 1].joint.position;

                        Vector3 boneDir = nextJpos - jpos; //from this joint to nextJoint;
                        Vector3 tgtDir = curTargetPos - nextJpos; //from this joint to targetPos

                        float dot = Vector3.Dot(boneDir.normalized, tgtDir.normalized);
                        if (dot < SAME_DIR_THRES)
                        {
                            bStretched = false;
                            break;
                        }
                    }

                    if (bStretched) //if all bones are directly pointed toward targetPos, then it's done
                    {
                        //Dbg.Log("AllBone aligned: Early quit: {0}", iter);
                        break;
                    }
                }

                //// if the whole link is locked down (not because all aligned which is handled in iteration, but by the constraint)
                //if (!hasJointChanged)
                //    _RandomRotateIKChain(); 

            } // for(int iter = 0 ...

            IKExecRes ret = IKExecRes.SUCCESS;

            if (!bSuccess) //check for epic failure
            { //if too far from the curTarget, and the direction is diverted more than 30d
              //revert the chain to starting state

                ret = IKExecRes.UNREACH_INLIMIT;

                Vector3 etDir = curTargetPos - endJoint.position;
                float targetDist = etDir.magnitude;
                if (targetDist > m_highDistThres)
                {
                    //Vector3 peDir = endJoint.position - endJoint.parent.position;
                    Vector3 peDir = endJoint.position - m_jointArr[m_joints.Count - 2].position;
                    if (Vector3.Angle(etDir, peDir) > 30f)
                    {
                        //Dbg.Log("Cannot reach {0} in {1} iterations, revert", curTargetPos, m_maxIter);
                        _RevertIKChainState();
                        ret = IKExecRes.UNREACH_EXCEEDLIMIT;
                    }
                }
            }

            if (m_revertOpt == RevertOption.RevertToPrevReasonableResult && ret != IKExecRes.UNREACH_EXCEEDLIMIT)
            {
                _RecordIKChainState();
            }

            //Dbg.Log("this time distance: {0}", Vector3.Magnitude(curTargetPos - endJoint.position));

            return ret;
        }

        //public List<IKConstraintMB> GetConstraintChecked(int jointIdx)
        //{
        //    var cons = m_joints[jointIdx].constraints;
        //    for (int i = cons.Count - 1; i >= 0; --i)
        //    {
        //        var con = cons[i];
        //        if (!con)
        //            cons.RemoveAt(i);
        //    }
        //    return cons;
        //}

		#endregion "ISolver impl."
	
		#region "Debug"
        public bool dbg_interrupt = false;
        public IEnumerator DBGExecute(float weight = 1.0f)
        {
            StringBuilder bld = new StringBuilder();
            Vector3 curTargetPos = Vector3.Lerp(m_joints.Last().joint.position, m_targetPos, weight);

            Transform endJoint = m_joints.Last().joint;

            dbg_interrupt = false;

            bool bSuccess = false;
            for (int iter = 0;
                iter < m_maxIter;
                ++iter)
            {

                for (int idx = m_joints.Count - 2; idx >= 0; --idx)
                {
                    var info = m_joints[idx];
                    var joint = info.joint;
                    Vector3 jpos = joint.position;
                    Vector3 ejPos = endJoint.position;

                    Vector3 curDir = ejPos - jpos; //from this joint to endJoint;
                    Vector3 tgtDir = curTargetPos - jpos; //from this joint to targetPos

                    Vector3 rotAxis = Vector3.Cross(curDir, tgtDir).normalized;
                    float rotDeg = _GetRotationDegree(curDir, tgtDir);

                    // notify constraints before rotation
                    _BeforeRotate(idx);

                    // straight line process
                    if (idx == m_joints.Count - 2 && rotAxis == Vector3.zero) // for the second last joint only, if straight line, check if the target is within the bone
                    {
                        float dot = Misc.VecDot(jpos, ejPos, ejPos, curTargetPos);
                        if (Mathf.Approximately(dot, -1f)) //the target is in opposite direction of the last bone primAxis
                        {
                            if (_AllBonesInLine())// check if all bones are in a straight line, no matter if same direction
                            { //if so, rotate the end bone randomly
                                rotDeg = Random.Range(15, 45f);
                                rotAxis = Random.onUnitSphere;
                            }
                        }
                    }

                    joint.Rotate(rotAxis, rotDeg, Space.World);
                    Dbg.Log("{0}, {3}: Rotate: axis: {1}, deg: {2}", iter, rotAxis.ToString("F2"), rotDeg, joint.name);
                    yield return 0;
                    if (dbg_interrupt)
                    {
                        Dbg.Log("dbg_interrupt");
                        yield break;
                    }

                    // apply constraints
                    var cs = info.constraints;
                    for (var ie = cs.GetEnumerator(); ie.MoveNext(); )
                    {
                        var mb = ie.Current;
                        if (mb.enabled)
                        {
                            mb.Apply(this, idx);
                            Dbg.Log("{0}, {1}: constraint ", iter, joint.name);

                            yield return 0;
                            if (dbg_interrupt)
                            {
                                Dbg.Log("dbg_interrupt");
                                yield break;
                            }
                        }
                    }

                    // check success condition
                    if (Count > 1)
                    {
                        if (Vector3.Distance(endJoint.position, curTargetPos) < m_distThres)
                        { //if reach target, then break out
                            bSuccess = true;
                            break;
                        }
                    }
                    else if (Count == 1)
                    { //if [1] - [0] same dir with target-[0], then success
                        Vector3 dir0 = (m_jointArr[1].position - m_jointArr[0].position).normalized;
                        Vector3 dir1 = (m_targetPos - m_jointArr[0].position).normalized;
                        if (Mathf.Approximately(Vector3.Dot(dir0, dir1), 1f))
                        {
                            bSuccess = true;
                            break;
                        }
                    }

                 
                }

                // summary for one iter, output all bones' rotation
                {
                    for (int idx = m_jointArr.Length - 2; idx >= 0; --idx)
                    {
                        var oj = m_jointArr[idx];
                        bld.AppendFormat("<{0}, {1}>", oj.name, oj.localEulerAngles.ToString("F3"));
                    }
                    Dbg.Log("iter{0}, {1}", iter, bld.ToString());
                    bld.Remove(0, bld.Length);
                }

                if (bSuccess)
                {
                    Dbg.Log("ReachTarget: Early quit: {0}", iter);
                    break;
                }

                bool allBonesAligned = true;
                // check if all bones are aligned to target pos
                for (int idx = 0; idx < Count; ++idx)
                {
                    var info = m_joints[idx];
                    var joint = info.joint;
                    Vector3 jpos = joint.position;
                    Vector3 nextJpos = m_joints[idx + 1].joint.position;

                    Vector3 boneDir = nextJpos - jpos; //from this joint to nextJoint;
                    Vector3 tgtDir = curTargetPos - nextJpos; //from this joint to targetPos

                    float dot = Vector3.Dot(boneDir.normalized, tgtDir.normalized);
                    if (dot < SAME_DIR_THRES)
                    {
                        allBonesAligned = false;
                        break;
                    }
                }

                if (allBonesAligned) //if all bones are directly pointed toward targetPos, then it's done
                {
                    Dbg.Log("AllBone aligned: Early quit: {0}", iter);
                    break;
                }
            }

            Dbg.Log("DbgExecute ends");
        }


		// "Debug" 
		
		#endregion "Debug"
	    #endregion "public method"
	
		#region "private method"
	    // private method

        private void _ClearBones()
        {
            m_joints.Clear();
            m_jointArr = null;
        }

        private float _GetRotationDegree(Vector3 curDir, Vector3 tgtDir)
        {
            float angle = Vector3.Angle(curDir, tgtDir);
            if (m_useDamp)
            {
                angle = Mathf.Clamp(angle, -m_globalDamp, m_globalDamp);
            }
            return angle;
        }

        private void _BeforeRotate(int idx)
        {
            var info = m_joints[idx];
            var cs = info.constraints;

            for (var ie = cs.GetEnumerator(); ie.MoveNext(); )
            {
                var mb = ie.Current;
                mb.BeforeRotate(this, idx);
            }
        }
	
        private void _ApplyConstraints(int idx)
        {
            var info = m_joints[idx];
            var cs = info.constraints;

            for (int i = 0; i < cs.Count; )
            {
                var mb = cs[i];
                if (!mb)
                {
                    cs.RemoveAt(i);
                    continue;
                }
                else
                {
                    if (mb.enabled) //only apply when the constraint's MB is enabled
                        mb.Apply(this, idx);

                    ++i;
                }
            }
        }        
		
        private bool _AllBonesInLine()
        {
            if (m_jointArr.Length < 2)
                return false;

            var dir = (m_jointArr[1].position - m_jointArr[0].position).normalized;

            for (int i = 1; i < m_jointArr.Length-1; ++i)
            {
                var j0 = m_jointArr[i];
                var j1 = m_jointArr[i + 1];
                var tdir = (j1.position - j0.position).normalized;
                if (!Mathf.Approximately(V3Ext.AbsDot(dir, tdir), 1f))
                    return false;
            }

            return true;
        }

        private void _RecordIKChainState()
        {
            m_backupData.Clear();
            for (int i = 0; i < m_jointArr.Length - 1; ++i)
            {
                var j = m_jointArr[i];
                m_backupData.Add(XformData.Create(j));
            }
        }

        private void _RevertIKChainState()
        {
            for (int i = 0; i < m_jointArr.Length - 1; ++i)
            {
                var j = m_jointArr[i];
                m_backupData[i].Apply(j);
            }
        }

        private void _RandomRotateIKChain()
        {
            for (int i = 0; i < m_jointArr.Length - 1; ++i)
            {
                var j = m_jointArr[i];
                Vector3 axis = Random.onUnitSphere;
                float angle = Random.Range(-30f, 30f);
                j.Rotate(axis, angle);
            }
        }

	    #endregion "private method"
	
		#region "constant data"
	    // constant data

        public enum RevertOption
        {
            NoRevert,
            RevertToStartState,
            RevertToPrevReasonableResult,
        }

        public const RevertOption DEF_RevertOpt = RevertOption.RevertToPrevReasonableResult;

        private readonly static float SAME_DIR_THRES = Mathf.Cos(3f * Mathf.Deg2Rad);
	
	    #endregion "constant data"

		#region "inner struct"
		// "inner struct" 

        public class JointData
        {
            public Transform joint;
            public List<IKConstraintMB> constraints = new List<IKConstraintMB>(); 
        }
		
		#endregion "inner struct"
        
        
    }
}
