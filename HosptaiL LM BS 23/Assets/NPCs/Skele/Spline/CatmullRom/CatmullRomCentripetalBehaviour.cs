using System;
using System.Collections.Generic;
using UnityEngine;

namespace MH.Curves
{
    public class CatmullRomCentripetalBehaviour : BaseSplineBehaviour
    {
        [SerializeField]
        private CatmullRomCentripetal m_spline = new CatmullRomCentripetal();

        public override ISpline Spline { get { return m_spline; } }
    }
}
