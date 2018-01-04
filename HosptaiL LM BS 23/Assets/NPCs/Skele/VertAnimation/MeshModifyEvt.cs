using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MH
{
	public class MeshModifyEvt
	{
        public delegate void MeshModified();
        public static event MeshModified evtMeshModified;

        public static void FireEvent()
        {
            if(evtMeshModified != null)
            {
                evtMeshModified();
            }
        }

        public static void AddDele(MeshModified del)
        {
            evtMeshModified += del;
        }

        public static void DelDele(MeshModified del)
        {
            evtMeshModified -= del;
        }
	}
}
