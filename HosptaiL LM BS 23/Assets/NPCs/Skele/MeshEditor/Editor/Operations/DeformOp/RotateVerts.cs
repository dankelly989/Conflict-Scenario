using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH
{
namespace MeshOp
{
    using VLst = System.Collections.Generic.List<int>;
    class RotateVerts
    {
	    #region "data"
        // data
        private EditableMesh m_Mesh;
        private Pivotor m_Pivot;

        #endregion "data"

	    #region "public method"
        // public method
        public RotateVerts(){}
        public void Init(EditableMesh mesh, Pivotor pivot)
        {
            m_Mesh = mesh;
            m_Pivot = pivot;
        }
        public void Fini()
        {

        }

        /// <summary>
        /// rotate selected verts by rotation
        /// </summary>
        /// <param name="vertIdxLst">the list containing index to all affected verts</param>
        /// <param name="modelPivotPos">the position of pivot in local space</param>
        /// <param name="modelRotOff">the rotation in model's local space</param>
        public void Execute(VLst vertIdxLst, Quaternion modelRotOff)
        {
            Vector3 modelPivotPos = m_Pivot.ModelPos;
            Mesh m = m_Mesh.mesh;
            Vector3[] vertsArray = m.vertices;

            for (int i = 0; i < vertIdxLst.Count; ++i)
            {
                int vidx = vertIdxLst[i];
                Vector3 offVec = vertsArray[vidx] - modelPivotPos;
                Vector3 transOff = modelRotOff * offVec;
                vertsArray[vidx] = transOff + modelPivotPos;
            }

            UndoMesh.SetVertices(m, vertsArray);
        }

        public void ExecuteSoft(SoftSelection softSel, Quaternion modelRotOff)
        {
            softSel.Prepare();

            Vector3 modelPivotPos = m_Pivot.ModelPos;
            Mesh m = m_Mesh.mesh;
            //Vector3[] cachedVertsArray = softSel.CachedVertPos;
            //Vector3[] vertsArray = (Vector3[])cachedVertsArray.Clone();
            Vector3[] vertsArray = m.vertices;

            VLst softSelIdxLst = softSel.GetEffectVerts();

            for (int i = 0; i < softSelIdxLst.Count; ++i)
            {
                int vidx = softSelIdxLst[i];
                float percentage = softSel.GetPercentage(vidx);

                Vector3 offVec = vertsArray[vidx] - modelPivotPos;
                Quaternion percModelRotoff = Quaternion.Slerp(Quaternion.identity, 
                    modelRotOff, percentage); // interpolated rotation

                Vector3 transOff = percModelRotoff * offVec;
                vertsArray[vidx] = transOff + modelPivotPos;
            }

            UndoMesh.SetVertices(m, vertsArray);
        }       

        public void ExecuteWorld(VLst vertIdxLst, Quaternion worldFrom, Quaternion worldTo)
        {
            Quaternion localFrom;
            Quaternion localTo;
            _CalcLocalRotations(ref worldFrom, ref worldTo, out localFrom, out localTo);

            Quaternion localOff = localTo * Quaternion.Inverse(localFrom);
            Execute(vertIdxLst, localOff);
        }

        public void ExecuteWorldSoft(SoftSelection softSel, Quaternion worldFrom, Quaternion worldTo)
        {
            Quaternion localFrom;
            Quaternion localTo;
            _CalcLocalRotations(ref worldFrom, ref worldTo, out localFrom, out localTo);
            
            Quaternion localOff = localTo * Quaternion.Inverse(localFrom);
            ExecuteSoft(softSel, localOff);
        }


        #endregion "public method"

	    #region "private method"
        // private method

        private void _CalcLocalRotations(ref Quaternion worldFrom, ref Quaternion worldTo, out Quaternion localFrom, out Quaternion localTo)
        {
            Transform meshTr = m_Mesh.transform;
            localFrom = Quaternion.identity;
            {
                Vector3 w0 = worldFrom * Vector3.forward;
                Vector3 w1 = worldFrom * Vector3.up;
                Vector3 m0 = meshTr.InverseTransformDirection(w0);
                Vector3 m1 = meshTr.InverseTransformDirection(w1);
                localFrom = Quaternion.LookRotation(m0, m1);
            }

            localTo = Quaternion.identity;
            {
                Vector3 w0 = worldTo * Vector3.forward;
                Vector3 w1 = worldTo * Vector3.up;
                Vector3 m0 = meshTr.InverseTransformDirection(w0);
                Vector3 m1 = meshTr.InverseTransformDirection(w1);
                localTo = Quaternion.LookRotation(m0, m1);
            }
        }

        #endregion "private method"

	    #region "constant data"
        // constant data

        #endregion "constant data"


    }
}
}
