using System;
using System.Collections.Generic;
using UnityEngine;

namespace MH
{
    /// <summary>
    /// K-D tree, used to manage the vectorK into tree hierarchy
    /// </summary>
    [Serializable]
    public class KDTree : ISerializationCallbackReceiver
    {
        #region "data"

        [SerializeField][Tooltip("the serialized data")]
        private List<KDTreeSerial> m_serials;

        private KDTreeNode m_rootNode = null;

        #endregion

        #region "public methods"

        public bool IsValid { get { return m_rootNode != null; } }

        public void Invalidate()
        {
            m_serials.Clear();
            m_rootNode = null;
        }

        /// <summary>
        /// build the KDTree
        /// </summary>
        public void Build(Mesh m)
        {
            Build(m.vertices);
        }
        public void Build(Vector3[] lst)
        {
            m_rootNode = _Recur_Build(lst, 0, lst.Length, 0);
        }

        /// <summary>
        /// find the point nearest to 'pt'
        /// </summary>
        //public int m_visitNodeCnt = 0; //used to evaluate kdtree's GetNearest perf
        public Vector3 GetNearest(Vector3 pt)
        {
            //m_visitNodeCnt = 0;
            int depth = 0;
            float minDistSqr = float.MaxValue;
            KDTreeNode nearest = m_rootNode;
            KDTreeNode curNode = _Recur_FindLeafNode(pt, m_rootNode, ref minDistSqr, ref nearest, ref depth);
            _Recur_GetNearest(pt, curNode, ref minDistSqr, ref nearest, depth);

            //Dbg.Log("m_visitNodeCnt = {0}", m_visitNodeCnt);

            return nearest.pos;
        }

        #endregion "public methods"

        #region "private methods"

        private KDTreeNode _Recur_Build(Vector3[] pts, int from, int cnt, int depth)
        {
            if( cnt <= 0 )
                return null;

            int axis = depth % 3;

            _SortPoints(pts, from, cnt, axis);
            Vector3 median = pts[from + cnt / 2];

            KDTreeNode newNode = _MakeNode(median);
            newNode.left = _Recur_Build(pts, from, cnt / 2, depth + 1);
            newNode.right = _Recur_Build(pts, from + cnt / 2 + 1, cnt - cnt / 2 - 1, depth + 1);

            if (newNode.left != null) newNode.left.parent = newNode;
            if (newNode.right != null) newNode.right.parent = newNode;

            return newNode;
        }

        private KDTreeNode _MakeNode(Vector3 median)
        {
            var node = new KDTreeNode();
            node.pos = median;
            return node;
        }

        private void _SortPoints(Vector3[] pts, int from, int cnt, int axis)
        {
            Array.Sort(pts, from, cnt, V3Cmp.cmpers[axis]);
        }

        #region "GetNearest"
        		
        private KDTreeNode _Recur_FindLeafNode(Vector3 pt, KDTreeNode nd, ref float minDistSqr, ref KDTreeNode nearest, ref int depth)
        {
            KDTreeNode nextNd = null;
            while (true)
            {
                //m_visitNodeCnt++;
                int axis = depth % 3;

                float sqr = (pt - nd.pos).sqrMagnitude;
                if( sqr < minDistSqr )
                {
                    minDistSqr = sqr;
                    nearest = nd;
                }

                if (pt[axis] < nd.pos[axis])
                    nextNd = nd.left;
                else
                    nextNd = nd.right;

                if (nextNd == null)
                    return nd;
                else
                    nd = nextNd;

                ++depth;
            }
        }

        private void _Recur_GetNearest(Vector3 pt, KDTreeNode nd, ref float minDistSqr, ref KDTreeNode nearest, int depth)
        {
            //check nearest
            while (nd != null)
            {
                float sqr = (pt - nd.pos).sqrMagnitude;
                if (sqr < minDistSqr)
                {
                    minDistSqr = sqr;
                    nearest = nd;
                }
                //++m_visitNodeCnt;

                KDTreeNode otherSideNode = _NeedCheckOtherSide(pt, nd.parent, minDistSqr, depth - 1);
                if (otherSideNode != null)
                    _Recur_CheckNearestDownward(pt, nd.parent, ref minDistSqr, ref nearest, depth - 1);

                nd = nd.parent;
                --depth; 
            }            
        }

        private void _Recur_CheckNearestDownward(Vector3 pt, KDTreeNode nd, ref float minDistSqr, ref KDTreeNode nearest, int depth)
        {
            //check nearest
            float sqr = (pt - nd.pos).sqrMagnitude;
            if (sqr < minDistSqr)
            {
                minDistSqr = sqr;
                nearest = nd;
            }
            //++m_visitNodeCnt;

            int axis = depth % 3;
            float ptv = pt[axis];
            float ndv = nd.pos[axis];

            float sqr_ptv_ndv = (ptv - ndv) * (ptv - ndv);

            if( nd.left != null && (ptv < ndv || sqr_ptv_ndv < minDistSqr) )
                _Recur_CheckNearestDownward(pt, nd.left, ref minDistSqr, ref nearest, depth+1);
            if( nd.right != null && (ptv > ndv || sqr_ptv_ndv < minDistSqr) )
                _Recur_CheckNearestDownward(pt, nd.right, ref minDistSqr, ref nearest, depth+1);
        }

        private KDTreeNode _NeedCheckOtherSide(Vector3 pt, KDTreeNode nd, float minDistSqr, int depth)
        {
            if (nd == null)
                return null;

            int axis = depth % 3;
            Vector3 ndPos = nd.pos;

            if( (pt[axis] - ndPos[axis]) * (pt[axis] - ndPos[axis]) > minDistSqr )
                return null; //not crossing the separating plane, return false

            if (pt[axis] < ndPos[axis])
            {
                return nd.right;
            }
            else 
            {
                return nd.left;
            }
        }

        #endregion "GetNearest"

        #endregion "private methods"
         
        #region "ISerializationCallbackReceiver impl."

        public void OnBeforeSerialize()
        {
            if (m_serials == null)
                m_serials = new List<KDTreeSerial>();
            m_serials.Clear();

            if( m_rootNode != null )
                _Recur_Serialize(m_rootNode);
        }

        private int _Recur_Serialize(KDTreeNode nd)
        {
            KDTreeSerial s = new KDTreeSerial();

            if( nd.left != null ) s.leftIdx = _Recur_Serialize(nd.left);
            if (nd.right != null) s.rightIdx = _Recur_Serialize(nd.right);

            s.pos = nd.pos;
            m_serials.Add(s);
            int curId = m_serials.Count - 1;

            if (s.leftIdx >= 0) m_serials[s.leftIdx].parentIdx = curId;
            if (s.rightIdx >= 0) m_serials[s.rightIdx].parentIdx = curId;

            return curId;
        }

        public void OnAfterDeserialize()
        {
            if (m_serials == null || m_serials.Count == 0)
                return;

            List<KDTreeNode> nodes = new List<KDTreeNode>();

            for (int i = 0; i < m_serials.Count; ++i)
            { //populate all KDTreeNode first
                KDTreeSerial s = m_serials[i];
                KDTreeNode node = new KDTreeNode();
                node.pos = s.pos;
                nodes.Add(node);
            }
            for (int i = 0; i < nodes.Count; ++i)
            { //recover the links
                int l = m_serials[i].leftIdx;
                nodes[i].left = l >= 0 ? nodes[l] : null;
                int r = m_serials[i].rightIdx;
                nodes[i].right = r >= 0 ? nodes[r] : null;
                int p = m_serials[i].parentIdx;
                nodes[i].parent = p >= 0 ? nodes[p] : null;
            }

            m_rootNode = nodes[nodes.Count-1]; //post-order serialization
        }
        		
        #endregion 

        public class V3Cmp : IComparer<Vector3>
        {
            public static V3Cmp[] cmpers = new V3Cmp[] { new V3Cmp(0), new V3Cmp(1), new V3Cmp(2) };

            private int axis;

            public V3Cmp(int axis) { this.axis = axis; }

            public int Compare(Vector3 lhs, Vector3 rhs)
            {
                return Math.Sign(lhs[axis] - rhs[axis]);
            }
        }
    }

    /// <summary>
    /// the tree node
    /// </summary>
    public class KDTreeNode
    {
        public Vector3 pos;
        public KDTreeNode left;
        public KDTreeNode right;
        public KDTreeNode parent;
    }

    /// <summary>
    /// the data-structure used to serialize KDTree
    /// </summary>
    [Serializable]
    public class KDTreeSerial
    {
        public Vector3 pos;
        public int leftIdx = -1;
        public int rightIdx = -1;
        public int parentIdx = -1;
    }

    
}
