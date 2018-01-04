using System;
using System.Collections.Generic;
using UnityEngine;

namespace MH.Curves
{
    public class BaseSplineBehaviour : MonoBehaviour
    {
        protected Transform m_tr = null;

        public virtual ISpline Spline { get { return null; } }

        public Transform Tr
        {
            get 
            { 
                if (m_tr == null) m_tr = transform;
                return m_tr;
            }
        }

        public Vector3 GetPosition(float t)
        {
            ISpline sp = Spline;
            if (sp == null)
            {
                Dbg.LogErr("BaseSplineBehaviour.GetPosition: not set spline yet");
                return Vector3.zero;
            }

            Dbg.Assert(0 <= t && t <= 1, "BaseSplineBehaviour.GetPosition: t beyond range: {0}", t);

            Vector3 pos = sp.Interp(t);

            return pos;
        }

        public Vector3 GetTangent(float t)
        {
            ISpline sp = Spline;
            if (sp == null)
            {
                Dbg.LogErr("BaseSplineBehaviour.GetTangent: not set spline yet");
                return Vector3.zero;
            }

            Dbg.Assert(0 <= t && t <= 1, "BaseSplineBehaviour.GetTangent: t beyond range: {0}", t);

            Vector3 tan = sp.Tangent(t);

            return tan;
        }

        public Vector3 GetUp(float t)
        {
            ISpline sp = Spline;
            if (sp == null)
            {
                Dbg.LogErr("BaseSplineBehaviour.GetUp: not set spline yet");
                return Vector3.zero;
            }

            Dbg.Assert(0 <= t && t <= 1, "BaseSplineBehaviour.GetUp: t beyond range: {0}", t);

            Vector3 up = sp.Up(t);

            return up;
        }

        public bool Calc(float t, out Vector3 pos, out Vector3 tan, out Vector3 up)
        {
            pos = Vector3.zero;
            tan = Vector3.forward;
            up = Vector3.up;

            ISpline sp = Spline;
            if (sp == null)
            {
                Dbg.LogErr("BaseSplineBehaviour.Calc: not set spline yet");
                return false;
            }

            Dbg.Assert(0 <= t && t <= 1, "BaseSplineBehaviour.GetUp: t beyond range: {0}", t);

            sp.Calc(t, out pos, out tan, out up);
            return true;
        }

        public bool CalcTransformed(float t, out Vector3 pos, out Vector3 tan, out Vector3 up)
        {
            if (!Calc(t, out pos, out tan, out up))
                return false;

            pos = Tr.TransformPoint(pos);
            tan = Tr.TransformDirection(tan);
            up = Tr.TransformDirection(up);

            return true;
        }

        public Vector3 GetTransformedPosition(float t)
        {
            Vector3 oriPos = GetPosition(t);
            Vector3 trPos = Tr.TransformPoint(oriPos);
            return trPos;
        }

        public Vector3 GetTransformedTangent(float t)
        {
            Vector3 oriTan = GetTangent(t);
            Vector3 trTan = Tr.TransformDirection(oriTan);
            return trTan;
        }

        public Vector3 GetTransformedUp(float t)
        {
            Vector3 oriUp = GetUp(t);
            Vector3 trUp = Tr.TransformDirection(oriUp);
            return trUp;
        }

        public Bounds CalculateBounds(int maxIter = DEF_ITER_BOUND)
        {
            ISpline spline = Spline;
            if (spline == null)
            {
                Dbg.LogErr("BaseSplineBehaviour.CalculateBounds: not set spline yet");
                return new Bounds();
            }

            Bounds bd = new Bounds();

            for (int i = 0; i < maxIter; ++i)
            {
                float t = (float)i / (float)maxIter;
                Vector3 p = GetPosition(t);
                bd.Encapsulate(p);
            }

            return bd;
        }

        public Bounds CalculateTransformedBounds(int maxIter = DEF_ITER_BOUND)
        {
            ISpline spline = Spline;
            if (spline == null)
            {
                Dbg.LogErr("BaseSplineBehaviour.CalculateTransformedBounds: not set spline yet");
                return new Bounds();
            }

            Bounds bd = new Bounds();

            if (maxIter < 0)
                maxIter = Mathf.Max((spline.PointCount-1) * 10, 100);

            for (int i = 0; i < maxIter; ++i)
            {
                float t = (float)i / (float)maxIter;
                Vector3 p = GetTransformedPosition(t);
                bd.Encapsulate(p);
            }

            return bd;
        }



        private const int DEF_ITER_BOUND = -1;
    }
}
