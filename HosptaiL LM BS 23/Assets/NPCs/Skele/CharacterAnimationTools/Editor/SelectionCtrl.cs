using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH
{
    using JointList = System.Collections.Generic.List<UnityEngine.Transform>;

	public class SelectionCtrl
	{
	    #region "data"
        // data
        private Transform m_SingleJoint;
        private List<Transform> m_MultiJoints;
        private List<Transform> m_ToAddMultiJoints;
        private List<Transform> m_ToDelMultiJoints;
        private Transform m_SingleJointReq;

        private bool m_SelectionChanged = false;

        #endregion "data"

	    #region "public method"
        // public method

        public SelectionCtrl()
        {
            m_SingleJointReq = m_SingleJoint = null;
            m_MultiJoints = new List<Transform>();
            m_ToAddMultiJoints = new List<Transform>();
            m_ToDelMultiJoints = new List<Transform>();
        }

        public bool SelectionChanged
        {
            get { return m_SelectionChanged; }
        }

        public Transform SingleJointReq
        {
            get { return m_SingleJointReq; }
            set { m_SingleJointReq = value; m_SelectionChanged = true; }
        }

        public Transform SingleJoint
        {
            get { return m_SingleJoint; }
        }
        public List<Transform> Joints
        {
            get { return m_MultiJoints; }
        }

        public Transform[] JointsArray
        {
            get { return m_MultiJoints.ToArray(); }
        }

        public bool HasMulti
        {
            get { return m_MultiJoints.Count > 1; }
        }
        public bool NoSelection
        {
            get { return m_SingleJoint == null; }
        }

        public void Clear()
        {
            m_SingleJoint = m_SingleJointReq = null;
            m_MultiJoints.Clear();
        }

        public void DirectSetSelectJoint(Transform joint)
        {
            m_SingleJoint = joint;
        }

        public void ApplyMultiJointChange()
        {
            if (!m_SelectionChanged)
                return;

            // add
            foreach( var tr in m_ToAddMultiJoints)
            {
                m_MultiJoints.Add(tr);
            }
            m_ToAddMultiJoints.Clear();

            // del
            foreach( var tr in m_ToDelMultiJoints )
            {
                m_MultiJoints.Remove(tr);
            }
            m_ToDelMultiJoints.Clear();

            // fix the single joint
            if (m_SingleJoint != null && !m_MultiJoints.Contains(m_SingleJoint))
            {
                SingleJointReq = (m_MultiJoints.Count != 0) ?
                    m_MultiJoints[m_MultiJoints.Count - 1] : null;
            }

            m_SelectionChanged = false;
        }

        public bool IsSelectedJoint(Transform joint)
        {
            return m_MultiJoints.Contains(joint);
        }

        /// <summary>
        /// find out if `child' is a child joint of a selected joint
        /// </summary>
        public bool IsChildOfSelectedJoint(Transform child)
        {
            for( int i=0; i < m_MultiJoints.Count; ++i )
            {
                Transform j = m_MultiJoints[i];
                if (child != j && child.IsChildOf(j))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// get the average position of all selected joints
        /// return zero if no selection
        /// </summary>
        public Vector3 AvgPos()
        {
            if (NoSelection)
                return Vector3.zero;

            Vector3 sum = Vector3.zero;
            for( int i=0; i<m_MultiJoints.Count; ++i )
            {
                Transform j = m_MultiJoints[i];
                sum += j.position;
            }
            sum /= m_MultiJoints.Count;

            return sum;
        }

        public Quaternion AvgRot()
        {
            if (NoSelection)
                return Quaternion.identity;

            if( m_MultiJoints.Count == 1 )
            {
                Transform j = m_MultiJoints[0];
                return SMREditor.GetQuaternionByPivotRotation(j);
            }

            //float t = 1f / m_MultiJoints.Count;
            Quaternion avg = Quaternion.identity;
            Vector3 dir = Vector3.zero;

            for( int i=0; i<m_MultiJoints.Count; ++i)
            {
                Transform j = m_MultiJoints[i];
                Quaternion r = SMREditor.GetQuaternionByPivotRotation(j);

                dir += r * Vector3.forward;

                //Quaternion q = Quaternion.Slerp(Quaternion.identity, r, t);
                //avg = q * avg;
            }

            if (dir == Vector3.zero)
                avg = Quaternion.identity;
            else
                avg = Quaternion.LookRotation(dir);

            return avg;
        }

        /// <summary>
        /// select/deselect all joints under `startJoint'
        /// </summary>
        public void RecurSelect(Transform startJoint, bool select)
        {
            JointList extendedJoints = SMREditor.GetExtendedJoints();
            if (!extendedJoints.Contains(startJoint))
                return;

            _Recur_SelectJointAndAllDescendants(startJoint, select);
        }

        /// <summary>
        /// select a joint, clear all multi-joints
        /// </summary>
        public void Select(Transform joint)
        {
            _AddToMultiJoints(joint);

            for (int i = 0; i < m_MultiJoints.Count; ++i )
            {
                if( m_MultiJoints[i] != joint )
                {
                    m_ToDelMultiJoints.Add(m_MultiJoints[i]);
                }
            }

            SingleJointReq = joint;
        }

        /// <summary>
        /// add a select joint 
        /// </summary>
        public void IncSelect(Transform joint, bool bModifySingleJoint = true)
        {
            if( bModifySingleJoint )
                SingleJointReq = joint;

            _AddToMultiJoints(joint);
            m_SelectionChanged = true;
        }

        /// <summary>
        /// remove a joint from selection,
        /// </summary>
        public void DecSelect(Transform joint)
        {
            _DelFromMultiJoints(joint);

            int idx = m_MultiJoints.IndexOf(joint);
            if( -1 != idx  )
            {
                m_SelectionChanged = true;
            }
        }

        /// <summary>
        /// if `joint' is not selected yet, == AddSelect()
        /// else == DecSelect
        /// </summary>
        public void ToggleSelect(Transform joint)
        {
            if (joint == null)
                return;

            if( IsSelectedJoint(joint) )
            {
                DecSelect(joint);
            }
            else
            {
                IncSelect(joint);
            }
        }

        /// <summary>
        /// if no selection, then do nothing;
        /// if no parent, do nothing;
        /// </summary>
        public void SelectParentBone()
        {
            if (NoSelection)
                return;

            List<Transform> joints = SMREditor.GetExtendedJoints();

            Transform joint = m_SingleJoint;
            if( joint.parent != null && joints.Contains(joint.parent))
                Select(joint.parent);
        }

        /// <summary>
        /// if no selection, then do nothing;
        /// if no child, do nothing
        /// else select the first child
        /// </summary>
        public void SelectChildBone(int idx = 0)
        {
            if (NoSelection)
                return;

            Transform joint = m_SingleJoint;
            if (joint.childCount == 0)
                return;

            Transform childJoint = _GetChildJointByIdx(joint, idx);

            if(childJoint != null)
                Select(childJoint);
        }

        public void SelectPrevSibling()
        {
            if (NoSelection)
                return;

            Transform joint = m_SingleJoint;
            if (joint == null || joint.parent == null)
                return;

            Transform parent = joint.parent;
            int cnt = _GetCountOfChildJoint(parent);
            int idx = _GetIdxOfChildJoint(parent, joint);

            idx = (idx + cnt - 1) % cnt;

            Transform prevJoint = _GetChildJointByIdx(parent, idx);
            if( prevJoint != null)
                Select(prevJoint);
        }

        public void SelectNextSibling()
        {
            if (NoSelection)
                return;

            Transform joint = m_SingleJoint;
            if (joint == null || joint.parent == null)
                return;

            Transform parent = joint.parent;
            int cnt = _GetCountOfChildJoint(parent);
            int idx = _GetIdxOfChildJoint(parent, joint);

            idx = (idx + 1) % cnt;

            Transform prevJoint = _GetChildJointByIdx(parent, idx);
            if (prevJoint != null)
                Select(prevJoint);
        }

        #endregion "public method"

	    #region "private method"
        // private method

        private void _AddToMultiJoints(Transform joint)
        {
            if (joint == null)
                return;

            if( !m_MultiJoints.Contains(joint) && !m_ToAddMultiJoints.Contains(joint))
                m_ToAddMultiJoints.Add(joint);

            m_ToDelMultiJoints.Remove(joint);
        }

        private void _DelFromMultiJoints(Transform joint)
        {
            if (joint == null)
                return;

            if( !m_ToDelMultiJoints.Contains(joint) )
                m_ToDelMultiJoints.Add(joint);

            m_ToAddMultiJoints.Remove(joint);
        }

        /// <summary>
        /// childJointIdx is the childJointIdx of bones, doesn't count other gameobject
        /// </summary>
        private Transform _GetChildJointByIdx(Transform joint, int childJointIdx)
        {
            List<Transform> extJoints = SMREditor.GetExtendedJoints();
            int cnt = 0;
            for( int i = 0; i < joint.childCount; ++ i)
            {
                Transform j = joint.GetChild(i);
                if( extJoints.Contains(j) )
                {
                    ++cnt;
                    if( cnt-1 == childJointIdx )
                    {
                        return j;
                    }
                }
            }

            return null;
        }

        private int _GetIdxOfChildJoint(Transform parentJoint, Transform childJoint)
        {
            int childJointIdx = -1;
            List<Transform> joints = SMREditor.GetExtendedJoints();

            for(int i = 0; i < parentJoint.childCount; ++i)
            {
                Transform j = parentJoint.GetChild(i);
                if( joints.Contains(j) )
                {
                    childJointIdx++;
                    if (j == childJoint)
                        return childJointIdx;
                }
            }

            return -1;
        }

        private int _GetCountOfChildJoint(Transform parentJoint)
        {
            List<Transform> joints = SMREditor.GetExtendedJoints();

            int cnt = 0;
            for(int idx = 0; idx < parentJoint.childCount; ++idx )
            {
                Transform j = parentJoint.GetChild(idx);
                if( joints.Contains(j) )
                {
                    ++cnt;
                }
            }

            return cnt;
        }

        private void _Recur_SelectJointAndAllDescendants(Transform joint, bool select)
        {
            JointList extendedJoints = SMREditor.GetExtendedJoints();
            if (!extendedJoints.Contains(joint))
                return;

            if (select)
            {
                IncSelect(joint);
            }
            else
            {
                DecSelect(joint);
            }

            for (int idx = 0; idx < joint.childCount; ++idx)
            {
                Transform j = joint.GetChild(idx);
                _Recur_SelectJointAndAllDescendants(j, select);
            }
        }

        #endregion "private method"

	    #region "constant data"
        // constant data

        #endregion "constant data"
	}
}
