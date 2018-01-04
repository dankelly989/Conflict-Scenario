using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace MH
{

    using Joints = System.Collections.Generic.List<IKRTSolver.JointInfo>;
    using Limits = System.Collections.Generic.List<IKRTSolver.Limit>;
    using CachedTRS = System.Collections.Generic.List<IKRTSolver._TransformData>;
    using AxisLimit = MH.Pair<float, float>; // in angle

    /// <summary>
    /// used to compute a IK solution for given target position
    /// this used the Tri IK solver, better change to use CCD + Constraint
    /// </summary>
    public class IKRTSolver
    {
        #region "data"
        // data

        private Joints m_Joints; // first joint is the IKRoot, the last is the end-effector
        private Limits m_Limits; // the limits list
        private Vector3 m_TargetPos; //the position of target [in world system]
        private Transform[] m_TransArr; // the array of transform
        private CachedTRS m_CachedTRS; //

        #endregion "data"

        #region "public method"
        // public method

        public IKRTSolver()
        {
            m_Joints = new Joints();
            m_Limits = new Limits();
            m_CachedTRS = new CachedTRS();
        }

        /// <summary>
        /// given the end joint and the IK link length, setup the IK link
        /// </summary>
        public void SetBones(Transform endJoint, int len)
        {
            Dbg.Assert(len > 0, "IKRTSolver.SetBones: the link length must be larger than 0");

            ClearBones(len);
            ClearLimits(len);

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
                Dbg.Assert(parentJoint != null, "IKRTSolver.SetBones: the link length is too big, there is already no parent joint for: {0}", joint);

                JointInfo info = new JointInfo();
                info.joint = parentJoint;
                info.boneLen = (parentJoint.position - joint.position).magnitude;
                info.remainLen = totalLen;

                totalLen += info.boneLen;

                m_Joints.Add(info);
                joint = parentJoint;
            }

            m_Joints.Reverse(); //reverse the array to make the most significant joint at first entry

            m_TransArr = m_Joints.Select(x => x.joint).ToArray();
        }

        /// <summary>
        /// clear all info, 
        /// this IK solver instance will not work until SetBones is called again
        /// </summary>
        public void Reset()
        {
            ClearBones(0);
            ClearLimits(0);
        }

        public Limits GetLimits()
        {
            return m_Limits;
        }

        public void SetLimit(float v, ELimit eLimit, int jointIdx)
        {
            Dbg.Assert(jointIdx < this.Count, "IKRTSolver.SetLimit: jointIdx beyond range: {0}", jointIdx);

            float fv = Misc.NormalizeAngle(v);

            Limit l = m_Limits[jointIdx];

            switch (eLimit)
            {
                case ELimit.XFrom: l.X.first = fv; break;
                case ELimit.XTo: l.X.second = fv; break;
                case ELimit.YFrom: l.Y.first = fv; break;
                case ELimit.YTo: l.Y.second = fv; break;
                case ELimit.ZFrom: l.Z.first = fv; break;
                case ELimit.ZTo: l.Z.second = fv; break;
                default: Dbg.LogErr("IKSovler.SetLimit: unexpected value: {0}", eLimit); break;
            }
        }

        /// <summary>
        /// clear the limits
        /// </summary>
        private void ClearLimits(int len)
        {
            m_Limits.Clear();
            for (int idx = 0; idx < len; ++idx)
            {
                m_Limits.Add(new Limit());
            }
        }

        /// <summary>
        /// clear the bones
        /// </summary>
        public void ClearBones(int len)
        {
            m_Joints.Clear();
            m_TransArr = null;

            m_CachedTRS.Clear();
            for(int i=0; i<len; ++i)
            {
                m_CachedTRS.Add(new _TransformData());
            }
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
        /// the count of managed bones ( not joints ) 
        /// </summary>
        public int Count
        {
            get { return Mathf.Max(0, m_Joints.Count - 1); }
        }

        /// <summary>
        /// execute calculation, 
        /// will set the value for each Bones
        /// </summary>
        public bool Execute(float weight = 1.0f)
        {
            Dbg.Assert(m_Joints.Count > 0, "IKRTSolver.FixRotation: the Joints list is empty!");

            //////////////////////////////////////////////////////////////////////////
            // cache the Transform data
            //////////////////////////////////////////////////////////////////////////
            for(int idx =0; idx < this.Count; ++idx)
            {
                m_CachedTRS[idx].CopyFrom(m_Joints[idx].joint);
            }

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

                //// rotate the plane to contain IKtarget if there're still grandchild joint
                //if (idx < this.Count - 1 && vec_c != Vector3.zero)
                //{
                //    Transform grandChildJoint = m_Joints[idx + 2].joint;
                //    Vector3 vec_d = (grandChildJoint.position - childJoint.position).normalized;

                //    Vector3 n1 = Vector3.Cross(vec_a, vec_d);
                //    Vector3 n2 = Vector3.Cross(vec_a, vec_c);

                //    //don't do rotation if joint0-1-2 is almost in a line 
                //    if (n1.sqrMagnitude > PLANE_ROTATE_THRES && n2.sqrMagnitude > PLANE_ROTATE_THRES)
                //    {
                //        Quaternion q = Quaternion.FromToRotation(n1, n2);
                //        joint.rotation = q * joint.rotation;

                //        // need to fix vec_a
                //        vec_a = (childJoint.position - joint.position).normalized;
                //    }
                //}

                // special condition fix
                if (vec_a == Vector3.zero)
                {
                    Dbg.LogWarn("WTH...vec_a is zero? : joint: {0}, childJoint: {1}", joint, childJoint);
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
                    //_JointLookAt(joint, childJoint, vec_c, ref vec_a, len_a);
                    _JointLookAt(joint, vec_c, vec_a);
                }
                else if (len_c <= Mathf.Abs(len_a - len_b)) //target is too near
                {
                    //_JointLookAt(joint, childJoint, -vec_c, ref vec_a, len_a);
                    _JointLookAt(joint, vec_c, vec_a);
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
                }

                //Dbg.Log("BeforeFix: localRotation: {0}", joint.localEulerAngles);
                //joint.localRotation = m_Limits[idx].FixRotation(joint.localRotation);
                //Dbg.Log("AfterFix: localRotation: {0}", joint.localEulerAngles);


                if (idx == this.Count - 1 && Mathf.Approximately(len_c, len_a))
                    bSuccess = true;
            }

            //////////////////////////////////////////////////
            // lerp with weight
            //////////////////////////////////////////////////
            for (int idx = 0; idx < m_CachedTRS.Count; ++idx )
            {
                m_CachedTRS[idx].Apply(m_Joints[idx].joint, weight);
            }

            return bSuccess;
        }

        public Transform[] GetJoints()
        {
            return m_TransArr;
        }


        #endregion "public method"

        #region "private method"
        // private method

        /// <summary>
        /// be careful that vec_c has to be normalized vector
        /// </summary>
        //private void _JointLookAt(Transform joint, Transform childJoint, Vector3 vec_c, ref Vector3 vec_a, float len_a)
        private void _JointLookAt(Transform joint, Vector3 vec_c, Vector3 vec_a)
        {
            if (vec_a == vec_c)
            {
                //do nothing
            }
            else if (vec_a == -vec_c)
            {
                Vector3 rotAxis = joint.up;
                joint.Rotate(rotAxis, 180f, Space.World);
            }
            else
            {
                //Dbg.Log("beforeLookat: vec_a: {0}, vec_c: {1}, joint.right: {2}", vec_a.ToString("F3"), vec_c.ToString("F3"), joint.right); 

                Quaternion rot = Quaternion.FromToRotation(vec_a, vec_c);
                joint.rotation = rot * joint.rotation;

                //Transform childJoint = joint.GetChild(0);
                //Dbg.Log("afterLookAt: vec_a: {0}, vec_c: {1}, joint.right: {2}",
                //    (childJoint.position - joint.position).normalized.ToString("F3"),
                //    (m_TargetPos - joint.position).normalized.ToString("F3"),
                //    joint.right
                //);
            }
        }




        #endregion "private method"

        #region "constant data"
        // constant data

        private const float PLANE_ROTATE_THRES = 0.001f; //make it high to prevent jittering

        public enum ELimit
        {
            INVALID = -1,
            XFrom,
            XTo,
            YFrom,
            YTo,
            ZFrom,
            ZTo,
            END
        }

        #endregion "constant data"

        #region "inner struct"
        // "inner struct" 

        /// <summary>
        /// used to cache transform's local data
        /// </summary>
        public class _TransformData
        {
            public Vector3 pos;
            public Quaternion rot;
            public Vector3 scale;

            public void CopyFrom(Transform tr)
            {
                pos = tr.localPosition;
                rot = tr.localRotation;
                scale = tr.localScale;
            }

            public void Apply(Transform tr)
            {
                tr.localPosition = pos;
                tr.localRotation = rot;
                tr.localScale = scale;
            }

            /// <summary>
            /// make a S-interpolation between "cached_value" and "current_val_in_tr"
            /// if weight = 1.0f, then use "current_val_in_tr"
            /// if weight = 0   , then use "cached_value"
            /// </summary>
            public void Apply(Transform tr, float weight)
            {
                Vector3 cPos = tr.localPosition;
                tr.localPosition = Vector3.Lerp(pos, cPos, weight);

                Quaternion cRot = tr.localRotation;
                tr.localRotation = Quaternion.Slerp(rot, cRot, weight);

                Vector3 cScale = tr.localScale;
                tr.localScale = Vector3.Lerp(scale, cScale, weight);
            }

            public void Clear()
            {
                pos = Vector3.zero;
                rot = Quaternion.identity;
                scale = Vector3.zero;
            }
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
        /// the limit parameters for one joint
        /// 
        /// each AxisLimit's first and second should be within [0,360]
        /// </summary>
        public class Limit
        {
            public AxisLimit[] m_xyz = new AxisLimit[3];

            public Limit()
            {
                for (int i = 0; i < 3; ++i)
                {
                    m_xyz[i] = new Pair<float, float>(0f, 360f);
                }
            }

            public AxisLimit X { get { return m_xyz[0]; } }
            public AxisLimit Y { get { return m_xyz[1]; } }
            public AxisLimit Z { get { return m_xyz[2]; } }

            /// <summary>
            /// pass-in a rotation, ensure return a quaternion meets the limit
            /// </summary>
            public Quaternion FixRotation(Quaternion q)
            {
                Vector3 euler = q.eulerAngles;

                for (int i = 0; i < 3; ++i)
                {
                    float v = Misc.NormalizeAngle(euler[i]);
                    AxisLimit limit = m_xyz[i];

                    float from = limit.first;
                    float to = limit.second;

                    bool bValid = false;
                    if (to < from)
                    {
                        bValid = (v <= to || from <= v);

                    }
                    else
                    {
                        bValid = (from <= v && v <= to);
                    }

                    if (!bValid)
                    {
                        float distTo = Misc.AngleDist(v, to);
                        float distFrom = Misc.AngleDist(v, from);

                        //Dbg.Log("v: {0}, from:{1}, to:{2}, distTo:{3}, distFrom:{4}", v, from, to, distTo, distFrom);

                        if (distTo <= distFrom)
                        {
                            v = to;
                        }
                        else
                        {
                            v = from;
                        }

                        euler[i] = v;

                    }
                }

                Quaternion newQ = Quaternion.Euler(euler);

                return newQ;
            }
        }

        #endregion "inner struct"
    }



}

