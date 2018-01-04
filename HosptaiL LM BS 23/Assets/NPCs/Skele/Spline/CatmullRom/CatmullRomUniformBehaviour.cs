using System;
using System.Collections.Generic;
using UnityEngine;

namespace MH.Curves
{
    public class CatmullRomUniformBehaviour : BaseSplineBehaviour
    {
        [SerializeField]
        private CatmullRomUniform m_spline = new CatmullRomUniform();
	
        public override ISpline Spline { get { return m_spline; } }
    }
}
