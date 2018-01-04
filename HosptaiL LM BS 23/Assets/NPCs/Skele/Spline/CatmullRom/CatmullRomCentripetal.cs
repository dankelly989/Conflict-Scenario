using System;
using System.Collections.Generic;
using MH.Constraints;
using UnityEngine;

//https://en.wikipedia.org/wiki/Centripetal_Catmull%E2%80%93Rom_spline
namespace MH.Curves
{
    [Serializable]
    public class CatmullRomCentripetal : ISpline
    {
        #region "data"

        [SerializeField]
        private List<CtrlPt> m_points = new List<CtrlPt>();
        [SerializeField]
        private int m_ptsPerSeg = 10;
        [SerializeField]
        private float m_BigT = 1; //as this is not Uniform, we need BigT to help map from t:[0,1] to the segment
        [SerializeField]
        private List<float> m_pointTpos = new List<float>(); //for each point in m_points, it has a pos in t:[0,1]
        [SerializeField]
        private bool m_cycle = false;

        private float m_length = 0; 
        private bool m_dirty = true;

        private CtrlPt m_ctrlPt0 = new CtrlPt(); //first control point
        private CtrlPt m_ctrlPtz = new CtrlPt(); //last control point

        [SerializeField]
        private List<Vector3> m_interUps = new List<Vector3>();
        [SerializeField]
        private ETwistMethod m_twistMtd = ETwistMethod.YUp;

#if UNITY_EDITOR
        private List<Vector3> m_interPts = new List<Vector3>(); //the interpolated points
        private List<Vector3> m_interTangents = new List<Vector3>(); //the tangents at interpolated points
#endif
	
	    #endregion "data"

        #region "public method"

        public CatmullRomCentripetal() : this(Vector3.zero, Vector3.right)
        {        }

        public CatmullRomCentripetal(Vector3 p0, Vector3 p1)
        {
            AddPoint(p0);
            AddPoint(p1);
        }

        /// <summary>
        /// set as dirty
        /// </summary>
        public void SetDirty()
        {
            m_dirty = true;
        }


        public void RemovePoint(int idx)
        {
            if (m_points.Count <= 2)
            {
                Dbg.LogWarn("CatmullRomCentripetal.RemovePoint: must have at least 2 points");
                return;
            }
            Dbg.Assert(idx < m_points.Count, "CatmullRomCentripetal.RemovePoint: the idx {0} beyond count: {1}", idx, m_points.Count);
            m_points.RemoveAt(idx);
            _MaintainCycle();
            SetDirty();
        }

        public void RemovePoint()
        {
            RemovePoint(m_points.Count - 1);
        }

        public void InsertPoint(int idx, Vector3 pt, float tilt = 0)
        {
            m_points.Insert(idx, new CtrlPt(pt, tilt));
            _MaintainCycle();
            SetDirty();
        }

        public void InsertPointAfter(int idx, Vector3 pt, float tilt = 0)
        {
            if (idx == m_points.Count - 1)
            {
                AddPoint(pt, tilt);
            }
            else
            {
                InsertPoint(idx+1, pt, tilt); 
            }
        }

        public void AddPoint(Vector3 pt, float tilt = 0)
        {
            m_points.Add(new CtrlPt(pt, tilt));
            _MaintainCycle();
            SetDirty();
        }

        public Vector3 this[int idx]
        {
            get
            {
                return m_points[idx].pos;
            }
            set
            {
                if (m_points[idx].pos != value)
                {
                    m_points[idx].pos = value;
                    _MaintainCycle();
                    SetDirty();
                }                
            }
        }

        // control point tilt
        public float GetTilt(int idx)
        {
            return m_points[idx].tilt;
        }
        public void SetTilt(int idx, float tilt)
        {
            if (m_points[idx].tilt != tilt)
            {
                m_points[idx].tilt = tilt;
                SetDirty();
            }
        }

        // control point scale
        public Vector3 GetScale(int idx)
        {
            return m_points[idx].scale;
        }
        public void SetScale(int idx, Vector3 scale)
        {
            if (m_points[idx].scale != scale)
            {
                m_points[idx].scale = scale;
                SetDirty();
            }
        }


#if UNITY_EDITOR
        /// <summary>
        /// used to draw line in editor
        /// </summary>
        public List<Vector3> InterPoints { 
            get {
                if (m_dirty) _Recalc();
                return m_interPts;
            }
        }

        public List<Vector3> InterTangents
        {
            get { 
                if (m_dirty) _Recalc();
                return m_interTangents;
            }
        }


#endif

        public List<Vector3> InterUps
        {
            get
            {
                if (m_dirty) _Recalc();
                return m_interUps;
            }
        }

        /// <summary>
        /// get/set the cycle prop
        /// </summary>
        public bool Cycle
        {
            get { return m_cycle; }
            set
            {
                if (m_cycle != value)
                {
                    if (value && PointCount < 3) return; //do nothing if try to make cycle with less than 3 points
                    m_cycle = value;

                    if (m_cycle)
                        _MakeCycle();
                    else
                        _BreakCycle();
                }
            }
        }

        public ETwistMethod TwistMtd
        {
            get { return m_twistMtd; }
            set { m_twistMtd = value; SetDirty(); }
        }

	    #endregion "public method"

        #region "ISpline impl."

        /// <summary>
        /// the curve type
        /// </summary>
        public ESplineType SplineType { get { return ESplineType.CatmullRomCentripetal; } }

        /// <summary>
        /// how many points in this curve
        /// </summary>
        public int PointCount { get { return m_points.Count; } }

        /// <summary>
        /// get the full-length of the curve
        /// </summary>
        public float CurveLength { get {
            if (m_dirty)
            {
                _Recalc();
            }
            return m_length;
        } }

        public int Resolution
        {
            get { return m_ptsPerSeg; }
            set { m_ptsPerSeg = value; SetDirty(); }
        }

        /// <summary>
        /// given a [0,1] parameter, return the point on curve
        /// </summary>
        public Vector3 Interp(float t)
        {
            int currPt;
            _PreCalc(t, out currPt);

            float T = t * m_BigT;
            int pcnt = m_points.Count;

            // prepare 4 control points
            Vector3 va = (currPt == 0) ? m_ctrlPt0.pos : m_points[currPt-1].pos;
            Vector3 vb = m_points[currPt].pos;
            Vector3 vc = m_points[currPt + 1].pos;
            Vector3 vd = (currPt+2 >= pcnt) ? m_ctrlPtz.pos : m_points[currPt + 2].pos;

            // prepare u
            float uAB = 0;
            if (currPt == 0) uAB = Mathf.Sqrt((vb - va).magnitude);
            else uAB = m_pointTpos[currPt] - m_pointTpos[currPt - 1];
            float u = uAB + (T - m_pointTpos[currPt]);

            // calculate
            float D0 = Mathf.Sqrt((va-vb).magnitude);
            float D1 = Mathf.Sqrt((vb-vc).magnitude);
            float D2 = Mathf.Sqrt((vc-vd).magnitude);
            float D01 = D0+D1;
            float D02 = D01+D2;
            float D12 = D1+D2;

            float A = ((D0 - u) * (D01 - u) * (D01 - u)) / (D0 * D1 * D01);
            float B = ((D01 - u) * (D01 - u) * u) / (D1 * D1 * D01) + ((D01 - u) * (D01 - u) * u) / (D0 * D1 * D01) + ((D01 - u) * (D02 - u) * (u - D0)) / (D1 * D1 * D12);
            float C = ((D01 - u) * (u - D0) * u) / (D1 * D1 * D01) + ((D02 - u) * (u - D0) * (u - D0)) / (D1 * D1 * D12) + ((D02 - u) * (u - D0) * (u - D0)) / (D1 * D2 * D12);
            float D = ((u - D0) * (u - D0) * (u - D01)) / (D1 * D2 * D12);

            return A*va+B*vb+C*vc+D*vd;
        }

        /// <summary>
        /// given a [0,1] parameter, return the tangent on curve
        /// </summary>
        public Vector3 Tangent(float t)
        {
            int currPt;
            _PreCalc(t, out currPt);

            float T = t * m_BigT;
            int pcnt = m_points.Count;

            // prepare 4 control points
            Vector3 va = (currPt == 0) ? m_ctrlPt0.pos : m_points[currPt - 1].pos;
            Vector3 vb = m_points[currPt].pos;
            Vector3 vc = m_points[currPt + 1].pos;
            Vector3 vd = (currPt + 2 >= pcnt) ? m_ctrlPtz.pos : m_points[currPt + 2].pos;

            // prepare u
            float uAB = 0;
            if (currPt == 0) uAB = Mathf.Sqrt((vb - va).magnitude);
            else uAB = m_pointTpos[currPt] - m_pointTpos[currPt - 1];
            float u = uAB + (T - m_pointTpos[currPt]);

            //calculate
            float D0 = Mathf.Sqrt((va-vb).magnitude);
            float D1 = Mathf.Sqrt((vb-vc).magnitude);
            float D2 = Mathf.Sqrt((vc-vd).magnitude);
            float D01 = D0+D1;
            float D02 = D01+D2;
            float D12 = D1+D2;

            float A = ((-2*(D0-u)*(D01-u))/(D0*D1*D01) - ((D01-u)*(D01-u))/(D0*D1*D01));
            float B = (D01-u)*(D01-u)/(D1*D1*D01) + (D01-u)*(D01-u)/(D0*D1*D01) + (D01-u)*(D02-u)/(D1*D1*D12) - 2*u*(D01-u)/(D1*D1*D01) - 2*u*(D01-u)/(D0*D1*D01) - (D01-u)*(u-D0)/(D1*D1*D12) - (D02-u)*(u-D0)/(D1*D1*D12);
            float C = (D01-u)*u/(D1*D1*D01) + (D01-u)*(u-D0)/(D1*D1*D01) + 2*(D02-u)*(u-D0)/(D1*D1*D12) + 2*(D02-u)*(u-D0)/(D1*D2*D12) - u*(u-D0)/(D1*D1*D01) - (u-D0)*(u-D0)/(D1*D1*D12) - (u-D0)*(u-D0)/(D1*D2*D12);
            float D = (u-D0)*(u-D0)/(D1*D2*D12) + 2*(u-D0)*(u-D01)/(D1*D2*D12);

            // derivative
            return (A*va+B*vb+C*vc+D*vd).normalized;
        }

        public Vector3 Up(float t)
        {
            return Up(t, Tangent(t));
        }

        public Vector3 Up(float t, Vector3 tan)
        {
            // calculate the interpolated up dir
            int interUpCnt = m_interUps.Count - 1; //sub the first point
            int currIdx = Mathf.Min(Mathf.FloorToInt(t * interUpCnt), interUpCnt - 1);
            float perc = t * interUpCnt - currIdx;

            Vector3 up0 = m_interUps[currIdx];
            Vector3 up1 = m_interUps[currIdx + 1];
            Vector3 up = Vector3.Slerp(up0, up1, perc); //interUp is already tilted up vector3

            //up = _ApplyTiltToUp(t, ref tan, ref up);

            return up;
        }

        public void Calc(float t, out Vector3 pos, out Vector3 tan, out Vector3 up)
        {
            int currPt;
            _PreCalc(t, out currPt);

            float T = t * m_BigT;
            int pcnt = m_points.Count;

            // prepare 4 control points
            Vector3 va = (currPt == 0) ? m_ctrlPt0.pos : m_points[currPt - 1].pos;
            Vector3 vb = m_points[currPt].pos;
            Vector3 vc = m_points[currPt + 1].pos;
            Vector3 vd = (currPt + 2 >= pcnt) ? m_ctrlPtz.pos : m_points[currPt + 2].pos;

            // prepare u
            float uAB = 0;
            if (currPt == 0) uAB = Mathf.Sqrt((vb - va).magnitude);
            else uAB = m_pointTpos[currPt] - m_pointTpos[currPt - 1];
            float u = uAB + (T - m_pointTpos[currPt]);

            // calculate
            float D0 = Mathf.Sqrt((va - vb).magnitude);
            float D1 = Mathf.Sqrt((vb - vc).magnitude);
            float D2 = Mathf.Sqrt((vc - vd).magnitude);
            float D01 = D0 + D1;
            float D02 = D01 + D2;
            float D12 = D1 + D2;

            //pos
            {
                float A = ((D0 - u) * (D01 - u) * (D01 - u)) / (D0 * D1 * D01);
                float B = ((D01 - u) * (D01 - u) * u) / (D1 * D1 * D01) + ((D01 - u) * (D01 - u) * u) / (D0 * D1 * D01) + ((D01 - u) * (D02 - u) * (u - D0)) / (D1 * D1 * D12);
                float C = ((D01 - u) * (u - D0) * u) / (D1 * D1 * D01) + ((D02 - u) * (u - D0) * (u - D0)) / (D1 * D1 * D12) + ((D02 - u) * (u - D0) * (u - D0)) / (D1 * D2 * D12);
                float D = ((u - D0) * (u - D0) * (u - D01)) / (D1 * D2 * D12);
                pos = A * va + B * vb + C * vc + D * vd;
            }

            //tangent
            {
                float A = ((-2 * (D0 - u) * (D01 - u)) / (D0 * D1 * D01) - ((D01 - u) * (D01 - u)) / (D0 * D1 * D01));
                float B = (D01 - u) * (D01 - u) / (D1 * D1 * D01) + (D01 - u) * (D01 - u) / (D0 * D1 * D01) + (D01 - u) * (D02 - u) / (D1 * D1 * D12) - 2 * u * (D01 - u) / (D1 * D1 * D01) - 2 * u * (D01 - u) / (D0 * D1 * D01) - (D01 - u) * (u - D0) / (D1 * D1 * D12) - (D02 - u) * (u - D0) / (D1 * D1 * D12);
                float C = (D01 - u) * u / (D1 * D1 * D01) + (D01 - u) * (u - D0) / (D1 * D1 * D01) + 2 * (D02 - u) * (u - D0) / (D1 * D1 * D12) + 2 * (D02 - u) * (u - D0) / (D1 * D2 * D12) - u * (u - D0) / (D1 * D1 * D01) - (u - D0) * (u - D0) / (D1 * D1 * D12) - (u - D0) * (u - D0) / (D1 * D2 * D12);
                float D = (u - D0) * (u - D0) / (D1 * D2 * D12) + 2 * (u - D0) * (u - D01) / (D1 * D2 * D12);
                // derivative
                tan = (A * va + B * vb + C * vc + D * vd).normalized;
            }

            //up
            {
                up = Up(t, tan);
            }
        }

        #endregion

        #region "private method"

        /// <summary>
        /// * calculate BigT for mapping t to segment
        /// * calculate the length of curve
        /// * extra: calculate smaller segments for editor to draw
        /// </summary>
        private void _Recalc()
        {
            m_dirty = false;
            m_length = 0;
            m_BigT = 0;
            m_pointTpos.Clear();

            // calculate bigT & pointTpos list
            m_pointTpos.Add(0); 
            for (int i = 1; i < m_points.Count; ++i)
            {
                Vector3 p0 = m_points[i-1].pos;
                Vector3 p1 = m_points[i].pos;
                m_BigT += Mathf.Sqrt((p1 - p0).magnitude);
                m_pointTpos.Add(m_BigT);
            }

            // calculate length of curve
            Vector3 prevPt = m_points[0].pos;
            Vector3 prevTan = Tangent(0);
            Vector3 prevBaseUp = (prevTan == Vector3.up) ? Vector3.back : Vector3.Cross(prevTan, Vector3.Cross(Vector3.up, prevTan)).normalized;

#if UNITY_EDITOR
            m_interPts.Clear();
            m_interPts.Add(prevPt);
            m_interTangents.Clear();
            m_interTangents.Add(prevTan);
#endif
            m_interUps.Clear();
            m_interUps.Add(ConUtil.ApplyTiltToUp(m_points, 0, ref prevTan, ref prevBaseUp));

            int SmoothAmount = (PointCount-1) * m_ptsPerSeg;
            for (int i = 1; i <= SmoothAmount; i++)
            {
                float pm = (float)i / SmoothAmount;
                Vector3 currPt = Interp(pm);
                m_length += Vector3.Distance(prevPt, currPt);

                Vector3 currTan = Tangent(pm);
                Vector3 up = _CalcUp(pm, prevTan, currTan, ref prevBaseUp);
                m_interUps.Add(up);
#if UNITY_EDITOR
                m_interPts.Add(currPt);
                m_interTangents.Add(currTan);
#endif

                prevPt = currPt;
                prevTan = currTan;
            }

        }

        private void _PrepControlPts(bool cycle)
        {
            int cnt = m_points.Count;
            if (cycle)
            {
                m_ctrlPt0.Copy(m_points[cnt - 2]);
                m_ctrlPtz.Copy(m_points[1]);
            }
            else
            {
                m_ctrlPt0.pos = m_points[0].pos + (m_points[0].pos - m_points[1].pos);
                m_ctrlPtz.pos = m_points[cnt-1].pos + (m_points[cnt-1].pos - m_points[cnt-2].pos) ;
            }
        }

        private void _PreCalc(float t, out int currPt)
        {
            int pcnt = m_points.Count;
            bool cycle = (m_points[0] == m_points[pcnt - 1]);
            _PrepControlPts(cycle);

            if (m_dirty)
                _Recalc();

            // calc currPt
            currPt = -1;
            float T = t * m_BigT;
            for (int i = m_pointTpos.Count - 2; i >= 0; --i)
            {
                if (T >= m_pointTpos[i])
                {
                    currPt = i;
                    break;
                }
            }

            Dbg.Assert(currPt >= 0, "CatmullRomCentripetal._PreCalc: failed to calc currPt");
        }

        private void _MaintainCycle()
        {
            if (m_cycle)
                m_points[m_points.Count - 1] = m_points[0];
        }

        private void _BreakCycle()
        {
            RemovePoint(); //remove the last point
        }

        private void _MakeCycle()
        {
            if (PointCount < 3) return; //do nothing if try to make cycle with less than 3 points
            AddPoint(m_points[0].pos);
        }

        /// <summary>
        /// use prev data to calculate current up dir
        /// </summary>
        private Vector3 _CalcUp(float t, Vector3 prevTan, Vector3 currTan, ref Vector3 prevBaseUp)
        {
            Vector3 currUp = Vector3.zero;
            switch (m_twistMtd)
            {
                case ETwistMethod.YUp:
                    {
                        Vector3 bino = Vector3.Cross(Vector3.up, currTan);
                        currUp = Vector3.Cross(currTan, bino).normalized;
                    }
                    break;
                case ETwistMethod.Minimum:
                    {
                        Vector3 prevBino = Vector3.Cross(prevBaseUp, prevTan);
                        currUp = Vector3.Cross(currTan, prevBino).normalized;
                    }
                    break;
                default:
                    Dbg.LogErr("CatmullRomCentripetal._CalcUp: unexpected twistMtd: {0}", m_twistMtd); break;
            }

            if (currUp == Vector3.zero)
                currUp = prevBaseUp;
            prevBaseUp = currUp;

            currUp = ConUtil.ApplyTiltToUp(m_points, t, ref currTan, ref currUp);

            return currUp;
        }

	    #endregion "private method"
    }
}
