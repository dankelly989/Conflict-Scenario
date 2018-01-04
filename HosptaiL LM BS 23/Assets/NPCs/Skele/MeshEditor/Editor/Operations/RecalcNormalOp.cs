using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH
{
namespace MeshOp
{
    class RecalcNormalOp
    {
	    #region "data"
        // data

        private EditableMesh m_Mesh;

        #endregion "data"

	    #region "public method"
        // public method

        public RecalcNormalOp() { }
        public void Init(EditableMesh m)
        {
            m_Mesh = m;
        }
        public void Fini()
        {

        }

        public void Execute()
        {
            Mesh m = m_Mesh.mesh;
            Undo.RecordObject(m, "Recalc MeshNormal");
            m.RecalculateNormals();
            m.RecalculateBounds(); //prevent disappearing
            EUtil.GetSceneView().Repaint();
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
