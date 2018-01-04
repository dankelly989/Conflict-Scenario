using System;
using System.Collections.Generic;
using UnityEngine;

namespace MH.IKConstraint
{
    public abstract class IKConstraintMB : MonoBehaviour
    {
        public virtual bool Init(ISolver solver, int jointIdx) { return true; }
        public virtual void BeforeRotate(ISolver solver, int jointIdx) { }
        public abstract void Apply(ISolver solver, int jointIdx);
    }
}
