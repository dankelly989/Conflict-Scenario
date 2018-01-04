using System;
using System.Collections.Generic;
using UnityEngine;

namespace MH
{
namespace MeshEditor
{
    class VMeshUtil
    {
        /// <summary>
        /// given world pos 'pt', find out the dist to given edge
        /// </summary>
        public static float DistPointToVEdge(Vector3 pt, VEdge vEdge)
        {
            Vector3 pos0 = vEdge.GetVVert(0).GetWorldPos();
            Vector3 pos1 = vEdge.GetVVert(1).GetWorldPos();

            return GeoUtil.DistPointToLine(pt, pos0, pos1);
        }
    }
}
}
