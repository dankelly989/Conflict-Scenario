using System;
using System.Collections.Generic;
using UnityEngine;

namespace MH
{
    public class DrawUtil
    {
        private static Material s_lineMat;

        private static Material lineMat
        {
            get
            {
                if (s_lineMat == null)
                {
                    // Unity has a built-in shader that is useful for drawing
                    // simple colored things.
                    var shader = Shader.Find("Skele/DrawUtilLine");
                    s_lineMat = new Material(shader);
                    s_lineMat.hideFlags = HideFlags.HideAndDontSave;
                }
                return s_lineMat;
            }
        }

        public static void DrawLine(Vector3 p0, Vector3 p1, Color c)
        {
            GL.PushMatrix();
            // Draw line
            lineMat.SetPass(0);
            GL.Begin(GL.LINES);
                GL.Color(c);
                GL.Vertex(p0);
                GL.Vertex(p1);
            GL.End();

            lineMat.SetPass(1);
            GL.Begin(GL.LINES);
                GL.Color(c);
                GL.Vertex(p0);
                GL.Vertex(p1);
            GL.End();

            GL.PopMatrix();
        }

        public static void DrawLine(Vector3 _p0, Vector3 _p1, Color c, float width)
        {
            Vector3 camFwd = Camera.current.cameraToWorldMatrix.MultiplyVector(Vector3.back);
            Vector3 lineDir = _p0 - _p1;
            Vector3 up = Vector3.Cross(camFwd, lineDir).normalized;

            Vector3 half = up * 0.5f * width;
            Vector3 p0 = _p0 - half;
            Vector3 p1 = _p0 + half;
            Vector3 p2 = _p1 + half;
            Vector3 p3 = _p1 - half;

            GL.PushMatrix();
            // Draw line
            lineMat.SetPass(0);
            GL.Begin(GL.QUADS);
                GL.Color(c);
                GL.Vertex(p0);
                GL.Vertex(p1);
                GL.Vertex(p2);
                GL.Vertex(p3);
            GL.End();

            lineMat.SetPass(1);
            GL.Begin(GL.QUADS);
                GL.Color(c);
                GL.Vertex(p0);
                GL.Vertex(p1);
                GL.Vertex(p2);
                GL.Vertex(p3);
            GL.End();

            GL.PopMatrix();
        }
    }
}
