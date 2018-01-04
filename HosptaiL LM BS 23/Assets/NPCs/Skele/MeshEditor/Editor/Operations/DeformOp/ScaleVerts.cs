using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using MH.MeshEditor;

namespace MH
{
namespace MeshOp
{
    using VLst = System.Collections.Generic.List<int>;

    /// <summary>
    /// this op will scale the verts based on the pivot pos
    /// </summary>
    class ScaleVerts
    {
	    #region "data"
        // data

        private EditableMesh m_Mesh;
        private Pivotor m_Pivot;
        private Transform m_Tr;

        #endregion "data"

	    #region "public method"
        // public method

        public ScaleVerts(){}

        public void Init(EditableMesh m, Pivotor pivot)
        {
            m_Mesh = m;
            m_Pivot = pivot;
            m_Tr = m_Mesh.transform;
        }

        public void Fini()
        {

        }

        /// <summary>
        /// scale specified verts based on pivot (parameter in world space)
        /// a affine transformation(?)
        /// </summary>
        /// <param name="vertIdxLst">the list containing index to all affected verts</param>
        /// <param name="worldScaleDelta">the scale info in world space</param>
        public void ExecuteWorld(VLst vertIdxLst, Vector3 worldFrom, Vector3 worldTo, Vector3 worldPivotPos)
        {
            Vector3 worldScaleDelta = V3Ext.DivideComp(worldTo, worldFrom); //when I say "world", it's not bound to be global orientation
            Matrix4x4 combinedMat = _CalcCombinedMat(ref worldScaleDelta);

            //Vector3 localPivotPos = m_Pivot.ModelPos; //this will accumulate errors 
            Vector3 localPivotPos = m_Tr.InverseTransformPoint(worldPivotPos);

            // apply change to verts
            Mesh m = m_Mesh.mesh;
            Vector3[] vertsArray = m.vertices;
            for(int i=0; i<vertIdxLst.Count; ++i)
            {
                int vidx = vertIdxLst[i];
                Vector3 off = vertsArray[vidx] - localPivotPos;
                Vector3 newOff = combinedMat.MultiplyPoint(off);
                vertsArray[vidx] = localPivotPos + newOff;
                //Dbg.Log("newOff={0}, off={1}, verts={2}, mat=\n{3}", newOff.ToString("F6"), off.ToString("F6"), vertsArray[vidx].ToString("F6"), combinedMat.ToString("F8"));
            }

            // apply to mesh
            UndoMesh.SetVertices(m, vertsArray);
        }

        public void ExecuteWorldSoft(SoftSelection softSel, Vector3 worldFrom, Vector3 worldTo, Vector3 worldPivotPos)
        {
            //Dbg.Log("ScaleVerts.ExecuteWorldSoft: from: {0}, to: {1}, revert: {2}", worldFrom.ToString("F3"), worldTo.ToString("F3"), bRevert);
            Vector3 worldScaleDelta = V3Ext.DivideComp(worldTo, worldFrom); //when I say "world", it's not bound to be global orientation

            Matrix4x4 mat, matI;
            _GetMatrices(out mat, out matI);

            //Vector3 localPivotPos = m_Pivot.ModelPos; //this will accumulate errors
            Vector3 localPivotPos = m_Tr.InverseTransformPoint(worldPivotPos);

            softSel.Prepare();

            // apply change to verts
            Mesh m = m_Mesh.mesh;
            Vector3[] cachedVertsArray = softSel.CachedVertPos;
            Vector3[] vertsArray = (Vector3[])cachedVertsArray.Clone();
            VLst softSelIdxLst = softSel.GetEffectVerts();

            //Vector3[] curVertArray = MeshCache.Instance.vertices;
            //Dbg.Log("modelPivotPos = {0}, {1}", localPivotPos.ToString("F3"), Misc.ListToString(softSelIdxLst, idx => { return curVertArray[idx].ToString("F3"); } ));

            // apply scaling
            for (int i = 0; i < softSelIdxLst.Count; ++i)
            {
                int vidx = softSelIdxLst[i];
                Vector3 off = vertsArray[vidx] - localPivotPos;

                // calc scale mat
                Vector3 percScale;
                float percentage = softSel.GetPercentage(vidx);
                percScale = Vector3.Lerp(Vector3.one, worldScaleDelta, percentage);
                
                Matrix4x4 matScalePerc = Matrix4x4.Scale(percScale);
                Matrix4x4 combinedMat = (mat * matScalePerc * matI);
                Vector3 newOff = combinedMat.MultiplyPoint(off);

                vertsArray[vidx] = localPivotPos + newOff;

                //Dbg.Log("newOff={0}, off={1}, verts={2}, mat=\n{3}", newOff.ToString("F6"), off.ToString("F6"), vertsArray[vidx].ToString("F6"), combinedMat.ToString("F8"));
            }

            // apply to mesh
            UndoMesh.SetVertices(m, vertsArray);

            //Dbg.Log("=======================");
        }

        #endregion "public method"

	    #region "private method"
        private Matrix4x4 _CalcCombinedMat(ref Vector3 worldScaleDelta)
        {
            Matrix4x4 mat, matI;
            _GetMatrices(out mat, out matI);

            Matrix4x4 matScale = Matrix4x4.Scale(worldScaleDelta);
            Matrix4x4 combinedMat = mat * matScale * matI;
            return combinedMat;
        }

        private void _GetMatrices(out Matrix4x4 mat, out Matrix4x4 matI)
        {
            Quaternion worldRot = m_Pivot.WorldRot;
            Vector3 worldFwd = worldRot * Vector3.forward;
            Vector3 worldUp = worldRot * Vector3.up;
            Vector3 worldRight = worldRot * Vector3.right;

            Transform meshTr = m_Mesh.transform;
            Vector3 localFwd = meshTr.InverseTransformDirection(worldFwd);
            Vector3 localUp = meshTr.InverseTransformDirection(worldUp);
            Vector3 localRight = meshTr.InverseTransformDirection(worldRight);

            // prepare matrix
            mat = Matrix4x4.identity;
            mat.SetColumn(0, localRight); // NOTE: w component is set to 0
            mat.SetColumn(1, localUp);
            mat.SetColumn(2, localFwd);
            matI = mat.inverse;
        }

        // private method

        #endregion "private method"

	    #region "constant data"
        // constant data

        #endregion "constant data"
    }
}
}
