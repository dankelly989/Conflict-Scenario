using System;
using System.Collections.Generic;

using UnityEngine;

namespace MH
{

/// <summary>
/// this constraints should be used with 2-bone IK links of arm or leg,
/// it will flip the joints whenever found violation of the constraint
/// 
/// it needs to specify the rotation axis
/// </summary>
public class LimbConstraint
{
	#region "data"
    // data

    private Vector3 m_RotateAxis = Vector3.zero; //the expected rotation axis, left-handed, 
                                         // when rotate the lower limb to upper limb by this axis, 
                                         // only allowed to rotate 0~180 degrees
                                         // this direction is relative to the IKRoot's transform

    private Vector3 m_IKRootRelDir = Vector3.zero; //this direction used to judge if IKRoot has rotated to wrong angle around bone-axis,
                                                   //relative to IKRoot's Transform

    private Vector3 m_SkeleRootRelDir = Vector3.zero; //with m_IKRootRelDir to judge if IKRoot has rotated to wrong angle around bond-axis,
                                                      //relative to Skeleton's root joint

    private Transform m_SkeleRootJoint = null;

    private float m_BoneAxisAngleThres = 0f;


    private ISolver m_IKSolver = null; //the related solver

    #endregion "data"

	#region "public method"
    // public method

    public LimbConstraint(ISolver solver)
    {
        m_IKSolver = solver;
    }

    public Vector3 RotateAxis
    {
        get { return m_RotateAxis; }
    }

    public void SetConstraintInfo(ConstraintInfo info)
    {
        m_RotateAxis = info.m_RotateAxis;
        m_IKRootRelDir = info.m_LimbRootRefDir;
        m_SkeleRootRelDir = info.m_SkeleRootRefDir;
        m_SkeleRootJoint = info.m_SkeleRootJoint;
        m_BoneAxisAngleThres = info.m_AngleThres;
    }

    /// <summary>
    /// 1. flip the joints if found the rotation is in wrong direction
    /// 2. Rotate the joints 180 degrees if the RotateAxis is pointing at wrong direction
    /// 
    /// 
    /// NOTE: if use in Editor, remember to call Undo.RecordObjects(),
    ///       as SMREditor already called Undo.RecordObjects before IKSolver.Execute, no need to do special things here.
    /// </summary>
    public void EnsureConstraint()
    {
        Dbg.Assert( m_RotateAxis != Vector3.zero, "LimbConstraint.EnsureConstaint: the RotateAxis is not set yet");
        Dbg.Assert( m_IKSolver.Count == 2, "LimbConstraint.EnsureConstraint: expected 2 bones, got {0} bones", m_IKSolver.Count);
        Transform[] joints = m_IKSolver.GetJoints();
        Transform jRoot = joints[0];
        Transform jMid = joints[1];
        Transform jEnd = joints[2];

        Vector3 upperDir = (jMid.position - jRoot.position).normalized;
        Vector3 lowerDir = (jEnd.position - jMid.position).normalized;

        Vector3 cross = Vector3.Cross(upperDir, lowerDir).normalized;
        if (cross == Vector3.zero)
            return;

        // Constraint 1
        {
            Quaternion q = Quaternion.FromToRotation(lowerDir, upperDir);

            float angle;
            Vector3 axis;
            q.ToAngleAxis(out angle, out axis);
            axis = jRoot.InverseTransformDirection(axis);

            if (MIN_ANGLE < angle && angle < MAX_ANGLE && Vector3.Dot(axis, m_RotateAxis) <= 0)
            {
                //Dbg.Log("LimbConstraint.EnsureConstraint: m_RotateAxis: {0}, axis: {1}, angle: {2}", m_RotateAxis, axis, angle);
                _FlipJoints();
            }
        }

        // Constraint 2
        {
            Vector3 worldSkeleRootRelDir = m_SkeleRootJoint.TransformDirection(m_SkeleRootRelDir).normalized;
            Vector3 worldIKRootRelDir = jRoot.TransformDirection(m_IKRootRelDir).normalized;
            float angle = Vector3.Angle(worldSkeleRootRelDir, worldIKRootRelDir);
            if (angle > m_BoneAxisAngleThres) //if the angle-dist 
            {
                //Dbg.Log("LimbConstraint.EnsureConstraint: skeleRootRel: {0}, ikRootRel: {1}", worldSkeleRootRelDir, worldIKRootRelDir);
                Vector3 rootEndAxis = jEnd.position - jRoot.position;
                jRoot.Rotate(rootEndAxis, 180f, Space.World);
            }
        }
        
    }

    #endregion "public method"

	#region "private method"
    // private method

    private void _FlipJoints()
    {
        Transform[] joints = m_IKSolver.GetJoints();

        Vector3 prev = (joints[joints.Length - 1].position - joints[0].position).normalized;

        if (prev == Vector3.zero)
        {
            Dbg.LogWarn("LimbConstraint._FlipJoints: the end-effector overlapped the IKRoot, interrupt flipping...");
            return;
        }

        for (int idx = 0; idx < joints.Length - 1; ++idx)
        {
            Transform curJoint = joints[idx];
            Vector3 boneDir = joints[idx + 1].position - joints[idx].position;
            Quaternion delta = Quaternion.FromToRotation(boneDir, prev);

            Quaternion newRot = delta * delta * curJoint.rotation;
            curJoint.rotation = newRot;

            prev = joints[idx + 1].position - joints[idx].position;
        }
    }

    #endregion "private method"

	#region "constant data"
    // constant data

    public const float MIN_ANGLE = 1.0f;
    public const float MAX_ANGLE = 180f;
    public const float AROUND_BONE_AXIS_ROTATE_LIMIT = 140f;

    #endregion "constant data"
}

}

