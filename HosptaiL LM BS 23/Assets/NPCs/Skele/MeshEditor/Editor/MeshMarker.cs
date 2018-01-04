using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

using MH.MeshEditor;

namespace MH
{
    using RVLst = System.Collections.Generic.List<int>;

    /// <summary>
    /// used to mark vert/edge/tri
    /// </summary>
	class MeshMarker
	{
	    #region "data"
        // data

        private MeshSelection m_Selection;
        private EditableMesh m_EditMesh;

        private VertMarker m_VertMarker;
        private EdgeMarker m_EdgeMarker;
        private TriMarker m_TriMarker;

        private bool m_Dirty = false;

        private int[] m_EdgeIndices;

        #endregion "data"

	    #region "public method"
        // public method

        public MeshMarker(){}

        public void Init(MeshSelection s, EditableMesh m)
        {
            m_Selection = s;
            m_EditMesh = m;

            // connect events
            m_Selection.evtSelectionChanged += this.OnSelectionChanged;
            MeshUndoer.AddDeleMeshModified(this.OnMeshModified);

            m_VertMarker = new VertMarker();
            Material vertBillboardMat = AssetDatabase.LoadAssetAtPath(VertBillboardMatPath, typeof(Material)) as Material;
            Dbg.Assert(vertBillboardMat != null, "MeshMaker.Init: failed to load vertBillboard Material at {0}", VertBillboardMatPath);
            m_VertMarker.Init(vertBillboardMat, 0);

            m_EdgeMarker = new EdgeMarker();
            Material edgeMat = AssetDatabase.LoadAssetAtPath(EdgeMatPath, typeof(Material)) as Material;
            Dbg.Assert(edgeMat != null, "MeshMaker.Init: failed to load edge Material at {0}", EdgeMatPath);
            m_EdgeMarker.Init(edgeMat);
            _PrepareEdgeIndices();

            m_TriMarker = new TriMarker();
            m_TriMarker.Init();

            EUtil.SetEnableWireframe(m.renderer, false);
            //EditorUtility.SetSelectedWireframeHidden(m.renderer, true); //hide the wireframe
        }

        public void Fini()
        {
            //EditorUtility.SetSelectedWireframeHidden(m_EditMesh.renderer, false); //show the wireframe
            EUtil.SetEnableWireframe(m_EditMesh.renderer, true);

            m_TriMarker.Fini();
            m_TriMarker = null;

            m_EdgeMarker.Fini();
            m_EdgeMarker = null;

            m_VertMarker.Fini();
            m_VertMarker = null;

            MeshUndoer.DelDeleMeshModified(this.OnMeshModified);
            m_Selection.evtSelectionChanged -= this.OnSelectionChanged;
        }

        /// <summary>
        /// set vertex marker size (in px)
        /// </summary>
        public void SetVertSize(float px)
        {
            m_VertMarker.SetPointSize(px);
            Dirty = true;
        }
        
        /// <summary>
        /// update the markers, if not dirty, do nothing;
        /// 
        /// will retrieve info from Mesh & Selection & other related external data-structures
        /// </summary>
        public void UpdateMarkers()
        {
            if (!Dirty)
                return;

            //Mesh m = m_EditMesh.mesh;
            //Transform meshTr = m_EditMesh.transform;
            //Vector3[] verts = MeshCache.Instance.vertices;

            VMesh vmesh = VMesh.Instance;

            // vert positions
            VVert[] allVVerts = vmesh.GetAllVVerts();
            int vcnt = allVVerts.Length;
            Vector3[] allVVertPos = new Vector3[vcnt];
            for (int i = 0; i < vcnt; ++i)
            {
                allVVertPos[i] = allVVerts[i].GetWorldPos();
            }

            // colors 
            Color32[] colors = new Color32[vcnt];
            for (int i = 0; i < vcnt; ++i)
            {
                VVert oneVVert = allVVerts[i];
                if (m_Selection.IsSelectedVert(oneVVert.RepVert))
                {
                    colors[i] = SelectedVertColor;
                }
                else
                {
                    colors[i] = NonSelectedVertColor;
                }
            }

            // set to mesh(vert)
            m_VertMarker.SetVerts(allVVertPos); //indices are set altogether with vertices
            m_VertMarker.SetColors(colors);

            // set to mesh(edge)
            m_EdgeMarker.SetVerts(allVVertPos);
            m_EdgeMarker.SetColors(colors);
            m_EdgeMarker.SetIndices(m_EdgeIndices);
            
            Dirty = false;
        }

        public void OnDraw()
        {
#if false
            var topo = m_Selection.Prim;
            switch (topo)
            {
                case MeshTopology.Points:
                    {
                        m_VertMarker.Draw();
                    }
                    break;
                case MeshTopology.Lines:
                    {

                    }
                    break;
                case MeshTopology.Triangles:
                    {

                    }
                    break;
                default:
                    Dbg.LogErr("MeshMarker.OnDraw: unexpected topology: {0}", topo);
                    break;
            }
#endif
            m_VertMarker.Draw();
            m_EdgeMarker.Draw();
        }

	    #region "callbacks"
	    // "callbacks" 

        public void OnMeshModified()
        {
            Dirty = true;
        }

        public void OnSelectionChanged()
        {
            Dirty = true;
        }
	
	    #endregion "callbacks"

        #endregion "public method"

	    #region "Prop"
	    // "Prop" 

        public bool Dirty
        {
            get { return m_Dirty; }
            set { m_Dirty = value; }
        }
	
	    #endregion "Prop"

	    #region "private method"
        // private method

        private void _PrepareEdgeIndices()
        {
            VMesh vmesh = VMesh.Instance;
            VVert[] allVVerts = vmesh.GetAllVVerts();
            VEdge[] allVEdges = vmesh.GetAllVActiveEdges();

            Dictionary<VVert, int> vert2idx = new Dictionary<VVert, int>();
            for(int i=0; i<allVVerts.Length; ++i)
            {
                VVert v = allVVerts[i];
                vert2idx[v] = i;
            }
            
            m_EdgeIndices = new int[allVEdges.Length * 2];

            for(int i=0; i<allVEdges.Length; i++)
            {
                VEdge e = allVEdges[i];
                VVert v0 = e.GetVVert(0);
                VVert v1 = e.GetVVert(1);
                int vidx0 = vert2idx[v0];
                int vidx1 = vert2idx[v1];

                m_EdgeIndices[i * 2] = vidx0;
                m_EdgeIndices[i * 2 + 1] = vidx1;
            }
        }

        
        #endregion "private method"

	    #region "constant data"
        // constant data

        private const string VertBillboardMatPath = "Assets/Skele/MeshEditor/Editor/MeshUtil/VertBillboard.mat";
        private const string EdgeMatPath = "Assets/Skele/MeshEditor/Editor/MeshUtil/EdgeMat.mat";

        private readonly static Color32 NonSelectedVertColor = new Color32(0, 0, 0, 255);
        private readonly static Color32 SelectedVertColor = new Color32(255, 160, 0, 255);

        #endregion "constant data"
	}
}
