using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH
{
    using UndoDict = System.Collections.Generic.Dictionary<UnityEngine.Mesh, MeshUndoer>;

    // utility class to handle with MeshUndoer
	class UndoMesh
	{
        private static UndoDict ms_UndoDict = new UndoDict();

	    #region "public methods"
	    // "public methods" 

        public static void SetEnableRecord(Mesh m, bool bEnable)
        {
            MeshUndoer undoer = _ForceGetUndoer(m);
            undoer.SetEnableRecord(bEnable);
        }

        public static void SetVertices(Mesh m, Vector3[] verts)
        {
            MeshUndoer undoer = _ForceGetUndoer(m);
            undoer.SetVerts(verts);
        }

        public static bool DeleteEntry(Mesh m)
        {
            return ms_UndoDict.Remove(m);
        }
	
	    #endregion "public methods"
        
	    #region "private methods"
	    // "private methods" 

        private static MeshUndoer _ForceGetUndoer(Mesh m)
        {
            MeshUndoer undoer = null;
            if (!ms_UndoDict.TryGetValue(m, out undoer))
            {
                undoer = ScriptableObject.CreateInstance<MeshUndoer>();
                undoer.Init(m);
                ms_UndoDict.Add(m, undoer);
            }
            return undoer;
        }
	
	    #endregion "private methods"
        
	}
}
