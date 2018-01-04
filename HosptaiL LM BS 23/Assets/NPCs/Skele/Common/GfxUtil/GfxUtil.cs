using System;
using System.Collections.Generic;
using UnityEngine;

namespace MH
{
    /// <summary>
    /// this Utility provides some helpers for doing graphics-related work
    /// </summary>
	public class GfxUtil
	{
        private static Shader m_RenderDepthShader = null;

        public static Shader GetRenderDepthShader()
        {
            if( m_RenderDepthShader == null )
            {
                m_RenderDepthShader = Shader.Find(RenderDepthShaderName);
                Dbg.Assert(m_RenderDepthShader != null, "GfxUtil.GetRenderDepthShader: failed to get: {0}", RenderDepthShaderName);
            }
            return m_RenderDepthShader;
        }

        /// <summary>
        /// return a depth buffer texture from given camera, only render "Opaque" renderType objects
        /// </summary>
        public static Texture2D CreateDepthBuffer(Camera cam)
        {
            RenderTexture rt = RenderTexture.GetTemporary(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);

            if( cam == null )
                cam = Camera.main;

            var oldClearFlags = cam.clearFlags; 
            cam.clearFlags = CameraClearFlags.SolidColor;
            var oldClearColor = cam.backgroundColor; 
            cam.backgroundColor = new Color32(254, 254, 254, 254);

            Shader shader = GetRenderDepthShader();
            cam.targetTexture = rt;
            cam.RenderWithShader(shader, ReplacementTag);
            cam.targetTexture = null;

            Texture2D tex = RTUtil.GetRTPixels(rt);

            RenderTexture.ReleaseTemporary(rt);

            cam.clearFlags = oldClearFlags;
            cam.backgroundColor = oldClearColor;

            //byte[] img = tex.EncodeToJPG();
            //System.IO.File.WriteAllBytes("Assets/depthBuffer.jpg", img);
            //Dbg.Log("write to depthBuffer: {0}, {1}x{2}", tex.GetInstanceID(), tex.width, tex.height);

            return tex;
        }

        /// <summary>
        /// given the depth-buffer texture and a screen-pos, return the sampled z-dist
        /// F: camera farClipPlane
        /// N: camera nearClipPlane
        /// FN : F*N
        /// NsF: N-F
        /// </summary>
        public static float GetZDistFromDepthBuffer(Texture2D zbuffer, Vector2 screenPos, float FN, float F, float NsF)
        {
            float norX = screenPos.x / zbuffer.width;
            float norY = screenPos.y / zbuffer.height;
            Color zBufColor = zbuffer.GetPixel(Mathf.RoundToInt(norX * Screen.width), Mathf.RoundToInt(norY * Screen.height));
            float zBufVal = RTUtil.DecodeFloatRGBA(zBufColor);
            float zBufDist = FN / (F + zBufVal * NsF);//IMPORTANT!!! z-depth is linearized based on reciprocal

            // ------------------------------
            // refer: https://en.wikipedia.org/wiki/Bilinear_filtering
            // DEPRECATED: not useful, the 4 points all far from right

            //float u = norX * Screen.width - 0.5f;
            //float v = norY * Screen.height - 0.5f;
            //int _x = Mathf.FloorToInt(u);
            //int x0 = Mathf.Max(0, _x);
            //int x1 = Mathf.Min(_x + 1, Screen.width - 1);
            //int _y = Mathf.FloorToInt(v);
            //int y0 = Mathf.Max(0, _y);
            //int y1 = Mathf.Min(_y + 1, Screen.height - 1);

            //float u_ratio = u - _x;
            //float v_ratio = v - _y;
            //float u_opposite = 1 - u_ratio;
            //float v_opposite = 1 - v_ratio;

            //float v00 = _GetZDist(zbuffer.GetPixel(x0, y0), FN, F, NsF);
            //float v01 = _GetZDist(zbuffer.GetPixel(x0, y1), FN, F, NsF);
            //float v10 = _GetZDist(zbuffer.GetPixel(x1, y0), FN, F, NsF);
            //float v11 = _GetZDist(zbuffer.GetPixel(x1, y1), FN, F, NsF);
            //Dbg.Log("v00: {0}, v01: {1}, v10: {2}, v11: {3}", v00, v01, v10, v11);

            //float zBufDist = (v00 * u_opposite + v10 * u_ratio) * v_opposite +
            //                 (v01 * u_opposite + v11 * u_ratio) * v_ratio;

            //------------------------------------------

            return zBufDist;
        }

        private static float _GetZDist(Color c, float FN, float F, float NsF)
        {
            float bufVal = RTUtil.DecodeFloatRGBA(c);
            float zdist = FN / (F + bufVal * NsF);
            return zdist;
        }

        private const string ReplacementTag = "RenderType";
        private const string RenderDepthShaderName = "Modeller/RenderDepth";
	}
}
