using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH
{
namespace MeshOp
{
    using VLst = System.Collections.Generic.List<int>;
    /// <summary>
    /// this op just moves the verts
    /// </summary>
    class MoveVerts
    {
	    #region "data"
        // data

        private EditableMesh m_Mesh;
        //private Pivotor m_Pivot;

        #endregion "data"

	    #region "public method"
        // public method
        public MoveVerts() { }

        public void Init(EditableMesh m)
        {
            m_Mesh = m;
            //m_Pivot = p;
        }

        public void Fini()
        {

        }

        /// <summary>
        /// move specified verts by offset
        /// </summary>
        /// <param name="vertIdxLst">the list containing index to all affected verts</param>
        /// <param name="modelSpaceOffset">the offset in model's local space</param>
        public void Execute(VLst vertIdxLst, Vector3 modelSpaceOffset)
        {
            Mesh m = m_Mesh.mesh;
            Vector3[] vertsArray = m.vertices;

            for (int i = 0; i < vertIdxLst.Count; ++i )
            {
                int vidx = vertIdxLst[i];
                vertsArray[vidx] += modelSpaceOffset;
            }

            UndoMesh.SetVertices(m, vertsArray);
        }

        public void ExecuteWorld(VLst vertIdxLst, Vector3 worldFrom, Vector3 worldTo)
        {
            Transform meshTr = m_Mesh.transform;
            Vector3 localStartPos = meshTr.InverseTransformPoint(worldFrom);
            Vector3 localNewPos = meshTr.InverseTransformPoint(worldTo);
            Vector3 localOff = localNewPos - localStartPos;

            Execute(vertIdxLst, localOff);
        }

        /// <summary>
        /// move specified verts by offset
        /// the base shape is cached in softSel
        /// </summary>
        public void ExecuteSoft(SoftSelection softSel, Vector3 modelSpaceOffset)
        {
            softSel.Prepare(); // will do calculation IF NEEDED

            Mesh m = m_Mesh.mesh;
            Vector3[] cachedVertsArray = softSel.CachedVertPos;
            Vector3[] vertsArray = (Vector3[])cachedVertsArray.Clone();

            VLst softSelIdxLst = softSel.GetEffectVerts();

            for (int i = 0; i < softSelIdxLst.Count; ++i)
            {
                int vidx = softSelIdxLst[i];
                float percentage = softSel.GetPercentage(vidx);
                vertsArray[vidx] += modelSpaceOffset * percentage;
            }

            UndoMesh.SetVertices(m, vertsArray);
        }

        public void ExecuteWorldSoft(SoftSelection softSel, Vector3 worldFrom, Vector3 worldTo)
        {
            Transform meshTr = m_Mesh.transform;
            Vector3 localStartPos = meshTr.InverseTransformPoint(worldFrom);
            Vector3 localNewPos = meshTr.InverseTransformPoint(worldTo);
            Vector3 localOff = localNewPos - localStartPos;

            ExecuteSoft(softSel, localOff);
        }

        #endregion "public method"

	    #region "private method"
        // private method

        #endregion "private method"

	    #region "constant data"
        // constant data

        #endregion "constant data"
    }
}	
}
