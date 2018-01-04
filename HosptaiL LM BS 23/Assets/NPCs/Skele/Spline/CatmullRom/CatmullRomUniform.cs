using System;
using System.Collections.Generic;
using MH.Constraints;
using UnityEngine;

namespace MH.Curves
{
    [Serializable]
    public class CatmullRomUniform : ISpline
    {
		#region "data"

        [SerializeField]
        private List<CtrlPt> m_points = new List<CtrlPt>();
        [SerializeField]
        private int m_ptsPerSeg = 10;
        [SerializeField]
        private bool m_cycle = false; //whether is cycle

        private float m_length = 0; 
        private bool m_dirty = true;

        private CtrlPt m_ctrlPt0 = new CtrlPt(); //first control point
        private CtrlPt m_ctrlPtz = new CtrlPt(); //last control point

        [SerializeField]
        private List<Vector3> m_interUps = new List<Vector3>(); //the up dir at interpolated points
        [SerializeField]
        private ETwistMethod m_twistMtd = ETwistMethod.YUp;


#if UNITY_EDITOR
        private List<Vector3> m_interPts = new List<Vector3>(); //the interpolated points
        private List<Vector3> m_interTangents = new List<Vector3>(); //the tangents at interpolated points
#endif
	
	    #endregion "data"

        #region "public method"

        public CatmullRomUniform() : this(Vector3.zero, Vector3.right)
        {        }

        public CatmullRomUniform(Vector3 p0, Vector3 p1)
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
                Dbg.LogWarn("CatmullRomUniform.RemovePoint: must have at least 2 points");
                return;
            }
            Dbg.Assert(idx < m_points.Count, "CatmullRomUniform.RemovePoint: the idx {0} beyond count: {1}", idx, m_points.Count);
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

        //control point position
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
                if (m_dirty) _RecalcLen();
                return m_interPts;
            }
        }

        public List<Vector3> InterTangents
        {
            get { 
                if (m_dirty) _RecalcLen();
                return m_interTangents;
            }
        }
#endif

        public List<Vector3> InterUps
        {
            get
            {
                if (m_dirty) _RecalcLen();
                return m_interUps;
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
        public ESplineType SplineType { get { return ESplineType.CatmullRomUniform; } }

        /// <summary>
        /// how many points in this curve
        /// </summary>
        public int PointCount { get { return m_points.Count; } }

        /// <summary>
        /// get the full-length of the curve
        /// </summary>
        public float CurveLength { 
            get {
                if (m_dirty)
                {
                    _RecalcLen();
                }
                return m_length;
            }
        }

        public int Resolution
        {
            get { return m_ptsPerSeg; }
            set { m_ptsPerSeg = value; SetDirty(); }
        }

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

        /// <summary>
        /// given a [0,1] parameter, return the point on curve
        /// </summary>
        public Vector3 Interp(float t)
        {
            int pcnt = m_points.Count;
            bool cycle = (m_points[0] == m_points[pcnt - 1]);
            _PrepControlPts(cycle);

            int numSections = pcnt - 1;
            int currPt = Mathf.Min(Mathf.FloorToInt(t * (float)numSections), numSections - 1);
            float u = t * (float)numSections - (float)currPt;

            Vector3 a = (currPt == 0) ? m_ctrlPt0.pos : m_points[currPt - 1].pos;
            Vector3 b = m_points[currPt].pos;
            Vector3 c = m_points[currPt + 1].pos;
            Vector3 d = (currPt + 2 >= pcnt) ? m_ctrlPtz.pos : m_points[currPt + 2].pos;

            return _PositionFormula(u, ref a, ref b, ref c, ref d);
        }

        /// <summary>
        /// given a [0,1] parameter, return the tangent on curve
        /// </summary>
        public Vector3 Tangent(float t)
        {
            int pcnt = m_points.Count;
            bool cycle = (m_points[0] == m_points[pcnt - 1]);
            _PrepControlPts(cycle);

            int numSections = pcnt - 1;
            int currPt = Mathf.Min(Mathf.FloorToInt(t * (float)numSections), numSections - 1);
            float u = t * (float)numSections - (float)currPt;

            Vector3 a = (currPt == 0) ? m_ctrlPt0.pos : m_points[currPt - 1].pos;
            Vector3 b = m_points[currPt].pos;
            Vector3 c = m_points[currPt + 1].pos;
            Vector3 d = (currPt + 2 >= pcnt) ? m_ctrlPtz.pos : m_points[currPt + 2].pos;

            // derivative
            return _TangentFormula(u, ref a, ref b, ref c, ref d).normalized;
        }

        /// <summary>
        /// given a [0,1] parameter, return the up on curve
        /// </summary>
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

        /// <summary>
        /// calc pos/tan/up at t
        /// </summary>
        public void Calc(float t, out Vector3 pos, out Vector3 tan, out Vector3 up)
        {
            int pcnt = m_points.Count;
            bool cycle = (m_points[0] == m_points[pcnt - 1]);
            _PrepControlPts(cycle);

            int numSections = pcnt - 1;
            int currPt = Mathf.Min(Mathf.FloorToInt(t * (float)numSections), numSections - 1);
            float u = t * (float)numSections - (float)currPt;

            Vector3 a = (currPt == 0) ? m_ctrlPt0.pos : m_points[currPt - 1].pos;
            Vector3 b = m_points[currPt].pos;
            Vector3 c = m_points[currPt + 1].pos;
            Vector3 d = (currPt + 2 >= pcnt) ? m_ctrlPtz.pos : m_points[currPt + 2].pos;

            pos = _PositionFormula(u, ref a, ref b, ref c, ref d);
            tan = _TangentFormula(u, ref a, ref b, ref c, ref d);
            up = Up(t, tan);
        }

        #endregion

        #region "private method"

        /// <summary>
        /// calculate the length of curve
        /// </summary>
        private void _RecalcLen()
        {
            m_length = 0;

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
                //prevBaseUp = up; //prevBaseUp is updated in _CalcUp
                prevTan = currTan;
            }

            m_dirty = false;
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
                    Dbg.LogErr("CatmullRomUniform._CalcUp: unexpected twistMtd: {0}", m_twistMtd); break;
            }

            if (currUp == Vector3.zero)
                currUp = prevBaseUp;
            prevBaseUp = currUp;

            currUp = ConUtil.ApplyTiltToUp(m_points, t, ref currTan, ref currUp);

            return currUp;
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

        private void _MaintainCycle()
        {
            if( m_cycle )
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

        private static Vector3 _PositionFormula(float u, ref Vector3 a, ref Vector3 b, ref Vector3 c, ref Vector3 d)
        {
            return .5f * (
                (-a + 3f * b - 3f * c + d) * (u * u * u)
                + (2f * a - 5f * b + 4f * c - d) * (u * u)
                + (-a + c) * u
                + 2f * b
            );
        }

        private static Vector3 _TangentFormula(float u, ref Vector3 a, ref Vector3 b, ref Vector3 c, ref Vector3 d)
        {
            return .5f * (
                  3 * (-a + 3f * b - 3f * c + d) * (u * u)
                + 2 * (2f * a - 5f * b + 4f * c - d) * (u)
                + (-a + c)
            );
        }

	    #endregion "private method"
    }
}
