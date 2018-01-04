//#define HAS_POOL

using System;
using System.Collections.Generic;
using ExtMethods;

namespace MH
{
    /// <summary>
    /// use adj-list to represent a directional/non-directional graph
    /// </summary>
	public class Graph<V> 
	{
	    #region "data"
        // data

        private Dictionary<V, GraphNode<V>> m_d2n = new Dictionary<V, GraphNode<V>>(); //data 2 node

        private Dictionary<GraphNode<V>, List<GraphNode<V>>> m_node2neighbors =
            new Dictionary<GraphNode<V>, List<GraphNode<V>>>(); //node 2 all neighbors

        #endregion "data"

	    #region "public method"
        // public method

	    #region "structure maintainence"
        /// <summary>
        /// if already has the data, return the node;
        /// else create new node and return
        /// </summary>Gra
        public GraphNode<V> AddData(V data)
        {
            GraphNode<V> node = GetNode(data);
            if( null == node )
            {
                node = GraphNode<V>.CreateNode(data);
                m_d2n.Add(data, node);
                m_node2neighbors.Add(node, _CreateList());
            }

            return node;
        }

        /// <summary>
        /// return corresponding node, or null if not present
        /// </summary>
        public GraphNode<V> GetNode(V data)
        {
            return m_d2n.TryGet(data);
        }

        public bool Contains(V data)
        {
            return m_d2n.ContainsKey(data);
        }

        /// <summary>
        /// call DelNode( correspondingNode )
        /// if not found return false;
        /// </summary>
        public bool DelData(V data)
        {
            GraphNode<V> node = GetNode(data);
            if (node == null) 
                return false;

            return DelNode(node);
        }

        /// <summary>
        /// 1. enum the neighbor list, remove the node from neighbor's list
        /// 2. remove the node's entry from m_node2neighbors
        /// 3. remove entry from m_d2n
        /// </summary>
        public bool DelNode(GraphNode<V> node)
        {
            List<GraphNode<V>> thisLst = m_node2neighbors.TryGet(node);
            if (null == thisLst)
                return false;

            //1
            for( var i = thisLst.GetEnumerator(); i.MoveNext(); )
            {
                var neighNode = i.Current; //neighbor node
                List<GraphNode<V>> nLst = m_node2neighbors[neighNode];
                nLst.Remove(node);
            }

            //2 
            m_node2neighbors.Remove(node);

            //3
            m_d2n.Remove(node.data);

            //4
            _DeleteList(thisLst);
            GraphNode<V>.DeleteNode(node);

            return true;            
        }

        /// <summary>
        /// if node not created, then create it;
        /// if adding success, return true;
        /// </summary>
        public bool AddConnBi(V lhs, V rhs)
        {
            GraphNode<V> ln = AddData(lhs);
            GraphNode<V> rn = AddData(rhs);

            return AddConnBi(ln, rn);
        }

        public bool AddConnBi(GraphNode<V> lnode, GraphNode<V> rnode)
        {
            List<GraphNode<V>> llst = m_node2neighbors.TryGet(lnode);
            if (llst.Contains(rnode))
                return false;
            llst.Add(rnode);

            List<GraphNode<V>> rlst = m_node2neighbors.TryGet(rnode);
            Dbg.Assert(!rlst.Contains(lnode), "Graph.AddConn: data corruption, rlst contains lnode already");
            rlst.Add(lnode);

            return true;
        }

        /// <summary>
        /// add uni-directional link from lhs -> rhs
        /// </summary>
        public bool AddConnUni(V lhs, V rhs)
        {
            GraphNode<V> ln = AddData(lhs);
            GraphNode<V> rn = AddData(rhs);

            return AddConnUni(ln, rn);
        }

        public bool AddConnUni(GraphNode<V> lnode, GraphNode<V> rnode)
        {
            List<GraphNode<V>> llst = m_node2neighbors.TryGet(lnode);
            if (llst.Contains(rnode))
                return false;
            llst.Add(rnode);
            return true;

        }

        public bool DelConnBi(V lhs, V rhs)
        {
            GraphNode<V> ln = AddData(lhs);
            GraphNode<V> rn = AddData(rhs);

            return DelConnBi(ln, rn);
        }

        /// <summary>
        /// remove the connection between lnode & rnode
        /// </summary>
        public bool DelConnBi(GraphNode<V> lnode, GraphNode<V> rnode)
        {
            List<GraphNode<V>> llst = m_node2neighbors.TryGet(lnode);
            bool found = llst.Remove(rnode);
            if (!found)
                return false;

            List<GraphNode<V>> rlst = m_node2neighbors.TryGet(rnode);
            found = rlst.Remove(lnode);
            Dbg.Assert(found, "Graph.DelConn: data corruption, rlst doesn't contain lnode");

            return true;
        }

        public bool DelConnUni(V lhs, V rhs)
        {
            GraphNode<V> ln = AddData(lhs);
            GraphNode<V> rn = AddData(rhs);

            return DelConnUni(ln, rn);
        }

        public bool DelConnUni(GraphNode<V> lnode, GraphNode<V> rnode)
        {
            List<GraphNode<V>> llst = m_node2neighbors.TryGet(lnode);
            bool found = llst.Remove(rnode);
            return found;
        }

        public Dictionary<V, GraphNode<V>>.Enumerator GetNodeEnumerator()
        {
            return m_d2n.GetEnumerator();
        }

        public DataEnumerator GetEnumerator()
        {
            return new DataEnumerator(m_d2n.GetEnumerator());
        }

        public List<GraphNode<V>> GetAdjList_Internal(V data)
        {
            GraphNode<V> lnode = GetNode(data);
            Dbg.Assert(lnode != null, "Graph.GetAdjList_Internal: cannot get node");
            List<GraphNode<V>> llst = m_node2neighbors.TryGet(lnode);
            return llst;
        }

        public void GetAdjList(V data, List<V> outLst)
        {
            GraphNode<V> lnode = GetNode(data);
            Dbg.Assert(lnode != null, "Graph.GetAdjList: cannot get node");
            List<GraphNode<V>> llst = m_node2neighbors.TryGet(lnode);
            for( var i = llst.GetEnumerator(); i.MoveNext(); )
            {
                outLst.Add(i.Current.data);
            }
        }

	    #endregion "structure maintainence"

	    #region "alg"

        /// <summary>
        /// if any of the data is not in graph, return false;
        /// </summary>
        public bool IsAdjacentUni(V lhs, V rhs)
        {
            GraphNode<V> lnode = GetNode(lhs);
            if (lnode == null) return false;
            GraphNode<V> rnode = GetNode(rhs);
            if (rnode == null) return false;

            return IsAdjacentUni(lnode, rnode);
        }

        public bool IsAdjacentUni(GraphNode<V> lnode, GraphNode<V> rnode)
        {
            List<GraphNode<V>> llst = m_node2neighbors.TryGet(lnode);
            return llst.Contains(rnode);
        }

        public bool IsAdjacentBi(V lhs, V rhs)
        {
            GraphNode<V> lnode = GetNode(lhs);
            if (lnode == null) return false;
            GraphNode<V> rnode = GetNode(rhs);
            if (rnode == null) return false;

            return IsAdjacentBi(lnode, rnode);
        }

        public bool IsAdjacentBi(GraphNode<V> lnode, GraphNode<V> rnode)
        {
            return IsAdjacentUni(lnode, rnode) && IsAdjacentUni(rnode, lnode);
        }
        /// <summary>
        /// check if two node has a path connected
        /// </summary>
        public bool IsConnected(V lhs, V rhs)
        {
            GraphNode<V> lnode = GetNode(lhs);
            if (lnode == null) return false;
            GraphNode<V> rnode = GetNode(rhs);
            if (rnode == null) return false;

            return IsConnected(lnode, rnode);
        }

        public bool IsConnected(GraphNode<V> lnode, GraphNode<V> rnode)
        {
            List<GraphNode<V>> llst = m_node2neighbors.TryGet(lnode);
            if (llst.Contains(rnode))
                return true;

            for(var i = llst.GetEnumerator(); i.MoveNext(); )
            {
                var cnode = i.Current;
                if (IsConnected(cnode, rnode))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// get all nodes that are connected to the srcNode, including srcNode itself
        /// </summary>
        public void GetAllConnectedTo(GraphNode<V> srcNode, HashSet<GraphNode<V>> outSet)
        {
            int nodeCnt = m_d2n.Count;
            GraphNode<V> curNode = srcNode;
            outSet.Add(srcNode);
            
            List<GraphNode<V>> neighborLst = m_node2neighbors.TryGet(curNode);
            outSet.UnionWith(neighborLst);
            if (outSet.Count == nodeCnt)
                return;

            for( var i = neighborLst.GetEnumerator(); i.MoveNext(); )
            {
                var nnode = i.Current;
                GetAllConnectedTo(nnode, outSet);
                if (outSet.Count == nodeCnt)
                    return;
            }
        }
	
	    #endregion "alg"

        #endregion "public method"

	    #region "private method"
        // private method

        private List<GraphNode<V>> _CreateList()
        {
            return new List<GraphNode<V>>();
        }

        private void _DeleteList(List<GraphNode<V>> lst)
        {
            // nothing
        }

        #endregion "private method"

	    #region "constant data"
        // constant data

        #endregion "constant data"

	    #region "enumerator"
	    // "enumerator" 

        public struct DataEnumerator
        {
            private Dictionary<V, GraphNode<V>>.Enumerator m_d2nEnumerator;

            public DataEnumerator(Dictionary<V, GraphNode<V>>.Enumerator de)
            {
                m_d2nEnumerator = de;
            }

            public bool MoveNext()
            {
                return m_d2nEnumerator.MoveNext();
            }

            public V Current
            {
                get { return m_d2nEnumerator.Current.Key; }
            }
        }
	
	    #endregion "enumerator"
	}

    public class GraphNode<V>
    {
        private V m_data;

        public static GraphNode<V> CreateNode(V data) { return new GraphNode<V>(data); }
        public static void DeleteNode(GraphNode<V> node) { } //do nothing for now
        public GraphNode(V data) { m_data = data; }

        public V data {
            get { return m_data; }
            set { m_data = value; }
        }
    }

}
