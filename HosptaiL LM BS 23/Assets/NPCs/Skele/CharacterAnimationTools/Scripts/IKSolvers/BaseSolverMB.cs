using System;
using System.Collections.Generic;
using UnityEngine;

namespace MH.Constraints
{
    public abstract class BaseSolverMB : BaseConstraint
    {
        public abstract IKSolverType solverType { get; }
    }
}
