using System;
using System.Collections.Generic;
using UnityEngine;

namespace MH.Curves
{
    public enum ESplineType
    {
        CatmullRomUniform,
        CatmullRomCentripetal,
        Bezier,
    }

    public enum ETwistMethod
    {
        YUp,
        Minimum,
    }

    public interface ISpline
    {
        /// <summary>
        /// the spline type
        /// </summary>
        ESplineType SplineType { get; }

        /// <summary>
        /// how many points in this spline, 
        /// this might differ by different spline types, usually it means user-specified control-point count
        /// </summary>
        int PointCount {get;}

        /// <summary>
        /// get the full-length of the spline
        /// </summary>
        float CurveLength { get; }

        /// <summary>
        /// access the resolution of the spline, 
        /// the interpretation could vary by different spline types
        /// </summary>
        int Resolution { get; set; }

        ///// <summary>
        ///// 
        ///// </summary>
        //CtrlPt GetPoint(int idx);

        ///// <summary>
        ///// 
        ///// </summary>
        //void SetPointDirty(int idx);

        /// <summary>
        /// given a [0,1] parameter, return the point on spline, local
        /// </summary>
        Vector3 Interp(float t);

        /// <summary>
        /// given a [0,1] parameter, return the tangent on spline, local
        /// </summary>
        Vector3 Tangent(float t);

        /// <summary>
        /// given a [0,1] parameter, return the up on spline, local
        /// </summary>
        Vector3 Up(float t);

        /// <summary>
        /// given a [0,1] parameter
        /// get pos/tan/up in one call, might save some performance than call separately
        /// </summary>
        void Calc(float t, out Vector3 pos, out Vector3 tangent, out Vector3 up);
    }

    public class SplineConst
    {
        public static Color SplinePtColor = new Color32(40, 100, 237, 200);
    }


    /// <summary>
    /// the control point of splines
    /// </summary>
    [Serializable]
    public class CtrlPt
    {
        [SerializeField]
        private Vector3 m_position;
        [SerializeField]
        private float m_tilt; //[-inf, inf]
        [SerializeField]
        private Vector3 m_scale;

        public CtrlPt()                         : this(Vector3.zero,0, Vector3.one) { }
        public CtrlPt(Vector3 pt)               : this(pt,0, Vector3.one) { }
        public CtrlPt(Vector3 pt, float tilt)   : this(pt,tilt, Vector3.one) { }
        public CtrlPt(Vector3 pt, float tilt, Vector3 scale)
        {
            m_position = pt;
            m_tilt = tilt;
            m_scale = scale;
        }

        public void Copy(CtrlPt o)
        {
            m_position = o.m_position;
            m_tilt = o.m_tilt;
            m_scale = o.m_scale;
        }

        public Vector3 pos
        {
            get { return m_position; }
            set { m_position = value; }
        }
        public float pos_x
        {
            get { return m_position.x; }
            set { m_position.x = value; }
        }
        public float pos_y
        {
            get { return m_position.y; }
            set { m_position.y = value; }
        }
        public float pos_z
        {
            get { return m_position.z; }
            set { m_position.z = value; }
        }

        public float tilt
        {
            get { return m_tilt; }
            set { m_tilt = value; }
        }
        public Vector3 scale
        {
            get { return m_scale; }
            set { m_scale = value; }
        }
    }

}
