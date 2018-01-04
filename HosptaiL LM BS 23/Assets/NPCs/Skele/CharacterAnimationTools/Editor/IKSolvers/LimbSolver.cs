//#define IMDEBUGGING

using System;
using System.Collections.Generic;
using UnityEngine;

namespace MH
{

using Joints = System.Collections.Generic.List<JointInfo>;

/// <summary>
/// this IK Solver is specialized for 2-bone IK link like arms & legs
/// 
/// it takes a rotation axis for the mid joint as param.
/// </summary>
public class IKLimbSolver : ISolver
{
    #region "data"
    // data

    private Joints m_Joints; // first joint is the IKRoot, the last is the end-effector
    private Vector3 m_TargetPos; //the position of target [in world system]
    private Transform[] m_TransArr; // the array of transform
    private LimbConstraint m_Constraint;

    private int m_cnter = 0;

    #endregion "data"

    #region "public method"
    // public method

    public IKLimbSolver()
    {
        m_Joints = new Joints();
        m_Constraint = new LimbConstraint(this);
    }

    /// <summary>
    /// given the end joint and the IK link length, setup the IK link
    /// </summary>
    public void SetBones(Transform endJoint, int len)
    {
        Dbg.Assert(len == 2, "IKLimbSolver.SetBones: the link length must be 2");

        ClearBones();

        // the endJoint is the joint user moves, the link's last joint is its parent
        Transform joint = endJoint;
        float totalLen = 0;

        // add the endJoint 
        JointInfo endInfo = new JointInfo();
        endInfo.joint = endJoint;
        endInfo.boneLen = 0;
        endInfo.remainLen = 0;
        m_Joints.Add(endInfo);

        // add joint from the end to the head
        for (int idx = 0; idx < len; ++idx)
        {
            Transform parentJoint = joint.parent;
            Dbg.Assert(parentJoint != null, "IKLimbSolver.SetBones: the link length is too big, there is already no parent joint for: {0}", joint);

            JointInfo info = new JointInfo();
            info.joint = parentJoint;
            info.boneLen = (parentJoint.position - joint.position).magnitude;
            info.remainLen = totalLen;

            totalLen += info.boneLen;

            m_Joints.Add(info);
            joint = parentJoint;
        }

        m_Joints.Reverse(); //reverse the array to make the most significant joint at first entry

        m_TransArr = new Transform[m_Joints.Count];
        for (int i = 0; i < m_TransArr.Length; ++i)
        {
            m_TransArr[i] = m_Joints[i].joint;
        }
    }

    public void SetConstraintInfo(ConstraintInfo info)
    {
        m_Constraint.SetConstraintInfo(info);
    }

    /// <summary>
    /// clear the bones
    /// </summary>
    public void ClearBones()
    {
        m_Joints.Clear();
        m_TransArr = null;
    }

    public IKSolverType Type
    {
        get { return IKSolverType.TRI_LIMB; }
    }

    /// <summary>
    /// the IK Target
    /// </summary>
    public Vector3 Target
    {
        get { return m_TargetPos; }
        set { m_TargetPos = value; }
    }

    /// <summary>
    /// the count of managed joints 
    /// </summary>
    public int Count
    {
        get { return Mathf.Max(0, m_Joints.Count - 1); }
    }

    /// <summary>
    /// execute calculation, 
    /// will set the value for each Bones
    /// </summary>
    public IKExecRes Execute(float weight = 1.0f)
    {
        ++m_cnter;
        Dbg.Assert(m_Joints.Count > 0, "IKLimbSolver.Execute: the Joints list is empty!");

        bool bSuccess = false;
        //////////////////////////////////////////////////
        // from the most significant joint to the end effector
        // don't count the endJoint
        //////////////////////////////////////////////////
        for (int idx = 0; idx < this.Count; ++idx)
        {
            JointInfo jointInfo = m_Joints[idx];
            Transform joint = jointInfo.joint;
            Transform childJoint = m_Joints[idx + 1].joint;
            float len_a = jointInfo.boneLen;
            float len_b = jointInfo.remainLen;

            Vector3 vec_c = m_TargetPos - joint.position;
            float len_c = vec_c.magnitude;

            // normalize both vec_a and vec_c
            vec_c.Normalize(); // vec_c is the direction with len = 1
            Vector3 vec_a = (childJoint.position - joint.position).normalized;

            // rotate the plane to contain IKtarget if there're still grandchild joint
            //NOTE1: "len_a+len_b > len_c" test is important to prevent rootJoint jitter when limb is straight
            if (idx == 0 && vec_c != Vector3.zero && len_a + len_b > len_c && ! IKSolverUtil.DirNear(vec_a, vec_c) )
            {
                Vector3 n1 = joint.TransformDirection(m_Constraint.RotateAxis);
                Vector3 n2 = Vector3.Cross(vec_c, vec_a).normalized;

                //Quaternion q = Quaternion.FromToRotation(n1, n2);
                Quaternion q1 = Quaternion.LookRotation(n1);
                Quaternion q2 = Quaternion.LookRotation(n2);
                Quaternion q = q2 * Quaternion.Inverse(q1);


                joint.rotation = q * joint.rotation;
                // need to fix vec_a
                vec_a = (childJoint.position - joint.position).normalized;


                #if IMDEBUGGING
                float angle = Quaternion.Angle(q1, q2);
                Dbg.Log("{0}, Align Plane: rot: {1}, angle: {2}, n1: {3}, n2:{4}", m_cnter, joint.rotation, angle, n1, n2);
                #endif

            }

            // special condition fix
            if (vec_a == Vector3.zero)
            {
                Dbg.LogWarn("What the?...childJoint at same pos with parentJoint!? : joint: {0}, childJoint: {1}", joint, childJoint);
            }
            if (vec_c == Vector3.zero)
            {
                vec_c = vec_a;
            }

            // main work
            if (len_c >= len_a + len_b || //target is too far
                idx == this.Count - 1 //the last joint should just look towards targetPos
                )
            {
                #if IMDEBUGGING
                Quaternion qold = joint.rotation;
                #endif
                IKSolverUtil.JointLookAt(joint, vec_c, vec_a);
                #if IMDEBUGGING
                if (idx == 0) Dbg.Log("{0}, LookAt1: oldRot: {1}, newRot: {2}", m_cnter, qold, joint.rotation);
                #endif
            }
            else if (len_c <= Mathf.Abs(len_a - len_b)) //target is too near
            {
                IKSolverUtil.JointLookAt(joint, vec_c, vec_a);
                #if IMDEBUGGING
                if (idx == 0) Dbg.Log("{0}, LookAt2: Rot: {1}", m_cnter, joint.rotation);
                #endif
            }
            else
            {
                float radian_b = Mathf.Acos((len_a * len_a + len_c * len_c - len_b * len_b) / (2 * len_a * len_c));
                float vv = Vector3.Dot(vec_a, vec_c);

                vv = Mathf.Clamp(vv, -1f, 1f); //precision error protection, the direct result of dotP might cause Acos return NaN
                float radian_rot = Mathf.Acos(vv) - radian_b;
                if (float.IsNaN(radian_rot))
                {
                    Dbg.LogWarn("NaN???!!!Not Again...");
                }
                float angle_rot = radian_rot * Mathf.Rad2Deg;

                Vector3 rotAxis;
                if (vec_a == vec_c || vec_a == -vec_c)
                {
                    rotAxis = joint.up;
                    joint.Rotate(rotAxis, angle_rot, Space.World);
                }
                else
                {
                    rotAxis = Vector3.Cross(vec_a, vec_c);
                    joint.Rotate(rotAxis, angle_rot, Space.World);
                }

                #if IMDEBUGGING
                if (idx == 0) Dbg.Log("{0}, Rotate: rot: {1}", m_cnter, joint.rotation); 
                #endif
            }

            // use constraint to prevent joint bend to wrong direction
            m_Constraint.EnsureConstraint();

            if (idx == this.Count - 1 && Mathf.Approximately(len_c, len_a))
                bSuccess = true;
        }

        return bSuccess ? IKExecRes.SUCCESS : IKExecRes.UNREACH_INLIMIT;
    }

    public Transform[] GetJoints()
    {
        return m_TransArr;
    }


    #endregion "public method"

    #region "private method"
    // private method

    

    #endregion "private method"

    #region "constant data"
    // constant data

    private const float PLANE_ROTATE_THRES = Vector3.kEpsilon; //make it high to prevent jittering

    #endregion "constant data"
}


}

