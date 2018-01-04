using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH
{
namespace MeshOp
{
    /// <summary>
    /// used for information exchange
    /// </summary>
    class OpUtil
    {
        public bool IsHandleChanging()
        {
            return MeshManipulator.Instance.IsHandleChanging;
        }

        public SoftSelection GetSoftSelection()
        {
            return MeshManipulator.Instance.GetSoftSelection();
        }
    }
}
}
