using System;
using System.Collections.Generic;
using UnityEngine;


namespace MH
{

using Joints = System.Collections.Generic.List<JointInfo>;

public class IKSolverUtil
{

    /// <summary>
    /// check if the two NORMALIZED directions are near
    /// </summary>
    public static bool DirNear(Vector3 lhs, Vector3 rhs)
    {
        Dbg.Assert(lhs != Vector3.zero && rhs != Vector3.zero, "IKSolverUtil.DirNear: zero vector");
        //float c = Vector3.Dot(lhs, rhs);
        //return Mathf.Approximately(c, 1.0f); 

        Vector3 c = Vector3.Cross(lhs, rhs).normalized;
        return c == Vector3.zero;
    }

    /// <summary>
    /// be careful that vec_c has to be normalized vector
    /// </summary>
    //private void _JointLookAt(Transform joint, Transform childJoint, Vector3 vec_c, ref Vector3 vec_a, float len_a)
    public static void JointLookAt(Transform joint, Vector3 vec_c, Vector3 vec_a)
    {
        if ( DirNear(vec_a, vec_c) )
        {
            //do nothing
        }
        else if (DirNear(vec_a, -vec_c))
        {
            Vector3 rotAxis = joint.up;
            joint.Rotate(rotAxis, 180f, Space.World);
        }
        else
        {
            //Dbg.Log("beforeLookat: vec_a: {0}, vec_c: {1}, joint.right: {2}", vec_a.ToString("F3"), vec_c.ToString("F3"), joint.right); 

            Quaternion rot = Quaternion.FromToRotation(vec_a, vec_c);
            //Quaternion q1 = Quaternion.LookRotation(vec_a);
            //Quaternion q2 = Quaternion.LookRotation(vec_c);
            //Quaternion rot = q2 * Quaternion.Inverse(q1);

            joint.rotation = rot * joint.rotation;

            //Transform childJoint = joint.GetChild(0);
            //Dbg.Log("afterLookAt: vec_a: {0}, vec_c: {1}, joint.right: {2}",
            //    (childJoint.position - joint.position).normalized.ToString("F3"),
            //    (m_TargetPos - joint.position).normalized.ToString("F3"),
            //    joint.right
            //);
        }
    }
}

/// <summary>
/// the interface for IKSolvers
/// </summary>
public interface ISolver
{
    void SetBones(Transform endJoint, int len); //len is the bone count, not joint count, e.g.: for arm, len = 2, endJoint = hand
    IKExecRes Execute(float weight = 1.0f);
    Vector3 Target { get; set; }
    int Count { get; } //get the bones in the link, not joints, so the Count = GetJoints().Length - 1
    IKSolverType Type { get; }
    Transform[] GetJoints(); //get all joints, including the end-effector, the [0] is root, the last is end-effector


}

public enum IKSolverType
{
    TRI,
    TRI_LIMB,
    CCD,
    END,
}

public enum IKExecRes
{
    SUCCESS,
    UNREACH_INLIMIT,
    UNREACH_EXCEEDLIMIT,
}

/// <summary>
/// endInfo for one joint
/// </summary>
public class JointInfo
{
    public Transform joint;
    public float boneLen; //the bone length, defined as the len( pos(joint) - pos(joint's child joint) );
    public float remainLen; //the total bones length under this bone, e.g.: for joints setup 1->2->3->4, consider (1), boneLen = (2)-(1), remainLen = (4)-(2)
}

/// <summary>
/// used to compute a IK solution for given target position
/// 
/// this Solver don't have constraint
/// </summary>
public class BaseIKSolver : ISolver
{
	#region "data"
    // data

    private Joints m_Joints; // first joint is the IKRoot, the last is the end-effector
    private Vector3 m_TargetPos; //the position of target [in world system]
    private Transform[] m_TransArr; // the array of transform

    #endregion "data"

	#region "public method"
    // public method

    public BaseIKSolver()
    {
        m_Joints = new Joints();
    }

    /// <summary>
    /// given the end joint and the IK link length, setup the IK link
    /// </summary>
    public void SetBones(Transform endJoint, int len)
    {
        Dbg.Assert(len > 0, "BaseIKSolver.SetBones: the link length must be larger than 0");

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
        for( int idx = 0; idx < len; ++idx )
        {
            Transform parentJoint = joint.parent;
            Dbg.Assert(parentJoint != null, "BaseIKSolver.SetBones: the link length is too big, there is already no parent joint for: {0}", joint);

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

    /// <summary>
    /// clear the bones
    /// </summary>
    public void ClearBones()
    {
        m_Joints.Clear();
        m_TransArr = null;
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
        get { return Mathf.Max(0, m_Joints.Count-1);}
    }

    public IKSolverType Type
    {
        get { return IKSolverType.TRI; }
    }

    /// <summary>
    /// execute calculation, 
    /// will set the value for each Bones
    /// </summary>
    public IKExecRes Execute(float weight = 1.0f)
    {
        Dbg.Assert(m_Joints.Count > 0, "BaseIKSolver.Execute: the Joints list is empty!");

        bool bSuccess = false;
        //////////////////////////////////////////////////
        // from the most significant joint to the end effector
        // don't count the endJoint
        //////////////////////////////////////////////////
        for (int idx = 0; idx < this.Count; ++idx ) 
        {
            JointInfo jointInfo = m_Joints[idx];
            Transform joint = jointInfo.joint;
            Transform childJoint = m_Joints[idx+1].joint;
            float len_a = jointInfo.boneLen;
            float len_b = jointInfo.remainLen;

            Vector3 vec_c = m_TargetPos - joint.position;
            float len_c = vec_c.magnitude;

            // normalize both vec_a and vec_c
            vec_c.Normalize(); // vec_c is the direction with len = 1
            Vector3 vec_a = (childJoint.position - joint.position).normalized;

            // rotate the plane to contain IKtarget if there're still grandchild joint
            if (idx < this.Count - 1 && len_a + len_b > len_c && vec_c != Vector3.zero)
            {
                Transform grandChildJoint = m_Joints[idx + 2].joint;
                Vector3 vec_d = (grandChildJoint.position - childJoint.position).normalized;

                Vector3 n1 = Vector3.Cross(vec_a, vec_d);
                Vector3 n2 = Vector3.Cross(vec_a, vec_c);

                //don't do rotation if joint0-1-2 is almost in a line 
                if (n1.sqrMagnitude > PLANE_ROTATE_THRES && n2.sqrMagnitude > PLANE_ROTATE_THRES)
                {
                    Quaternion q = Quaternion.FromToRotation(n1, n2);
                    joint.rotation = q * joint.rotation;

                    // need to fix vec_a
                    vec_a = (childJoint.position - joint.position).normalized;
                }
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
                //IKSolverUtil.JointLookAt(joint, childJoint, vec_c, ref vec_a, len_a);
                IKSolverUtil.JointLookAt(joint, vec_c, vec_a);
            }
            else if (len_c <= Mathf.Abs(len_a - len_b)) //target is too near
            {
                //IKSolverUtil.JointLookAt(joint, childJoint, -vec_c, ref vec_a, len_a);
                IKSolverUtil.JointLookAt(joint, vec_c, vec_a);
            }
            else
            {
                float radian_b = Mathf.Acos((len_a * len_a + len_c * len_c - len_b * len_b) / (2 * len_a * len_c));
                float vv = Vector3.Dot(vec_a, vec_c); 
               
                vv = Mathf.Clamp(vv, -1f, 1f); //precision error protection, the direct result of dotP might cause Acos return NaN
                float radian_rot = Mathf.Acos(vv) - radian_b;
                if( float.IsNaN(radian_rot) )
                {
                    Dbg.LogWarn("NaN???!!!Not Again...");
                }
                float angle_rot = radian_rot * Mathf.Rad2Deg;

                Vector3 rotAxis;
                if(vec_a == vec_c || vec_a == -vec_c)
                {
                    rotAxis = joint.up;
                    joint.Rotate(rotAxis, angle_rot, Space.World);
                }
                else
                {
                    rotAxis = Vector3.Cross(vec_a, vec_c);
                    joint.Rotate(rotAxis, angle_rot, Space.World);
                }
            }

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

	#region "inner struct"
	// "inner struct" 

    ///// <summary>
    ///// the limit parameters for one joint
    ///// 
    ///// each AxisLimit's first and second should be within [0,360]
    ///// </summary>
    //public class Limit
    //{
    //    public AxisLimit[] m_xyz = new AxisLimit[3];

    //    public Limit()
    //    {      
    //        for(int i=0; i<3; ++i)
    //        {
    //            m_xyz[i] = new Pair<float,float>(0f, 360f);
    //        }
    //    }

    //    public AxisLimit X { get { return m_xyz[0]; } }
    //    public AxisLimit Y { get { return m_xyz[1]; } }
    //    public AxisLimit Z { get { return m_xyz[2]; } }

    //    /// <summary>
    //    /// pass-in a rotation, ensure return a quaternion meets the limit
    //    /// </summary>
    //    public Quaternion FixRotation(Quaternion q)
    //    {
    //        Vector3 euler = q.eulerAngles;

    //        for( int i=0; i<3; ++i)
    //        {
    //            float v = Misc.NormalizeAngle(euler[i]);
    //            AxisLimit limit = m_xyz[i];

    //            float from = limit.first;
    //            float to = limit.second;

    //            bool bValid = false;
    //            if( to < from ) 
    //            {
    //                bValid = (v <= to || from <= v);
                    
    //            }
    //            else
    //            {
    //                bValid = (from <= v && v <= to);
    //            }

    //            if (!bValid)
    //            {
    //                float distTo = Misc.AngleDist(v, to);
    //                float distFrom = Misc.AngleDist(v, from);

    //                //Dbg.Log("v: {0}, from:{1}, to:{2}, distTo:{3}, distFrom:{4}", v, from, to, distTo, distFrom);

    //                if (distTo <= distFrom)
    //                {
    //                    v = to;
    //                }
    //                else
    //                {
    //                    v = from;
    //                }

    //                euler[i] = v;

    //            }
    //        }

    //        Quaternion newQ = Quaternion.Euler(euler);

    //        return newQ;
    //    }
    //}
	
	#endregion "inner struct"
}





}

