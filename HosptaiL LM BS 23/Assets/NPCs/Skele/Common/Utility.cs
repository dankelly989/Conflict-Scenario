//#define HAS_ZIP
//#define HAS_EVTSET
#define HAS_JSON
//#define HAS_MAPUTIL
#define HAS_POOL

using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

#if HAS_ZIP
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;
#endif

#if HAS_JSON
using LitJson;
#if HAS_JSON_DOTNET
using Newtonsoft.Json;
#else
using FullSerializer;
#endif
#endif

#if HAS_MAPUTIL
using Pathfinding;
using Path = System.IO.Path;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

using Job = System.Collections.IEnumerator;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using ExtMethods;

namespace MH
{
    public enum ESpace
    {
        World,
        Self,
    }

    public enum EAxis
    {
        None = 0,
        X = 1,
        Y = 2,
        Z = 4,
    }

    public enum EAxisD
    {
        None = 0,
        X = 1, Y = 2, Z = 4,
        InvX = 8, InvY = 16, InvZ = 32,

        XYZ = X | Y | Z,
    }

    /// <summary>
    /// some methods for Vector3
    /// </summary>
    public class V3Ext
    {
        public static void ReplaceXY(ref Vector3 v, float x, float y)
        {
            v.x = x;
            v.y = y;
        }
        public static void ReplaceXY2(ref Vector3 v3, ref Vector2 v2)
        {
            v3.x = v2.x;
            v3.y = v2.y;
        }
        public static void ReplaceXY(ref Vector3 v3, ref Vector3 src)
        {
            v3.x = src.x;
            v3.y = src.y;
        }
        public static void ReplaceXZ(ref Vector3 v3, float x, float z)
        {
            v3.x = x;
            v3.z = z;
        }
        public static void ReplaceXZ2(ref Vector3 v3, ref Vector2 v2)
        {
            v3.x = v2.x;
            v3.z = v2.y;
        }
        public static void ReplaceXZ(ref Vector3 v3, ref Vector3 src)
        {
            v3.x = src.x;
            v3.y = src.y;
        }

        public static Vector3 MultiplyComp(Vector3 a, Vector3 b)
        {
            return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
        }

        public static Vector3 DivideComp(Vector3 a, Vector3 b)
        {
            return new Vector3(a.x / b.x, a.y / b.y, a.z / b.z);
        }

        /// <summary>
        /// 
        /// </summary>
        public static Vector3 FixZeroComponent(Vector3 v, float small = 0.001f)
        {
            if (Mathf.Approximately(v.x, 0))
            {
                v.x = Mathf.Sign(v.x) * small;
            }
            if (Mathf.Approximately(v.y, 0))
            {
                v.y = Mathf.Sign(v.y) * small;
            }
            if (Mathf.Approximately(v.z, 0))
            {
                v.z = Mathf.Sign(v.z) * small;
            }

            return v;
        }

        /// <summary>
        /// = Mathf.Abs(Vector3.Dot(a, b))
        /// </summary>
        public static float AbsDot(Vector3 a, Vector3 b)
        {
            return Mathf.Abs(Vector3.Dot(a, b));
        }

        /// <summary>
        /// This method will ignore Y coord, returned point will have Y equal A.y
        /// given two points A & B, return a point C that  abs(angle( AC, AB )) lt maxAngleDist;
        /// and minDist <= len(AC) <= maxDist
        /// </summary>
        public static Vector3 RandomPositionWithinRange(Vector3 from, Vector3 to, float maxAbsAngleDist, float minDist, float maxDist)
        {
            float storedY = from.y;
            Vector3 A = from; A.y = 0;
            Vector3 B = to; B.y = 0;

            float d = Random.Range(minDist, maxDist);
            Vector3 off = ( B - A ).normalized * d;
            float angle = Random.Range(-maxAbsAngleDist, maxAbsAngleDist);

            off = Quaternion.Euler(0, angle, 0) * off;

            Vector3 C = A + off; C.y = storedY;

            return C;
        }

        /// <summary>
        /// given two directions and a rotation-axis, map the two directions both to the plane to get A and B;
        /// calculate the angle dist of A -> B; (-180, 180]
        /// if any of the direction's dotprod is 0 with axis, return 0
        /// if axis is 0 vec, error-out;
        /// </summary>
        public static float AngleDistOnPlane(Vector3 from, Vector3 to, Vector3 axis)
        {
            Dbg.Assert(axis != Vector3.zero, "V3Ext.AngleDistOnPlane: axis is zero vec");

            Vector3 A = Vector3.ProjectOnPlane(from, axis);
            Vector3 B = Vector3.ProjectOnPlane(to, axis);
            if (A == Vector3.zero) return 0;
            if (B == Vector3.zero) return 0;

            float angle = Vector3.Angle(A, B);
            Vector3 cross = Vector3.Cross(A, B);
            if (Vector3.Dot(cross, axis) > 0)
                return angle;
            else
                return -angle;
        }

        /// <summary>
        /// given a directions A and a plane-normal, map dir along the normal to get B;
        /// calculate the angle dist of A -> B; [0, 90]
        /// if A == 0vec, return 0;
        /// if axis is 0 vec, error-out;
        /// 
        /// A doesn't need to be normalized
        /// </summary>
        public static float AngleWithPlaneProject(Vector3 A, Vector3 n)
        {
            Dbg.Assert(n != Vector3.zero, "V3Ext.AngleWithPlaneProject: normal is zero vec");

            if (A == Vector3.zero) return 0;

            Vector3 B = Vector3.ProjectOnPlane(A, n);
            //if (B == Vector3.zero) return 90f; //no need, Vector3.Angle() will return 90 for 0vec

            float angle = Vector3.Angle(A, B);

            return angle;
        }

        /// <summary>
        /// dir.y = 0;
        /// dir = normalized(dir)
        /// </summary>
        public static Vector3 ToNormalFlatDir(Vector3 dir)
        {
            dir.y = 0;
            return dir.normalized;
        }

        /// <summary>
        /// remove y component
        /// </summary>
        public static Vector3 ToFlatDir(Vector3 dir)
        {
            dir.y = 0;
            return dir;
        }

        /// <summary>
        /// given a dir, return Vector2 by remove its y component, if it's longer than threshold, normalize it;
        /// else return Vector2.zero
        /// </summary>
        public static Vector2 ToNormalDir2D(Vector3 dir)
        {
            dir.y = 0;
            if( dir.magnitude > kDirMagThres )
            {
                Vector3 d = dir.normalized;
                return new Vector2(d.x, d.z);
            }
            else
            {
                return Vector2.zero;
            }
        }

        /// <summary>
        /// convert (x,y,z) to (x,z)
        /// </summary>
        public static Vector2 ToDir2D(Vector3 dir)
        {
            return new Vector2(dir.x, dir.z);
        }

        /// <summary>
        /// convert (x,z) to (x,0,z)
        /// </summary>
        public static Vector3 ToDir3D(Vector2 dir)
        {
            return new Vector3(dir.x, 0, dir.y);
        }

        /// <summary>
        /// return a vector3 that each component will less-equal lhs & rhs
        /// </summary>
        public static Vector3 MinComponents(Vector3 lhs, Vector3 rhs)
        {
            Vector3 r = new Vector3();
            r.x = Mathf.Min(lhs.x, rhs.x);
            r.y = Mathf.Min(lhs.y, rhs.y);
            r.z = Mathf.Min(lhs.z, rhs.z);
            return r;
        }

        /// <summary>
        /// return a vector3 that each component will greater-equal lhs & rhs
        /// </summary>
        public static Vector3 MaxComponents(Vector3 lhs, Vector3 rhs)
        {
            Vector3 r = new Vector3();
            r.x = Mathf.Max(lhs.x, rhs.x);
            r.y = Mathf.Max(lhs.y, rhs.y);
            r.z = Mathf.Max(lhs.z, rhs.z);
            return r;
        }

        /// <summary>
        /// return a vector that has specified length, could accept zero vector and negative length
        /// </summary>
        public static Vector3 SetMagnitude(Vector3 v, float len)
        {
            Vector3 n = v.normalized;
            return n * len;
        }

        /// <summary>
        /// return a vector that has specified length of 'len+lenOff', could accept zero vector and negative offset
        /// </summary>
        public static Vector3 SetMagnitudeByOffset(Vector3 v, float lenOff)
        {
            float len = v.magnitude;
            if( len > 0 )
            {
                float newLen = len + lenOff;
                return v * (newLen / len);
            }
            else
            {
                return Vector3.zero;
            }
        }
        public static Vector3 SetMagnitudeByOffset(Vector3 v, float lenOff, float minLen, float maxLen)
        {
            float len = v.magnitude;
            if (len > 0)
            {
                float newLen = Mathf.Clamp(len + lenOff, minLen, maxLen);
                return v * (newLen / len);
            }
            else
            {
                return Vector3.zero;
            }
        }

        public const float kDirMagThres = 0.01f;
    }

    public class Misc
    {

        public static string
        GetTransformPath(Transform tr)
        {
            StringBuilder bld = new StringBuilder();
            while (tr != null)
            {
                if (bld.Length != 0)
                    bld.Insert(0, '/');
                bld.Insert(0, tr.name);
                tr = tr.parent;
            }
            return bld.ToString();
        }

        /// <summary>
        /// return a transform path from ancestor to tr, if not parental relation, return null;
        /// e.g.:
        /// ancestor = A, tr = A/B/C, ==> return B/C
        /// </summary>
        public static string
        GetTransformPath(Transform ancestor, Transform tr)
        {
            if (tr == null) return null;
            if (ancestor != null)
            {
                if( tr == null )
                    return null;
                if (!tr.IsChildOf(ancestor))
                    return null;
            }            

            StringBuilder bld = new StringBuilder();
            while(tr != null)
            {
                if (tr == ancestor)
                    break;

                if (bld.Length != 0)
                    bld.Insert(0, '/');
                bld.Insert(0, tr.name);
                tr = tr.parent;
            }

            return bld.ToString();

        }

        public static GameObject AssertGetGOByTag(string tag)
        {
            GameObject go = GameObject.FindGameObjectWithTag(tag);
            Dbg.Assert(go != null, "Misc.AssertGetGOByTag : go is null");
            return go;
        }

        /// <summary>
        /// use GameObject to Find specified GO,
        /// if not found, create one
        /// </summary>
        public static GameObject
        ForceGetGO(string gopath, GameObject baseGO = null)
        {
            if (gopath == null || gopath.Length == 0)
                return null;
             
            GameObject go = null;
            Transform tr = baseGO == null ? null : baseGO.transform;

            string[] paths = gopath.Split('/');
            for (int idx = 0; idx < paths.Length; ++idx)
            {
                go = _ForceGetGO(paths[idx], tr);
                tr = go.transform;
            }

            Dbg.Assert(go != null, "Misc.ForceGetGO: failed to get go for path: {0}", gopath);

            return go;
        }

        private static GameObject
        _ForceGetGO(string onePath, Transform baseTR)
        {
            if( baseTR == null )
            {
                string pureName = onePath;
                if (pureName.StartsWith("/"))
                    pureName = pureName.Substring(1);

                GameObject go = GameObject.Find("/" + pureName);
                if (go == null)
                    go = new GameObject(pureName);

                return go;
            }
            else
            {
                Transform tr = baseTR.Find(onePath);
                if( tr == null )
                {
                    GameObject go = new GameObject(onePath);
                    Misc.AddChild(baseTR, go);
                    return go;
                }
                else
                {
                    return tr.gameObject;
                }
            }
        }

        public static Transform
        ForceGetChildTr(Transform baseTr, string childName)
        {
            Transform childTr = baseTr.Find(childName);
            if( childTr == null )
            {
                GameObject go = new GameObject(childName);
                Misc.AddChild(baseTr, go);
                return go.transform;
            }
            else
            {
                return childTr;
            }
        }

        /// <summary>
        /// add `child' as children of `parent'
        /// </summary>
        public static void
        AddChild(GameObject parent, GameObject child, bool keepLocalTr = false)
        {
            if (child == null)
                return;
            
            AddChild(parent != null ? parent.transform : null,
                child.transform,
                keepLocalTr);
        }        
        public static void
        AddChild(GameObject parent, Transform child, bool keepLocalTr = false)
        {
            if (child == null)
                return;
            
            AddChild(parent != null ? parent.transform : null, child, keepLocalTr);
        }
        public static void
        AddChild(Transform parent, GameObject child, bool keepLocalTr = false )
        {
            if (child == null)
                return;

            AddChild(parent, child.transform, keepLocalTr);
        }
        public static void
        AddChild(Transform parent, Transform child, bool keepLocalTr = false)
        {
            child.SetParent(parent, !keepLocalTr);

            //XformData data = null;
            //if( keepLocalTr )
            //{
            //    data = new XformData();
            //    data.CopyFrom(child);
            //}
            //child.parent = parent;
            //if( keepLocalTr )
            //{
            //    data.Apply(child);
            //}
        }

        public static void DestroyChildren(Transform parent)
        {
            int cnt = parent.childCount;
            if (Application.isPlaying)
            {
                for (int i = cnt - 1; i >= 0; --i)
                {
                    GameObject.Destroy(parent.GetChild(i).gameObject);
                }
            }
            else
            {
                for (int i = cnt - 1; i >= 0; --i)
                {
                    GameObject.DestroyImmediate(parent.GetChild(i).gameObject);
                }
            }
        }


        public static byte[] FromBase64(string es)
        {
            return System.Convert.FromBase64String(es);
        }

        /// <summary>
        /// convert a byte[] to a Base64 string
        /// </summary>
        public static string ToBase64(byte[] bytes)
        {
            return System.Convert.ToBase64String(bytes);
        }
        

        /// <summary>
        /// swap two elements in a IList<T>
        /// </summary>
        public static void Swap<T>(IList<T> lst, int idx1, int idx2)
        {
            T tmp = lst[idx1];
            lst[idx1] = lst[idx2];
            lst[idx2] = tmp;
        }

        public static void Swap<T>(ref T lhs, ref T rhs) 
        {
            T tmp = lhs;
            lhs = rhs;
            rhs = tmp;
        }

        /// <summary>
        /// given a rotation radian(-inf, inf),
        /// transform it to [0, 2*PI]
        /// </summary>
        public static float NormalizeRotation(float radian)
        {
            float pi2 = Mathf.PI * 2f;
            float r = radian % pi2;
            r = (r < 0) ? (r + pi2) : r;
            return r;
        }

        /// <summary>
        /// given a rotation angle (-inf, inf)
        /// transform it to [0, 360]
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static float NormalizeAngle(float angle)
        {
            float pi2 = 360f;
            float r = angle % pi2;
            r = (r < 0) ? (r + pi2) : r;
            return r;
        }

        /// <summary>
        /// given a rotation angel (-inf, inf)
        /// transform into [-180, 180]
        /// </summary>
        public static float NormalizeAnglePI(float angle)
        {
            float pi2 = 360f;
            float r = angle % pi2;
            float abs = Mathf.Abs(r);
            float s = Mathf.Sign(r);
            if (abs > 180f)
                r += -s * pi2;
            return r;
        }

        public static Vector3 NormalizeAnglePI(Vector3 vec)
        {
            return new Vector3(
                NormalizeAnglePI(vec.x),
                NormalizeAnglePI(vec.y),
                NormalizeAnglePI(vec.z)
                );
        }

        /// <summary>
        /// given vec, return a vector which is projection of it on a plane defined by normal 'n'
        /// </summary>
        public static Vector3 ProjectOnPlane(Vector3 vec, Vector3 n)
        {
            Vector3 np = Vector3.Project(vec, n);
            return vec - np;
        }

        /// <summary>
        /// get the abs distance of two angles
        /// for signed distance of angles, use Mathf.DeltaAngle
        /// </summary>
        public static float AngleDist(float lhs, float rhs)
        {
            //float a = Misc.NormalizeAngle(lhs);
            //float b = Misc.NormalizeAngle(rhs);
            //float d1 = Mathf.Abs(a - b);
            //float d2 = Mathf.Abs(a - b - 360f);
            //float d3 = Mathf.Abs(a - b + 360f);

            //return Mathf.Min(d3, Mathf.Min(d1, d2));

            return Mathf.Abs(Mathf.DeltaAngle(lhs, rhs));
        }

        private static StringBuilder ms_bld = null;
        public static string ListToString<T>(IList<T> lst, Func<T, string> toStr = null)
        {
            if( ms_bld == null )
            {
                ms_bld = new StringBuilder();
            }
            
            if( toStr == null )
            {
                for (int i = 0; i < lst.Count; ++i)
                {
                    ms_bld.AppendFormat("{0} ", lst[i]);
                }
            }
            else
            {
                for (int i = 0; i < lst.Count; ++i)
                {
                    ms_bld.AppendFormat("{0} ", toStr(lst[i]));
                }
            }
            

            string s = ms_bld.ToString();
            ms_bld.Remove(0, ms_bld.Length);
            return s;
        }

        public static string IEnumToString<T>(IEnumerator<T> ie, Func<T, string> toStr = null)
        {
            if (ms_bld == null)
            {
                ms_bld = new StringBuilder();
            }

            if (toStr == null)
            {
                for (; ie.MoveNext(); )
                {
                    ms_bld.AppendFormat("{0} ", ie.Current.ToString());
                }
            }
            else
            {
                for (; ie.MoveNext(); )
                {
                    ms_bld.AppendFormat("{0} ", toStr(ie.Current));
                }
            }


            string s = ms_bld.ToString();
            ms_bld.Remove(0, ms_bld.Length);
            return s;
        }

        //public static T RandomGetElem<T>(IList<T> lst) //move to List extension class
        //{
        //    int cnt = lst.Count;
        //    int idx = Random.Range(0, cnt);
        //    return lst[idx];
        //}

        /// <summary>
        /// given a value, return [0, range], the calculate simulates ping pong,
        /// could process negative value
        /// e.g.: range = 1, value = 1.4, => result 0.6
        ///                  value = 2.3  => result 0.3
        /// </summary>
        public static float PingPongnize(float value, float range)
        {
            float bigRange = 2 * range;
            float v = value % bigRange;
            if (v < 0)
                v += bigRange;

            if (v > range)
            {
                v = bigRange - v;
            }

            return v;
        }

        public static void ShowCursor(bool bShow)
        {
#if ! UNITY_5
            Cursor.visible = bShow;
#else
            Cursor.visible = bShow;
#endif
        }

        public static bool IsShowCursor()
        {
#if ! UNITY_5
            return Cursor.visible;
#else
            return Cursor.visible;
#endif
        }

        public static void LockCursor(bool bLock)
        {
            //Dbg.Log("LockCursor: {0}", bLock);
#if ! UNITY_5
            Screen.lockCursor = bLock;
#else
            Cursor.lockState = bLock ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !bLock;
#endif
        }

        public static bool IsLockCursor()
        {
#if ! UNITY_5
            return Screen.lockCursor;
#else
            return Cursor.lockState == CursorLockMode.Locked;
#endif
        }

        /// <summary>
        /// check if a point is inside the camera's range, 
        /// NOTE: in range doesn't mean visible or not be blocked
        /// </summary>
        public static bool IsInCameraRange(Vector3 wpos, Camera cam)
        {
            Vector3 vpt = cam.WorldToViewportPoint(wpos);

            return 0 <= vpt.x && vpt.x <= 1f &&
                   0 <= vpt.y && vpt.y <= 1f &&
                   cam.nearClipPlane <= vpt.z && vpt.z <= cam.farClipPlane;
        }

        /// <summary>
        /// return the value with bigger abs
        /// </summary>
        public static float AbsMax(float lhs, float rhs)
        {
            float l = Mathf.Abs(lhs);
            float r = Mathf.Abs(rhs);

            return l > r ? lhs : rhs;
        }

        /// <summary>
        /// return the value with smaller abs
        /// </summary>
        public static float AbsMin(float lhs, float rhs)
        {
            float l = Mathf.Abs(lhs);
            float r = Mathf.Abs(rhs);

            return l < r ? lhs : rhs;
        }

        /// <summary>
        /// check if two vectors form acute angle ( less Than 90deg )
        /// </summary>
        public static bool IsAcuteAngle(Vector3 a, Vector3 b)
        {
            float dot = Vector3.Dot(a, b);
            return dot > 0;
        }

        /// <summary>
        /// check if two vectors form obtuse angle ( greater Than 90deg )
        /// </summary>
        public static bool IsObtuseAngle(Vector3 a, Vector3 b)
        {
            float dot = Vector3.Dot(a, b);
            return dot < 0;
        }

        /// <summary>
        /// if tr is null then return dir, 
        /// else tr.TransformDirection(dir)
        /// </summary>
        public static Vector3 TransformDirection(Transform tr, Vector3 dir)
        {
            if (tr == null)
                return dir;
            else
                return tr.TransformDirection(dir);
        }

        /// <summary>
        /// if tr is null then return dir, 
        /// else tr.InverseTransformDirection(dir)
        /// </summary>
        public static Vector3 InverseTransformDirection(Transform tr, Vector3 dir)
        {
            if (tr == null)
                return dir;
            else
                return tr.InverseTransformDirection(dir);
        }

        /// <summary>
        /// return <p0,p1> dot <p0,p2>
        /// </summary>
        public static float VecDot(Vector3 p0, Vector3 p1, Vector3 p2, bool normalize = true)
        {
            Vector3 p01 = p1 - p0;
            Vector3 p02 = p2 - p0;
            if (normalize)
            {
                p01.Normalize();
                p02.Normalize();
            }
            return Vector3.Dot(p01, p02);
        }

        /// <summary>
        /// return <p0,p1> dot <p2,p3>
        /// </summary>
        public static float VecDot(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, bool normalize = true)
        {
            Vector3 p01 = p1 - p0;
            Vector3 p23 = p3 - p2;
            if (normalize)
            {
                p01.Normalize();
                p23.Normalize();
            }
            return Vector3.Dot(p01, p23);
        }

        /// <summary>
        /// return <p0, p1> cross <p0, p2>
        /// </summary>
        public static Vector3 VecCross(Vector3 p0, Vector3 p1, Vector3 p2, bool normalize = true)
        {
            Vector3 p01 = p1 - p0;
            Vector3 p02 = p2 - p0;
            Vector3 cross = Vector3.Cross(p01, p02);
            if (normalize)
                cross.Normalize();

            return cross;
        }

        /// <summary>
        /// return <p0,p1> cross <p2,p3>
        /// </summary>
        public static Vector3 VecCross(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, bool normalize = true)
        {
            Vector3 p01 = p1 - p0;
            Vector3 p23 = p3 - p2;
            Vector3 cross = Vector3.Cross(p01, p23);
            if (normalize)
                cross.Normalize();

            return cross;
        }

        /// <summary>
        /// calculate the angle of (from -> to), and the related rotation axis,
        /// the axis would be near "hintAxis"
        /// if hintAxis is coplanar with from, to, then axis will be the ('from' X 'to')
        /// the returned angle is in [-180f, 180f]
        /// </summary>
        public static float ToAngleAxis(Vector3 from, Vector3 to, Vector3 hintAxis)
        {
            Vector3 tmpAxis;
            return ToAngleAxis(from, to, hintAxis, out tmpAxis);
        }
        public static float ToAngleAxis(Vector3 from, Vector3 to, Vector3 hintAxis, out Vector3 axis)
        {
            Quaternion q = Quaternion.FromToRotation(from, to);
            float angle = 0f;
            q.ToAngleAxis(out angle, out axis);
            if (Misc.IsObtuseAngle(axis, hintAxis))
            {
                angle = -angle;
                axis = -axis;
            }

            angle = Misc.NormalizeAnglePI(angle);

            return angle;
        }

        /// <summary>
        /// slerp between two vector3 representing two euler angles
        /// </summary>
        public static Vector3 EulerSlerp(Vector3 eulerFrom, Vector3 eulerTo, float t)
        {
            Quaternion q0 = Quaternion.Euler(eulerFrom);
            Quaternion q1 = Quaternion.Euler(eulerTo);
            Quaternion q = Quaternion.Slerp(q0, q1, t);
            Vector3 euler = q.eulerAngles;
            return euler;
        }

        #region "Lerp"
        /// these functions will not clamp01

        public static float InverseLerp(float from, float to, float v)
        {
            if (Mathf.Approximately(from, to))
                return 0;

            return (v - from) / (to - from);
        }

        public static float Lerp(float from, float to, float t)
        {
            return from + (to - from) * t;
        }

        public static Vector2 Lerp(Vector2 from, Vector2 to, float t)
        {
            return from + (to - from) * t;
        }

        public static Vector3 Lerp(Vector3 from, Vector3 to, float t)
        {
            return from + (to - from) * t;
        }

        #endregion "Lerp"

        #region "create list"

        /// <summary>
        /// new an array, and create each element in array
        /// </summary>
        public static T[] CreateArray<T>(int sz) where T : class, new()
        {
            T[] arr = new T[sz];
            for (int i = 0; i < sz; ++i)
            {
                arr[i] = new T();
            }
            return arr;
        }
        public static T[] CreateArray<T>(int sz, System.Func<T> initor)
        {
            T[] arr = new T[sz];
            for (int i = 0; i < sz; ++i)
            {
                arr[i] = initor();
            }
            return arr;
        }

        public static List<T> CreateList<T>(int sz)
        {
            List<T> lst = new List<T>();
            lst.Resize(sz);
            return lst;
        }

        public static List<T> CreateList<T>(int sz, System.Func<T> initor) 
        {
            List<T> lst = new List<T>();
            for (int i = 0; i < sz; ++i)
            {
                var elem = initor();
                lst.Add(elem);
            }
            return lst;
        }

        #endregion "create list"

        #region "bit manipulation"

        public static int CountOnes(uint v)
        {
            uint c = 0;
            v = v - ((v >> 1) & 0x55555555);                    // reuse input as temporary
            v = (v & 0x33333333) + ((v >> 2) & 0x33333333);     // temp
            c = ((v + (v >> 4) & 0xF0F0F0F) * 0x1010101) >> 24; // count
            return (int)c;
        }

        /// <summary>
        /// count the consecutive zero bits on the right side
        /// </summary>
        public static int CountZeroOnRight(uint v)
        {
            int c = 32;
            if (v != 0) c--;
            if ((v & 0x0000FFFF) != 0) { c -= 16; v &= 0x0000FFFF; }
            if ((v & 0x00FF00FF) != 0) { c -= 8; v &= 0x00FF00FF; }
            if ((v & 0x0F0F0F0F) != 0) { c -= 4; v &= 0x0F0F0F0F; }
            if ((v & 0x33333333) != 0) { c -= 2; v &= 0x33333333; }
            if ((v & 0x55555555) != 0) { c -= 1; v &= 0x55555555; }

            return c;
        }
        /// <summary>
        /// given a value 'val', and how many bit we need from it,
        /// return a value that with at most 'needBit' bit of 1 extracting from 'val'
        /// </summary>
        public static int RandomGetBitMask(int val, int needBit)
        {
            int ret = 0;
            if (needBit <= 0)
                return ret;

            int leftOnes = CountOnes((uint)val);

            int m = 1;
            for(int i=0; i<32; ++i)
            {
                if( (m & val) != 0)
                {
                    if( Random.value < ((float)needBit) / leftOnes )
                    {
                        ret |= m;
                        needBit--;
                        if (needBit <= 0)
                            break;
                    }

                    leftOnes--;
                }
                m <<= 1;
            }

            return ret;
        }


        #endregion "bit manipulation"

    }

    

    public class PhysxUtil
    {
        public static bool RaycastHitPos(out Vector3 pos, float maxDist = Mathf.Infinity, int layerMask = Physics.DefaultRaycastLayers)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            return RaycastHitPos(out pos, ray, maxDist, layerMask);
        }

        public static bool RaycastHitPos(out Vector3 pos, Vector3 from, Vector3 dir, 
            float maxDist = Mathf.Infinity, int layerMask = Physics.DefaultRaycastLayers)
        {
            pos = Vector3.zero;
            RaycastHit hit;

            bool bHit = Physics.Raycast(from, dir, out hit, maxDist, layerMask);
            if(bHit)
            {
                pos = hit.point;
            }

            return bHit;
        }

        public static bool RaycastHitPos(out Vector3 pos, Ray ray, float maxDist = Mathf.Infinity, int layerMask = Physics.DefaultRaycastLayers)
        {
            pos = Vector3.zero;
            RaycastHit hit;
            bool bHit = Physics.Raycast(ray, out hit, maxDist, layerMask);
            if( bHit )
            {
                pos = hit.point;
            }
            return bHit;
        }

        public static bool SphereHitPos(out Vector3 pos, Vector3 from, Vector3 dir, float radius,
            float maxDist = Mathf.Infinity, int layerMask = Physics.DefaultRaycastLayers)
        {
            pos = Vector3.zero;
            RaycastHit hit;

            bool bHit = Physics.SphereCast(from, radius, dir, out hit, maxDist, layerMask);
            if (bHit)
                pos = hit.point;

            return bHit;
        }

        public static GameObject RaycastObject()
        {
            return RaycastObject(Input.mousePosition);
        }

        public static GameObject RaycastObject(Vector3 screenPos, float maxDist = Mathf.Infinity, int layerMask = Physics.DefaultRaycastLayers)
        {
            GameObject ret = null;
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(screenPos);

            bool bHit = Physics.Raycast(ray, out hit, maxDist, layerMask);
            if (bHit)
            {
                ret = hit.collider.gameObject;
            }
            return ret;
        }
    }

    /// <summary>
    /// some utilities related to EAxis/EAxisD
    /// </summary>
    public class AxisUtil
    {
        public static void MapValTo(ref Vector3 vec, float v, EAxisD axis)
        {
            switch (axis)
            {
                case EAxisD.X: vec.x = v; break;
                case EAxisD.Y: vec.y = v; break;
                case EAxisD.Z: vec.z = v; break;
                case EAxisD.InvX: vec.x = -v; break;
                case EAxisD.InvY: vec.y = -v; break;
                case EAxisD.InvZ: vec.z = -v; break;
                case EAxisD.None: break;
                default: Dbg.LogErr("AxisUtil.MapValTo: unexpected axis: {0}", axis); break;
            }
        }
        public static void MapValTo(ref Vector2 vec, float v, EAxisD axis)
        {
            switch (axis)
            {
                case EAxisD.X: vec.x = v; break;
                case EAxisD.Y: vec.y = v; break;
                case EAxisD.InvX: vec.x = -v; break;
                case EAxisD.InvY: vec.y = -v; break;
                case EAxisD.None: break;
                default: Dbg.LogErr("AxisUtil.MapValTo: unexpected axis: {0}", axis); break;
            }
        }

        public static Vector2 MapValTo(ref Vector2 src, EAxisD xMapTo, EAxisD yMapTo)
        {
            Vector2 dst = Vector2.zero;
            MapValTo(ref dst, src.x, xMapTo);
            MapValTo(ref dst, src.y, yMapTo);
            return dst;
        }
        public static Vector3 MapValTo(ref Vector3 src, EAxisD xMapTo, EAxisD yMapTo, EAxisD zMapTo)
        {
            Vector3 dst = Vector3.zero;
            MapValTo(ref dst, src.x, xMapTo);
            MapValTo(ref dst, src.y, yMapTo);
            MapValTo(ref dst, src.z, zMapTo);
            return dst;
        }

        public static float InverseMapValTo(Vector3 inVal, EAxisD axis)
        {
            switch( axis )
            {
                case EAxisD.X: return inVal.x;
                case EAxisD.Y: return inVal.y;
                case EAxisD.Z: return inVal.z;
                case EAxisD.InvX: return -inVal.x;
                case EAxisD.InvY: return -inVal.y;
                case EAxisD.InvZ: return -inVal.z;
                default: Dbg.LogErr("AxisUtil.InverseMapValTo: unexpected axis: {0}", axis); return 0;
            }
        }

        public static Vector3 GetTransformDirection(Transform tr, EAxisD axis)
        {
            switch( axis )
            {
                case EAxisD.X: return tr.right;
                case EAxisD.Y: return tr.up;
                case EAxisD.Z: return tr.forward;
                case EAxisD.InvX: return -tr.right;
                case EAxisD.InvY: return -tr.up;
                case EAxisD.InvZ: return -tr.forward;
                default: Dbg.LogErr("AxisUtil.GetTransformDirection: unexpected axis: {0}", axis); return Vector3.zero;
            }
        }

        /// <summary>
        /// find out the EAxisD as the result of 'x cross y'
        /// </summary>
        public static EAxisD Cross(EAxisD x, EAxisD y)
        {
            switch( x )
            {
                case EAxisD.X:
                case EAxisD.InvX:
                    {
                        switch( y )
                        {
                            case EAxisD.X: return EAxisD.None;
                            case EAxisD.Y: return x == EAxisD.X ? EAxisD.Z : EAxisD.InvZ;
                            case EAxisD.Z: return x == EAxisD.X ? EAxisD.InvY : EAxisD.Y;
                            case EAxisD.InvX: return EAxisD.None;
                            case EAxisD.InvY: return x == EAxisD.X ? EAxisD.InvZ : EAxisD.Z;
                            case EAxisD.InvZ: return x == EAxisD.X ? EAxisD.Y : EAxisD.InvY;
                        }
                    }
                    break;
                case EAxisD.Y:
                case EAxisD.InvY:
                    {
                        switch (y)
                        {
                            case EAxisD.X: return x == EAxisD.Y ? EAxisD.InvZ : EAxisD.Z;
                            case EAxisD.Y: return EAxisD.None;
                            case EAxisD.Z: return x == EAxisD.Y ? EAxisD.X : EAxisD.InvX;
                            case EAxisD.InvX: return x == EAxisD.Y ? EAxisD.Z : EAxisD.InvZ;
                            case EAxisD.InvY: return EAxisD.None;
                            case EAxisD.InvZ: return x == EAxisD.Y ? EAxisD.InvX : EAxisD.X;
                        }
                    }
                    break;
                case EAxisD.Z:
                case EAxisD.InvZ:
                    {
                        switch (y)
                        {
                            case EAxisD.X: return x == EAxisD.Z ? EAxisD.Y : EAxisD.InvY;
                            case EAxisD.Y: return x == EAxisD.Z ? EAxisD.InvX : EAxisD.X;
                            case EAxisD.Z: return EAxisD.None;
                            case EAxisD.InvX: return x == EAxisD.Z ? EAxisD.InvY : EAxisD.Y;
                            case EAxisD.InvY: return x == EAxisD.Z ? EAxisD.X : EAxisD.InvX;
                            case EAxisD.InvZ: return EAxisD.None;
                        }
                    }
                    break;
                default: break;
            }

            Dbg.LogErr("AxisUtil.Cross: aint be here... {0}, {1}", x, y);
            return EAxisD.None;
        }
    }

    /// <summary>
    /// data conversion
    /// </summary>
    public class DataUtil
    {
        public static int ParseAsInt(string s, int def = 0)
        {
            int ret = 0;
            bool bOk = Int32.TryParse(s, out ret);
            return bOk ? ret : def;
        }

        public static uint ParseAsUint(string s, uint def = 0)
        {
            uint ret = 0;
            bool bOk = UInt32.TryParse(s, out ret);
            return bOk ? ret : def;
        }

        public static float ParseAsFloat(string s, float def = 0)
        {
            float ret = 0;
            bool bOk = Single.TryParse(s, out ret);
            return bOk ? ret : def;
        }

        public static double ParseAsDouble(string s, double def = 0)
        {
            double ret = 0;
            bool bOk = Double.TryParse(s, out ret);
            return bOk ? ret : def;
        }

        public static bool ParseAsBool(string s, bool def = false)
        {
            bool ret = false;
            bool bOK = Boolean.TryParse(s, out ret);
            return bOK ? ret : def;
        }

        public static T ParseAsEnum<T>(string s, Type eType)
        {
            try{
                T ret = (T)Enum.Parse(eType, s);
                return ret;
            }catch(ArgumentException){
                Dbg.LogErr("DataUtil.ParseAsEnum: failed to parse {0} as {1}", s, eType.ToString());
                return default(T);
            }
        }


    }



    #if HAS_ZIP
    /// <summary>
    /// utility about zip/unzip
    /// </summary>
    public class ZUtil
    {
        public static void ZipFile(string inputFile, string outputFile)
        {
            Dbg.Assert(File.Exists(inputFile), "ZUtil.ZipFile: the inputfile not exist: {0}", inputFile);

            FastZip fzip = new FastZip();
            fzip.CreateZip(outputFile, Path.GetDirectoryName(inputFile), true, Path.GetFileName(inputFile));
        }

        public static void ZipDir(string inputDir, string outputFile)
        {
            Dbg.Assert(Directory.Exists(inputDir), "ZUtil.ZipDir: the inputDir not exist: {0}", inputDir);

            FastZip fzip = new FastZip();
            fzip.CreateZip(outputFile, inputDir, true, null);
        }

        public static void UnzipFile(string inputFile, string outBaseDir)
        {
            Dbg.Assert(File.Exists(inputFile), "ZUtil.UnzipFile: the inputFile not exist: {0}", inputFile);

            FastZip fzip = new FastZip();
            if (!Directory.Exists(outBaseDir))
            {
                Directory.CreateDirectory(outBaseDir);
            }
            fzip.ExtractZip(inputFile, outBaseDir, FastZip.Overwrite.Always, null, null, null, false);
        }

        public static void UnzipStream(Stream iStream, string outBaseDir)
        {
            if (!Directory.Exists(Path.GetDirectoryName(outBaseDir)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outBaseDir));
            }

            ZipInputStream zipInputStream = new ZipInputStream(iStream);
            ZipEntry zipEntry = zipInputStream.GetNextEntry();
            while (zipEntry != null)
            {
                String entryFileName = zipEntry.Name;
                // to remove the folder from the entry:- entryFileName = Path.GetFileName(entryFileName);
                // Optionally match entrynames against a selection list here to skip as desired.
                // The unpacked length is available in the zipEntry.Size property.

                byte[] buffer = new byte[4096];     // 4K is optimum

                // Manipulate the output filename here as desired.
                String fullZipToPath = PathUtil.Combine(outBaseDir, entryFileName);
                string directoryName = Path.GetDirectoryName(fullZipToPath);
                if (directoryName.Length > 0)
                    Directory.CreateDirectory(directoryName);

                // Unzip file in buffered chunks. This is just as fast as unpacking to a buffer the full size
                // of the file, but does not waste memory.
                // The "using" will close the stream even if an exception occurs.
                using (FileStream streamWriter = File.Create(fullZipToPath))
                {
                    StreamUtils.Copy(zipInputStream, streamWriter, buffer);
                }
                zipEntry = zipInputStream.GetNextEntry();
            }

        }

    }
    #endif

#if HAS_POOL
    /// <summary>
    /// alloc/dealloc object by pool
    /// </summary>
    public class Mem
    {
        public static T New<T>() where T : class, new()
        {
            ObjectPool<T> pool = ObjectPool<T>.ForceAcquire();
            return pool.Spawn();
        }

        public static void Del<T>(T obj) where T : class, new()
        {
            ObjectPool<T> pool = ObjectPool<T>.ForceAcquire();
            pool.Despawn(obj);
        }

        public static void DelV(object obj)
        {
            var tp = obj.GetType().TypeHandle;
            IPool pool = PoolMgr.Instance.GetTypePool(tp);
            pool.Despawn(obj);
        }

        public static void SetResetAction<T>(Action<T> resetAction) where T : class, new()
        {
            ObjectPool<T> pool = ObjectPool<T>.ForceAcquire();
            pool.ResetAction = resetAction;
        }

        public static void SetFirstInitAction<T>(Action<T> initAction) where T : class, new()
        {
            ObjectPool<T> pool = ObjectPool<T>.ForceAcquire();
            pool.InitAction = initAction;
        }

        public static void SetLivingObjCntWarning<T>(int thres, int incr) where T : class, new()
        {
            ObjectPool<T> pool = ObjectPool<T>.ForceAcquire();
            pool.WarningCnt = thres;
            pool.WarningCntIncr = incr;
        }

        public static void CleanUnused<T>() where T : class, new()
        {
            ObjectPool<T> pool = ObjectPool<T>.ForceAcquire();
            pool.CleanUnused();
        }

        //public static GameObject NewPrefabGO(string poolName, Transform parent)
        //{
        //    ResPrefabPool<GameObject> pool = PoolMgr.Instance.Get(poolName) as ResPrefabPool<GameObject>;
        //    if (pool == null)
        //    {
        //        pool = new ResPrefabPool<GameObject>(poolName);
        //        PoolMgr.Instance.Add(pool);
        //    }

        //    GameObject obj = pool.Spawn();
        //    obj.transform.parent = parent;

        //    return obj;
        //}

        //public static T NewPrefab<T>(string poolName) where T : UnityEngine.Object
        //{
        //    ResPrefabPool<T> pool = PoolMgr.Instance.Get(poolName) as ResPrefabPool<T>;
        //    if (pool == null)
        //    {
        //        pool = new ResPrefabPool<T>(poolName);
        //        PoolMgr.Instance.Add(pool);
        //    }

        //    T obj = pool.Spawn();

        //    return obj;
        //}

        //public static void DelPrefabGO(GameObject obj)
        //{
        //    PoolTicket ticket = obj.GetComponent<PoolTicket>();
        //    Dbg.Assert(ticket != null, "Mem.DelPrefabGO: the obj doesn't have PoolTicket");
        //    ResPrefabPool<GameObject> pool = PoolMgr.Instance.Get(ticket.PoolName) as ResPrefabPool<GameObject>;
        //    Dbg.Assert(null != pool, "Mem.DelPrefabGO: failed to get pool: {0}", ticket.PoolName);
        //    pool.Despawn(obj);
        //}
    }

    public class MemC
    {
        public static T New<T>() where T : class
        {
            ObjectPoolC<T> pool = ObjectPoolC<T>.ForceAcquire();
            return pool.Spawn();
        }

        public static void Del<T>(T obj) where T : class
        {
            ObjectPoolC<T> pool = ObjectPoolC<T>.ForceAcquire();
            pool.Despawn(obj);
        }

        public static void SetCreateFunc<T>(Func<T> createFunc) where T : class
        {
            ObjectPoolC<T> pool = ObjectPoolC<T>.ForceAcquire();
            pool.CreateFunc = createFunc;
        }

        public static void CleanUnused<T>() where T : class
        {
            ObjectPoolC<T> pool = ObjectPoolC<T>.ForceAcquire();
            pool.CleanUnused();
        }
    }
#endif

    /// <summary>
    /// get UID in a multi-queue style
    /// </summary>
    public class UID
    {        
        public enum Q{
            DEFAULT,
            CR,  //coroutine            
            Q_CNT
        };

        public static uint[] s_ids = Enumerable.Repeat((uint)0, (int)Q.Q_CNT).ToArray();

        public static uint Get(Q que = Q.DEFAULT)
        {
            return s_ids[(int)que]++;
        }
        public static string GetAsString(Q que)
        {
            uint v = Get(que);
            return v.ToString();
        }

        /// <summary>
        /// peek next id of specified queue
        /// </summary>
        public static uint Peek(Q que)
        {
            return s_ids[(int)que];
        }
    }

    /// <summary>
    /// utility pair
    /// </summary>
    [Serializable]
    public class Pair<T, U>
    {
        public T first;
        public U second;

        public Pair() { }
        public Pair(T f, U s) { first = f; second = s; }

        public T x { get { return first; } set { first = value; } }
        public U y { get { return second; } set { second = value; } }
    }
    
    /// <summary>
    /// some extra utilities on IO
    /// </summary>
    public class IOUtil
    {
        /// <summary>
        /// write to file, automatically create directories
        /// </summary>
        public static void WriteAllText(string pathName, string content)
        {
#if (!UNITY_WEBPLAYER && !UNITY_WINRT && !UNITY_IPHONE)
            Directory.CreateDirectory(Path.GetDirectoryName(pathName));
            System.IO.File.WriteAllText(pathName, content, Encoding.UTF8);
#endif
        }

        /// <summary>
        /// write to file, automatically create directories
        /// </summary>
        public static void WriteAllBytes(string pathName, byte[] bytes)
        {
#if (!UNITY_WEBPLAYER && !UNITY_WINRT && !UNITY_IPHONE)
            Directory.CreateDirectory(Path.GetDirectoryName(pathName));
            File.WriteAllBytes(pathName, bytes);
#endif
        }
    }


    public class PathUtil
    {
        public static string GetDocumentPath()
        {
            return Application.persistentDataPath;
        }

        public static string GetDocumentPath(string filename)
        {
            return Combine (Application.persistentDataPath, filename);
        }

        /// <summary>
        /// combine the paths, and force use the directory separator used by Resources.Load ('/')
        /// </summary>
        public static string Combine(string path1, string path2)
        {
            string combined = Path.Combine(path1, path2);
            return ForceForwardSlash(combined);
        }

        /// <summary>
        /// strip extension if the path has
        /// </summary>
        public static string StripExtension(string path)
        {
            string dir = Path.GetDirectoryName(path);
            string nameNoExt = Path.GetFileNameWithoutExtension(path);
            if( dir.Length == 0 )
            {
                return nameNoExt;
            }
            else
            {
                return dir + "/" + nameNoExt;
            }
        }

        /// <summary>
        /// convert all "\" to "/" in path
        /// </summary>
        public static string ForceForwardSlash(string path)
        {
            return path.Replace('\\', '/');
        }

        /// <summary>
        /// given "xxx/yyy/A.asset", return "xxx/yyy/B.asset"
        /// </summary>
        public static string ReplaceFileNameWithoutExtension(string path, string newFileName)
        {
            string ext = Path.GetExtension(path);
            return Path.GetDirectoryName(path) + "/" + newFileName + 
                ((ext.Length != 0) ? ext : "");
        }

        /// <summary>
        /// given "xxx/yyy/A.asset", return "xxx/yyy/A$append$.asset"
        /// </summary>
        public static string AppendFileNameWithoutChangeExtension(string path, string append)
        {
            string ext = Path.GetExtension(path);
            string dir = Path.GetDirectoryName(path);
            string nameNoExt = Path.GetFileNameWithoutExtension(path);
            return dir + "/" + nameNoExt + append + ((ext.Length != 0) ? ext : "");
        }

        /// <summary>
        /// given "xxx/yyy/A.asset" return "xxx/yyy/A$append$$newExt$"
        /// 
        /// the newext should have '.' at head
        /// </summary>
        public static string AppendFileNameWithChangeExtension(string path, string append, string newext)
        {
            string dir = Path.GetDirectoryName(path);
            string nameNoExt = Path.GetFileNameWithoutExtension(path);
            return dir + "/" + nameNoExt + append + newext;
        }

        #if UNITY_EDITOR
        /// <summary>
        /// editor used method;
        /// given "XXXXX", return "f:/Project/Game/Assets/Resources/XXXXX"
        /// </summary>
        public static string ResourcesPath2FullPath(string pathName)
        {
            return string.Format("{0}/Resources/{1}", Application.dataPath, pathName);
        }

        /// <summary>
        /// given "XXXXX", return "Assets/Resources/XXXXX"
        /// </summary>
        public static string ResourcesPath2ProjectPath(string pathName)
        {
            return PathUtil.Combine("Assets/Resources", pathName);
        }

        /// <summary>
        /// given "Assets/Resources/XXXXX", return "XXXXX"
        /// </summary>
        public static string ProjectPath2ResourcesPath(string Prjpath)
        {
            Dbg.Assert(Prjpath.StartsWith("Assets/Resources"), "PathUtil.ProjectPath2ResourcesPath: invalid path");
            return Prjpath.Substring("Assets/Resources".Length+1);
        }

        /// <summary>
        /// given "f:/Project/Game/Assets/xxx" return "Assets/xxx"
        /// if not right, then return original string
        /// </summary>
        public static string FullPath2ProjectPath(string fullpath, bool silent = true)
        {
            string prjPath = GetProjectPath();
            bool ok = fullpath.StartsWith(prjPath);
            if (!ok )
            {
                if( !silent )
                    Dbg.Assert(fullpath.StartsWith(prjPath), "PathUtil.FullPath2ProjectPath: The path is not in Assets directory: {0}", fullpath);
                return fullpath;
            }
            else
            {
                return fullpath.Substring(prjPath.Length);
            }
        }

        public static string GetProjectPath()
        {
            string prjPath = Application.dataPath.Substring(0, Application.dataPath.Length - 6);
            return prjPath;
        }

        public static string ProjectPath2FullPath(string prjPath)
        {
            return GetProjectPath() + prjPath;
        }

        #endif
    }

#if HAS_JSON
    //public class Json
    //{
    //    private static JsonWriter s_PrettyWriter;

    //    static Json(){
    //        s_PrettyWriter = new JsonWriter();
    //        s_PrettyWriter.PrettyPrint = true;
    //    }

    //    /// <summary>
    //    /// convert object to json string
    //    /// </summary>
    //    public static string ToStr(object obj) 
    //    {
    //        return ToStr(obj, true);
    //    }
    //    public static string ToStr(object obj, bool bIndented)
    //    {
    //        if( bIndented )
    //        {
    //            s_PrettyWriter.Reset();
    //            JsonMapper.ToJson(obj, s_PrettyWriter);
    //            string o = s_PrettyWriter.ToString();
    //            return o;
    //        }
    //        else
    //        {
    //            string o = JsonMapper.ToJson(obj);
    //            return o;
    //        }
    //    }

    //    /// <summary>
    //    /// convert json string to object
    //    /// </summary>
    //    public static T ToObj<T>(string str)
    //    {
    //        T o = JsonMapper.ToObject<T>(str);
    //        return o;
    //    }

    //    public static object ToObj(string str, Type t)
    //    {
    //        object o = JsonMapper.ToObject(str, t);
    //        return o;
    //    }

    //}

    /// <summary>
    /// LJson is better suited to handle non-structured json data
    /// </summary>
    public class LJson
    {
        private static LitJson.JsonWriter s_PrettyWriter;

        static LJson()
        {
            s_PrettyWriter = new LitJson.JsonWriter();
            s_PrettyWriter.PrettyPrint = true;
        }

        /// <summary>
        /// convert object to json string
        /// </summary>
        public static string ToStr(object obj)
        {
            return ToStr(obj, true);
        }
        public static string ToStr(object obj, bool bIndented)
        {
            if (bIndented)
            {
                s_PrettyWriter.Reset();
                JsonMapper.ToJson(obj, s_PrettyWriter);
                string o = s_PrettyWriter.ToString();
                return o;
            }
            else
            {
                string o = JsonMapper.ToJson(obj);
                return o;
            }
        }

        /// <summary>
        /// convert json string to object
        /// </summary>
        public static T ToObj<T>(string str)
        {
            T o = JsonMapper.ToObject<T>(str);
            return o;
        }

        public static object ToObj(string str, Type t)
        {
            object o = JsonMapper.ToObject(str, t);
            return o;
        }

    }

#if HAS_JSON_DOTNET

    public class Json
    {
        private static JsonSerializerSettings _settings = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto, ReferenceLoopHandling = ReferenceLoopHandling.Ignore };

        public static JsonSerializerSettings defSettings { get { return _settings; } }

        public static string ToStr(object obj)
        {
            return ToStr(obj, true);
        }
        public static string ToStr(object obj, bool indented)
        {
            return JsonConvert.SerializeObject(obj, indented ? Formatting.Indented : Formatting.None, _settings);
        }

        public static T ToObj<T>(string str)
        {
            T o = JsonConvert.DeserializeObject<T>(str, _settings);
            return o;
        }

        public static object ToObj(string str, Type tp)
        {
            return JsonConvert.DeserializeObject(str, tp, _settings);
        }

    }

#else

    public class Json
    {
        private static readonly fsSerializer _serializer;

        static Json()
        {
            _serializer = new fsSerializer();
//#if UNITY_EDITOR
//            _serializer.AddConverter(new ObjectConverter());
//#endif
        }

        /// <summary>
        /// convert object to json string
        /// </summary>
        public static string ToStr(object obj)
        {
            return ToStr(obj, true);
        }
        public static string ToStr(object obj, bool bIndented)
        {
            fsData fsdata;
            _serializer.TrySerialize(obj.GetType(), obj, out fsdata).AssertSuccess();

            if(bIndented)
                return fsJsonPrinter.PrettyJson(fsdata);
            else
                return fsJsonPrinter.CompressedJson(fsdata);
        }

        /// <summary>
        /// convert json string to object
        /// </summary>
        public static T ToObj<T>(string str)
        {
            T o = default(T);
            fsData fsdata = fsJsonParser.Parse(str);
            _serializer.TryDeserialize<T>(fsdata, ref o).AssertSuccess();
            return o;
        }

        public static object ToObj(string str, Type t)
        {
            object o = null;
            fsData fsdata = fsJsonParser.Parse(str);
            _serializer.TryDeserialize(fsdata, t, ref o).AssertSuccess();
            return o;
        }


    }
#endif

    /// <summary>
    /// dump object to file
    /// </summary>
    public class Dumper
    {
        public static void Dump<T>(T obj)
        {
            string filename = DateTime.Now.ToString("yyyyMMdd_hhmmss") + ".dump";
            Dump(obj, filename);
        }

        public static void Dump<T>(T obj, string path)
        {
#if !UNITY_WINRT
            //string serialized = JsonConvert.SerializeObject(obj, Formatting.Indented);
            string serialized = Json.ToStr(obj, true);
            using (StreamWriter sw = new StreamWriter(path))
            {
                sw.Write(serialized);
            }
#endif
        }


    }
#endif

    /// <summary>
    /// used by QUtil.LookAtXXX series function, 
    /// to specify how to calc Quaternion with given parameters
    /// </summary>
    public enum LookAtMtd
    {
        XY, //lookat x, reference y 
        XZ, //lookat x, reference z
        YX, //lookat y, reference x
        YZ, //lookat y, reference z
        ZX, //lookat z, reference x
        ZY  //lookat z, reference y
    }

    public class QUtil
    {
        public static Quaternion LookAt(EAxisD fwdAxis, EAxisD upAxis, Vector3 fwdDir)
        {
            return LookAt(fwdAxis, upAxis, fwdDir, Vector3.up);
        }
        public static Quaternion LookAt(EAxisD fwdAxis, EAxisD upAxis, Vector3 fwdDir, Vector3 upDir)
        {
            LookAtMtd mtd = LookAtMtd.ZY;
            if (fwdAxis == EAxisD.X || fwdAxis == EAxisD.InvX)
            {
                if (upAxis == EAxisD.Y || upAxis == EAxisD.InvY) mtd = LookAtMtd.XY;
                if (upAxis == EAxisD.Z || upAxis == EAxisD.InvZ) mtd = LookAtMtd.XZ;
            }
            else if (fwdAxis == EAxisD.Y || fwdAxis == EAxisD.InvY)
            {
                if (upAxis == EAxisD.X || upAxis == EAxisD.InvX) mtd = LookAtMtd.YX;
                if (upAxis == EAxisD.Z || upAxis == EAxisD.InvZ) mtd = LookAtMtd.YZ;
            }
            else if (fwdAxis == EAxisD.Z || fwdAxis == EAxisD.InvZ)
            {
                if (upAxis == EAxisD.X || upAxis == EAxisD.InvX) mtd = LookAtMtd.ZX;
                if (upAxis == EAxisD.Y || upAxis == EAxisD.InvY) mtd = LookAtMtd.ZY;
            }
            else
            {
                Dbg.LogErr("QUtil.LookAt: lookAxis: error: {0}", fwdAxis);
            }

            Vector3 _lookDir = (fwdAxis == EAxisD.X || fwdAxis == EAxisD.Y || fwdAxis == EAxisD.Z) ? fwdDir : -fwdDir;
            Vector3 _upDir = (upAxis == EAxisD.X || upAxis == EAxisD.Y || upAxis == EAxisD.Z) ? upDir : -upDir;

            Quaternion q = QUtil.LookAt(mtd, _lookDir, _upDir);

            return q;
        }

        public static Quaternion LookAt(LookAtMtd mtd, Vector3 lookDir, Vector3 refDir)
        {
            switch (mtd)
            {
                case LookAtMtd.XY: return LookAtXY(lookDir, refDir);
                case LookAtMtd.XZ: return LookAtXZ(lookDir, refDir);
                case LookAtMtd.YX: return LookAtYX(lookDir, refDir);
                case LookAtMtd.YZ: return LookAtYZ(lookDir, refDir);
                case LookAtMtd.ZX: return LookAtZX(lookDir, refDir);
                case LookAtMtd.ZY: return LookAtZY(lookDir, refDir);
                default:
                    Dbg.LogErr("QUtil.LookAt: unexpected method: {0}", mtd);
                    return Quaternion.identity;
            }
        }

        public static Quaternion LookAtZY(Vector3 lookDir, Vector3 refDir)
        {
            Vector3 newZ = lookDir;
            Vector3 newY = refDir; //ref
            Vector3 newX; //calc

            if (newZ == Vector3.up && newY == Vector3.up)
            {
                newY = -Vector3.forward; //ref
                newX = Vector3.right; //calc
            }
            else
            {
                newX = Vector3.Cross(newY, newZ); //calc
                if (newX == Vector3.zero)
                {
                    newY = Vector3.up; //ref
                    newX = Vector3.Cross(newY, newZ); //calc
                }
                newY = Vector3.Cross(newZ, newX); //recalc ref
            }

            Quaternion q = Quaternion.LookRotation(newZ, newY);
            return q;
        }

        public static Quaternion LookAtZX(Vector3 lookDir, Vector3 refDir)
        {
            Vector3 newZ = lookDir;
            Vector3 newX = refDir; //ref
            Vector3 newY; //calc

            if (newZ == Vector3.right && newX == Vector3.right)
            {
                newX = -Vector3.forward; //ref
                newY = Vector3.up; //calc
            }
            else
            {
                newY = Vector3.Cross(newZ, newX); //calc
                if (newY == Vector3.zero)
                {
                    newX = Vector3.right; //ref
                    newY = Vector3.Cross(newZ, newX); //calc
                }
                newX = Vector3.Cross(newY, newZ); //recalc ref
            }

            Quaternion q = Quaternion.LookRotation(newZ, newY);
            return q;
        }

        public static Quaternion LookAtXY(Vector3 lookDir, Vector3 refDir)
        {
            Vector3 newX = lookDir;
            Vector3 newY = refDir;
            Vector3 newZ; //calc-vec

            if (newX == Vector3.up && newY == Vector3.up)
            {
                newY = -Vector3.forward; //ref
                newZ = -Vector3.right; //calc
            }
            else
            {
                newZ = Vector3.Cross(newX, newY); //calc
                if (newZ == Vector3.zero)
                {
                    newY = Vector3.up; //ref
                    newZ = Vector3.Cross(newX, newY); //calc
                }
                newY = Vector3.Cross(newZ, newX); //recalc ref-vec
            }

            Quaternion q = Quaternion.LookRotation(newZ, newY);
            return q;
        }

        public static Quaternion LookAtXZ(Vector3 lookDir, Vector3 refDir)
        {
            Vector3 newX = lookDir;
            Vector3 newZ = refDir;
            Vector3 newY; //calc-vec

            if (newX == Vector3.forward && newZ == Vector3.forward)
            {
                newZ = -Vector3.right; //ref-vec
                newY = Vector3.up; //calc-vec
            }
            else
            {
                newY = Vector3.Cross(newZ, newX); //calc-vec
                if (newY == Vector3.zero)
                {
                    newZ = Vector3.forward; //ref-vec
                    newY = Vector3.Cross(newZ, newX); //calc-vec
                }
                newZ = Vector3.Cross(newX, newY); //recalc ref-vec
            }

            Quaternion q = Quaternion.LookRotation(newZ, newY);
            return q;
        }

        public static Quaternion LookAtYX(Vector3 lookDir, Vector3 refDir)
        {
            Vector3 newY = lookDir;
            Vector3 newX = refDir;
            Vector3 newZ; //calc-vec

            if (newY == Vector3.right && newX == Vector3.right)
            {
                newX = Vector3.down; //ref-vec
                newZ = Vector3.forward; //calc-vec
            }
            else
            {
                newZ = Vector3.Cross(newX, newY); //calc-vec
                if (newZ == Vector3.zero) //calc-vec
                {
                    newX = Vector3.right; //ref-vec
                    newZ = Vector3.Cross(newX, newY); //calc-vec
                }
                newX = Vector3.Cross(newY, newZ); //recalc ref-vec
            }

            Quaternion q = Quaternion.LookRotation(newZ, newY);
            return q;
        }

        public static Quaternion LookAtYZ(Vector3 lookDir, Vector3 refDir)
        {
            Vector3 newY = lookDir;
            Vector3 newZ = refDir;
            Vector3 newX; //calc-vec

            if (newY == Vector3.forward && newZ == Vector3.forward)
            {
                newZ = Vector3.down; //ref-vec
                newX = Vector3.right; //calc-vec
            }
            else
            {
                newX = Vector3.Cross(newY, newZ); //calc-vec
                if (newX == Vector3.zero) //calc-vec
                {
                    newZ = Vector3.forward; //ref-vec
                    newX = Vector3.Cross(newY, newZ); //calc-vec
                }
                newZ = Vector3.Cross(newX, newY); //recalc ref-vec
            }

            Quaternion q = Quaternion.LookRotation(newZ, newY);
            return q;
        }
        
        /// <summary>
        /// make quaternion's fwd and up vector to reflect along (1,0,0)
        /// </summary>
        public static Quaternion ReflectX_YZ(Quaternion q)
        {
            Vector3 fwd = q * Vector3.forward;
            Vector3 up = q * Vector3.up;

            Vector3 newFwd = Vector3.Reflect(fwd, Vector3.right);
            Vector3 newUp = Vector3.Reflect(up, Vector3.right);

            return Quaternion.LookRotation(newFwd, newUp);
        }

        public static Quaternion ReflectX_XZ(Quaternion q)
        {
            Vector3 fwd = q * Vector3.forward;
            Vector3 right = q * Vector3.right;

            Vector3 newFwd = Vector3.Reflect(fwd, Vector3.right);
            Vector3 newRight = Vector3.Reflect(right, Vector3.right);

            Vector3 newUp = Vector3.Cross(newFwd, newRight);
            return Quaternion.LookRotation(newFwd, newUp);
        }

        public static Quaternion ReflectX_XY(Quaternion q)
        {
            Vector3 right = q * Vector3.right;
            Vector3 up = q * Vector3.up;

            Vector3 newRight = Vector3.Reflect(right, Vector3.right);
            Vector3 newUp = Vector3.Reflect(up, Vector3.right);

            Vector3 newFwd = Vector3.Cross(newRight, newUp);
            return Quaternion.LookRotation(newFwd, newUp);
        }

        public static Quaternion Reflect_YZ(Quaternion q, Vector3 planeNormal)
        {
            Vector3 fwd = q * Vector3.forward;
            Vector3 up = q * Vector3.up;

            Vector3 newFwd = Vector3.Reflect(fwd, planeNormal);
            Vector3 newUp = Vector3.Reflect(up, planeNormal);

            return Quaternion.LookRotation(newFwd, newUp);
        }

        public static Quaternion Reflect_XZ(Quaternion q, Vector3 planeNormal)
        {
            Vector3 fwd = q * Vector3.forward;
            Vector3 right = q * Vector3.right;

            Vector3 newFwd = Vector3.Reflect(fwd, planeNormal);
            Vector3 newRight = Vector3.Reflect(right, planeNormal);

            Vector3 newUp = Vector3.Cross(newFwd, newRight);
            return Quaternion.LookRotation(newFwd, newUp);
        }

        public static Quaternion Reflect_XY(Quaternion q, Vector3 planeNormal)
        {
            Vector3 right = q * Vector3.right;
            Vector3 up = q * Vector3.up;

            Vector3 newRight = Vector3.Reflect(right, planeNormal);
            Vector3 newUp = Vector3.Reflect(up, planeNormal);

            Vector3 newFwd = Vector3.Cross(newRight, newUp);
            return Quaternion.LookRotation(newFwd, newUp);
        }

        public static Quaternion Normalize(Quaternion rot)
        {
            float norm = Mathf.Sqrt(rot.x * rot.x + rot.y * rot.y + rot.z * rot.z + rot.w * rot.w);
            Dbg.Assert(norm != 0, "QUtil.Normalize: the given Quaternion is all 0");
            rot.x /= norm;
            rot.y /= norm;
            rot.z /= norm;
            rot.w /= norm;

            return rot;
        }

        public static float Magnitude(Quaternion q)
        {
            float mag = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
            return mag;
        }

        /// <summary>
        /// return the angle of q, make into [-PI, PI]
        /// </summary>
        public static float AnglePI(Quaternion q)
        {
            Vector3 axis; float angle;
            q.ToAngleAxis(out angle, out axis);
            return Misc.NormalizeAnglePI(angle);
        }
    }

    public class RTUtil
    {
        public static Texture2D GetRTPixels(RenderTexture rt = null)
        {
            if (rt != null)
            {
                RenderTexture currentActiveRT = RenderTexture.active;
                RenderTexture.active = rt;
                Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false);
                tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
                tex.Apply();
                RenderTexture.active = currentActiveRT;
                return tex;
            }
            else
            {
                rt = RenderTexture.active;
                Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false);
                tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
                tex.Apply();
                return tex;
            }

        }

        public static Texture2D GetRTPixels(RenderTexture rt, Rect area)
        {
            Texture2D tex = GetRTPixels(rt);

            int w = (int)area.width;
            int h = (int)area.height;

            Color[] pix = tex.GetPixels((int)area.x, (int)area.y, w, h);
            Texture2D ret = new Texture2D(w, h);
            ret.SetPixels(0, 0, w, h, pix);
            ret.Apply();

            return ret;
        }

        public static Texture2D GetScreenTex()
        {
            return GetScreenTex(new Rect(0, 0, Screen.width, Screen.height));
        }

        public static void WriteScreenToFile(string path)
        {
#if !UNITY_WEBPLAYER
            Texture2D tex = GetScreenTex();
            byte[] img = tex.EncodeToJPG();
            System.IO.File.WriteAllBytes(path, img);
#endif
        }

        public static void WriteRTToFile(RenderTexture rt, string path)
        {
#if !UNITY_WEBPLAYER
            Texture2D tex = GetRTPixels(rt);
            byte[] img = tex.EncodeToPNG();
            System.IO.File.WriteAllBytes(path, img);
#endif
        }

        public static Texture2D GetScreenTex(Rect area)
        {
            int x = (int)area.x;
            int y = (int)area.y;
            int w = (int)area.width;
            int h = (int)area.height;

            Texture2D tex = new Texture2D(w, h, TextureFormat.ARGB32, false);
            tex.ReadPixels(new Rect(x, y, w, h), 0, 0);
            tex.Apply();
            return tex;
        }

        public static float DecodeFloatRGBA( Color enc )
        {	        
            return Vector4.Dot( enc, DECODE_FLOAT_RGBA);
        }

        public static float DecodeFloatRG(Color enc)
        {
            Vector2 rg = new Vector2(enc.r, enc.g);
            return Vector2.Dot(rg, DECODE_FLOAT_RG);
        }

        public static readonly Vector4 DECODE_FLOAT_RGBA = new Vector4(1.0f, 1f / 255.0f, 1f / 65025.0f, 1f / 16581375.0f);
        public static readonly Vector2 DECODE_FLOAT_RG = new Vector2(1.0f, 1f / 255.0f);
    }

    /// <summary>
    /// matrix utility
    /// </summary>
    public class MtxUtil
    {
        public static Vector3 GetPosition(ref Matrix4x4 m)
        {
            return m.GetColumn(3);
        }

        public static Quaternion GetRotation(ref Matrix4x4 m)
        {
            Vector3 z = m.GetColumn(2);
            Vector3 y = m.GetColumn(1);
            return Quaternion.LookRotation(z, y);
        }

        /// <summary>
        /// WARNING: this method requires that the matrix is a non-skewed matrix, a normal localMatrix of transform is fit
        /// If there's non-uniform matrix in hierarchy, and there's rotation, the result is usually wrong. it just cannot be represented by one TRS matrix
        /// </summary>
        public static Vector3 GetScale(ref Matrix4x4 m)
        {
            Vector3 z = m.GetColumn(2);
            Vector3 y = m.GetColumn(1);
            Vector3 x = m.GetColumn(0);

            Vector3 scale = Vector3.zero;
            scale.x = (x).magnitude;
            scale.y = (y).magnitude;
            scale.z = (z).magnitude;

            if (Vector3.Dot(Vector3.Cross(x, y), z) < 0) //right-handed, happen when <-n,-n,-n> scale
                scale *= -1f;

            return scale;
        }

        /// <summary>
        /// calculate a matrix with the given tr's worldPos & worldRot only
        /// </summary>
        public static Matrix4x4 GetMatrix_TR(Transform tr)
        {
            Vector3 worldPos = tr.position;
            Quaternion worldRot = tr.rotation;

            return Matrix4x4.TRS(worldPos, worldRot, Vector3.one);
        }
        public static Matrix4x4 GetMatrix_TR(Rigidbody rb)
        {
            Vector3 worldPos = rb.position;
            Quaternion worldRot = rb.rotation;
            return Matrix4x4.TRS(worldPos, worldRot, Vector3.one);
        }

        public static Matrix4x4 FlipX(ref Matrix4x4 m)
        {
            Matrix4x4 mtx = m;
            Vector4 r0 = mtx.GetRow(0);
            r0 *= -1f;
            mtx.SetRow(0, r0);
            return mtx;
        }

        /// <summary>
        /// matrix equation: AX = B, calculate X
        /// </summary>
        public static Matrix4x4 Solve_AX_eq_B(ref Matrix4x4 mA, ref Matrix4x4 mB)
        {
            return mA.inverse * mB;
        }
        public static Matrix4x4 Solve_AX_eq_B(Vector3 wposA, Quaternion wrotA, Vector3 wposB)
        {
            Matrix4x4 mA = Matrix4x4.TRS(wposA, wrotA, Vector3.one);
            Matrix4x4 mB = Matrix4x4.TRS(wposB, Quaternion.identity, Vector3.one);
            return mA.inverse * mB;
        }
        public static Matrix4x4 Solve_AX_eq_B(Vector3 wposA, Quaternion wrotA, Vector3 wposB, Quaternion wrotB)
        {
            Matrix4x4 mA = Matrix4x4.TRS(wposA, wrotA, Vector3.one);
            Matrix4x4 mB = Matrix4x4.TRS(wposB, wrotB, Vector3.one);
            return mA.inverse * mB;
        }

        /// <summary>
        /// A B   x    E  
        ///     *    =  
        /// C D   y    F 
        /// 
        /// calculate matrix [A,B/C,D] * [X,Y] = [E,F]
        /// if det == 0 return false, x y will be 0
        /// </summary>
        public static bool SolveEquation(float A, float B, float C, float D, float E, float F, out float x, out float y)
        {
            x = y = 0;
            float down = A * D - B * C;
            if (Mathf.Approximately(down, 0))
                return false;

            float A1 = D * E - B * F;
            float A2 = A * F - C * E;

            x = A1 / down;
            y = A2 / down;

            return true;
        }
    }

    /// <summary>
    /// geometry math util
    /// </summary>
    public class GeoUtil
    {
        /// <summary>
        /// try best to get Bounds for given GO, 
        /// 1. try renderer.bounds
        /// 2. try collider.bounds
        /// return zero bounds if none succeeds
        /// 
        /// NOTE: the bounds is in world-space
        /// </summary>
        public static Bounds CalculateBounds(GameObject go)
        {
            Renderer rd = go.GetComponent<Renderer>();
            if (rd != null)
                return rd.bounds;

            Collider col = go.GetComponent<Collider>();
            if (col != null)
                return col.bounds;

            return new Bounds();
        }

        /// <summary>
        /// return bounds in the local-space of `parent'
        /// </summary>
        public static Bounds CalculateBounds(GameObject go, Transform parent)
        {
            Bounds worldBd = CalculateBounds(go);
            if (parent != null)
                return parent.InverseTransformBounds(worldBd);
            else
                return worldBd;
        }

        /// <summary>
        /// make a ray with mainCamera & scrPos, and cast against the collider
        /// NOTE: left-bottom is <0,0>
        /// </summary>
        public static bool ColliderCast(Collider col, Vector2 scrPos, out RaycastHit hit)
        {
            Ray ray = Camera.main.ScreenPointToRay(scrPos);
            return ColliderCast(col, ray, out hit);
        }
        public static bool ColliderCast(Collider col, Ray ray, out RaycastHit hit)
        {
            return col.Raycast(ray, out hit, float.MaxValue);
        }

        /// <summary>
        /// return the distance of 'pt' to line defined by 'linePt0' & 'linePt1'
        /// </summary>
        public static float DistPointToLine(Vector3 pt, Vector3 linePt0, Vector3 linePt1)
        {
            Vector3 seg0 = pt - linePt0;
            Vector3 seg1 = linePt1 - linePt0;

            float cross = Vector3.Cross(seg0, seg1).magnitude;
            float len1 = seg1.magnitude;

            float D = cross / len1;
            return D;
        }

        /// <summary>
        /// assume the mesh is closed, this is 
        /// "The trick is to calculate the signed volume of a tetrahedron - based on your triangle and topped off at the origin. The sign of the volume comes from whether your triangle is pointing in the direction of the origin."
        /// 
        /// Reference: http://stackoverflow.com/questions/1406029/how-to-calculate-the-volume-of-a-3d-mesh-object-the-surface-of-which-is-made-up
        /// </summary>
        public static float CalcMeshVolume(Mesh mesh)
        {
            Vector3[] posLst = mesh.vertices;
            int[] tris = mesh.triangles;
            int triCnt = tris.Length;

            float vol = 0;

            for(int i=0; i<triCnt; i+=3)
            {
                Vector3 v0 = posLst[tris[i]];
                Vector3 v1 = posLst[tris[i + 1]]; 
                Vector3 v2 = posLst[tris[i + 2]];

                float v = Vector3.Dot(v0, Vector3.Cross(v1, v2)) / 6f;
                vol += v;
            }

            return vol;
        }

        /// <summary>
        /// two-pass check: ep = pt + right * bound.x_width * 2
        /// 1. use segment ray <ep, pt> to recur hit, count hit number HA; if no hit, false
        /// 2. use segment ray <pt, ep> to recur hit, count hit number HB;
        /// 
        /// if HA+HB == odd, true;
        /// else false
        /// </summary>
        //public static bool IsInsideMeshCollider(MeshCollider col, Vector3 pt, List<Vector3> ipts, List<Vector3> opts)
        public static bool IsInsideMeshCollider(Collider col, Vector3 pt)
        {
            Dbg.CAssert(col, col.gameObject.activeInHierarchy, "GeoUtil.IsInsideMeshCollider: collider GO not active: {0}", col.name);
            Vector3 refDir = Vector3.forward;
            Bounds bd = col.bounds;

            if (!bd.Contains(pt))
                return false;

            float len = bd.size.x * 2f;
            Vector3 ept = pt + refDir * len;

            int hitA = 0;
            int hitB = 0;

            // 1
            {
                Vector3 dir = -refDir;
                Ray r = new Ray(ept, dir);
                RaycastHit hit;
                float leftLen = len;

                while (true)
                {
                    if (!col.Raycast(r, out hit, leftLen))
                        break;

                    //ipts.Add(hit.point);

                    hitA++;
                    r.origin = hit.point + dir * 0.05f; //move origin a bit
                    leftLen = (r.origin - pt).magnitude; //shrink len limit
                }

                if (hitA == 0)
                    return false; //early quit
            }

            // 2
            {
                Vector3 dir = refDir;
                Ray r = new Ray(pt, dir);
                RaycastHit hit;
                float leftLen = len;

                while (true)
                {
                    if (!col.Raycast(r, out hit, leftLen))
                        break;

                    //opts.Add(hit.point);

                    hitB++;
                    r.origin = hit.point + dir * 0.05f; //move origin a bit
                    leftLen = (r.origin - ept).magnitude; //shrink len limit
                }
            }

            //DbgWatcher.AddEntry("Hit", "Inward {0}, outward {1}", hitA, hitB);
            return (hitA + hitB) % 2 == 1;
        }

        /// <summary>
        ///  | ax  ay |  
        ///  | bx  by |  equal 0 <==> parallel
        /// </summary>
        public static bool IsParallel(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x == 0;
        }

        /// <summary>
        ///  | ax  ay |   | ax  az |    | ay  az |
        ///  | bx  by | = | bx  bz |  = | by  bz | = 0  <==> parallel
        /// </summary>
        public static bool IsParallel(Vector3 a, Vector3 b)
        {
            return (a.x * b.y - a.y * b.x == 0) &&
                   (a.x * b.z - a.z * b.x == 0);
            //should add a.y*b.z - a.z*b.y == 0?
        }

    }
    
    public class CryptoUtil
    {
        private static StringBuilder ms_bld = new StringBuilder();

        public static byte[] MD5SumBytes(string strToEncrypt)
        {
            System.Text.UTF8Encoding ue = new System.Text.UTF8Encoding();
            byte[] bytes = ue.GetBytes(strToEncrypt);

            // encrypt bytes
            System.Security.Cryptography.MD5CryptoServiceProvider md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] hashBytes = md5.ComputeHash(bytes);

            return hashBytes;
        }
        public static string MD5Sum(string strToEncrypt)
        {
            System.Text.UTF8Encoding ue = new System.Text.UTF8Encoding();
            byte[] bytes = ue.GetBytes(strToEncrypt);

            // encrypt bytes
            System.Security.Cryptography.MD5CryptoServiceProvider md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] hashBytes = md5.ComputeHash(bytes);

            // Convert the encrypted bytes back to a string (base 16)

            StringBuilder bld = ms_bld;

            for (int i = 0; i < hashBytes.Length; i++)
            {
                bld.Append(hashBytes[i].ToString("X2"));
            }

            string ret = bld.ToString().PadLeft(32, '0');
            bld.Remove(0, bld.Length);

            return ret;
        }
    }

    public class MUndo
    {
        public static void RecordObject(Object o, string name)
        {
#if UNITY_EDITOR
            if ( !EditorApplication.isPlaying)
                Undo.RecordObject(o, name);
#endif
        }

        public static void RecordObjects(Object[] os, string name)
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
                Undo.RecordObjects(os, name);
#endif
        }

        public static void DestroyObj(Object o)
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying )
            {
                if( o != null )
                    Undo.DestroyObjectImmediate(o);
            }
            else
            {
                Object.Destroy(o);
            }
#else
            Object.Destroy(o);
#endif
        }
    }


    public class Levels
    {
        public static void LoadLevel(string name)
        {
#if UNITY_5_3_OR_NEWER || UNITY_5_3
            UnityEngine.SceneManagement.SceneManager.LoadScene(name);
#else
            Application.LoadLevel(name);
#endif
        }

        public static void LoadLevel(int idx)
        {
#if UNITY_5_3_OR_NEWER || UNITY_5_3
            UnityEngine.SceneManagement.SceneManager.LoadScene(idx);
#else
            Application.LoadLevel(idx);
#endif
        }

        public static AsyncOperation LoadLevelAsync(string name)
        {
#if UNITY_5_3_OR_NEWER || UNITY_5_3
            return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(name);
#else
            return Application.LoadLevelAsync(name);
#endif
        }

        public static AsyncOperation LoadLevelAsync(int idx)
        {
#if UNITY_5_3_OR_NEWER || UNITY_5_3
            return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(idx);
#else
            return Application.LoadLevelAsync(idx);
#endif
        }

        public static AsyncOperation LoadLevelAdditiveAsync(string name)
        {
#if UNITY_5_3_OR_NEWER || UNITY_5_3
            return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(name, UnityEngine.SceneManagement.LoadSceneMode.Additive);
#else
            return Application.LoadLevelAsync(name);
#endif
        }

        public static AsyncOperation LoadLevelAdditiveAsync(int idx)
        {
#if UNITY_5_3_OR_NEWER || UNITY_5_3
            return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(idx, UnityEngine.SceneManagement.LoadSceneMode.Additive);
#else
            return Application.LoadLevelAsync(idx);
#endif
        }



        public static string activeLevelName
        {
            get {
#if UNITY_5_3_OR_NEWER || UNITY_5_3
                return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
#else
                return Application.loadedLevelName;
#endif                   
            }
        }

        public static int activeLevel
        {
            get
            {
#if UNITY_5_3_OR_NEWER || UNITY_5_3
                return UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
#else
                return Application.loadedLevel;
#endif                   
            }
        }

        public static int levelCount
        {
            get
            {
#if UNITY_5_3_OR_NEWER || UNITY_5_3
                return UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
#else
                return Application.levelCount;
#endif          
            }
        }
    }

    /// <summary>
    /// an equality comparer wrapper
    /// </summary>
    public class FuncEqualityComparer<T> : IEqualityComparer<T>
    {
        private Func<T, T, bool> _comparer;
        private Func<T, int> _hash;

        public FuncEqualityComparer(Func<T, T, bool> comparer)
            : this(comparer, null) // NB Cannot assume anything about how e.g., t.GetHashCode() interacts with the comparer's behavior
        {
        }

        public FuncEqualityComparer(Func<T, T, bool> comparer, Func<T, int> hash)
        {
            _comparer = comparer;
            _hash = hash;
        }

        public void SetComp(Func<T, T, bool> cp) { _comparer = cp; }
        public void SetHash(Func<T, int> h) { _hash = h; }

        public bool Equals(T x, T y)
        {
            return _comparer(x, y);
        }

        public int GetHashCode(T obj)
        {
            if (_hash != null)
                return _hash(obj);
            else
                return obj.GetHashCode();
        }
    }

}

namespace ExtMethods
{
    using ESpace = MH.ESpace;

    public static class DictExt
    {
        /// <summary>
        /// try to get the value of given key, return null if not found
        /// </summary>
        public static V TryGet<K,V>(this Dictionary<K,V> dict, K key) where V : class
        {
            V ret;
            if( dict.TryGetValue(key, out ret))
            {
                return ret;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// if the key exist, return the existing value;
        /// if not, create a new value and set inplace;
        /// </summary>
        public static V ForceGet<K,V>(this Dictionary<K,V> dict, K key, Func<V> createFunc) where V : class
        {
            V ret = null;
            if( dict.TryGetValue(key, out ret) )
            {
                return ret;
            }
            else
            {
                V newV = createFunc();
                dict.Add(key, newV);
                return newV;
            }
        }
        public static V ForceGet<K,V>(this Dictionary<K,V> dict, K key) where V : class, new()
        {
            V ret = null;
            if (dict.TryGetValue(key, out ret))
            {
                return ret;
            }
            else
            {
                V newV = new V();
                dict.Add(key, newV);
                return newV;
            }
        }
    }

    public static class TransformExt
    {
        public static void ResetLocalTransform(this Transform tr)
        {
            tr.localPosition = Vector3.zero;
            tr.localEulerAngles = Vector3.zero;
            tr.localScale = Vector3.one;
        }

        public static Vector3 GetPosition(this Transform tr, ESpace space)
        {
            switch (space)
            {
                case ESpace.World:
                    return tr.position;
                case ESpace.Self:
                    return tr.localPosition;
                default:
                    Dbg.LogErr("TransformExt.GetPosition: unexpected space enum: {0}", space);
                    return Vector3.zero;
            }
        }

        public static void SetPosition(this Transform tr, Vector3 v, ESpace space)
        {
            switch (space)
            {
                case ESpace.World:
                    tr.position = v; break;
                case ESpace.Self:
                    tr.localPosition = v; break;
                default:
                    Dbg.LogErr("TransformExt.SetPosition: unexpected space enum: {0}", space);
                    break;
            }
        }

        public static Vector3 GetEuler(this Transform tr, ESpace space)
        {
            switch (space)
            {
                case ESpace.World:
                    return tr.eulerAngles;
                case ESpace.Self:
                    return tr.localEulerAngles;
                default:
                    Dbg.LogErr("TransformExt.GetEuler: unexpected space enum: {0}", space);
                    return Vector3.zero;
            }
        }

        public static void SetEuler(this Transform tr, Vector3 v, ESpace space)
        {
            switch (space)
            {
                case ESpace.World:
                    tr.eulerAngles = v; break;
                case ESpace.Self:
                    tr.localEulerAngles = v; break;
                default:
                    Dbg.LogErr("TransformExt.SetEuler: unexpected space enum: {0}", space);
                    break;
            }
        }

        public static Quaternion GetQuaternion(this Transform tr, ESpace space)
        {
            switch (space)
            {
                case ESpace.World:
                    return tr.rotation;
                case ESpace.Self:
                    return tr.localRotation;
                default:
                    Dbg.LogErr("TransformExt.GetQuaternion: unexpected space enum: {0}", space);
                    return Quaternion.identity;
            }
        }

        public static void SetQuaternion(this Transform tr, Quaternion v, ESpace space)
        {
            switch (space)
            {
                case ESpace.World:
                    tr.rotation = v; break;
                case ESpace.Self:
                    tr.localRotation = v; break;
                default:
                    Dbg.LogErr("TransformExt.SetQuaternion: unexpected space enum: {0}", space);
                    break;
            }
        }

        public static Vector3 GetScale(this Transform tr, ESpace space)
        {
            switch (space)
            {
                case ESpace.World:
                    return tr.lossyScale;
                case ESpace.Self:
                    return tr.localScale;
                default:
                    Dbg.LogErr("Transform.GetScale: unexpected space enum: {0}", space);
                    return Vector3.one;
            }
        }

        /// <summary>
        /// WARNING: the world method works, only if the `tr''s ancestors are all uniform scaled
        /// </summary>
        public static void SetScale(this Transform tr, Vector3 v, ESpace space)
        {
            switch (space)
            {
                case ESpace.World:
                    if (tr.parent)
                    {
                        Vector3 safeParentLossyScale = MH.V3Ext.FixZeroComponent(tr.parent.lossyScale, 0.001f);
                        tr.localScale = MH.V3Ext.DivideComp(v, safeParentLossyScale);
                    }
                    else
                    {
                        tr.localScale = v;
                    }
                    break; //do nothing
                case ESpace.Self:
                    tr.localScale = v; break;
                default:
                    Dbg.LogErr("TransformExt.SetScale: unexpected space enum: {0}", space);
                    break;
            }
        }

        public static void CopyLocal(this Transform tr, Transform from)
        {
            tr.localPosition = from.localPosition;
            tr.localRotation = from.localRotation;
            tr.localScale = from.localScale;
        }

        public static void CopyLocalPR(this Transform tr, Transform from)
        {
            tr.localPosition = from.localPosition;
            tr.localRotation = from.localRotation;
        }

        /// <summary>
        /// scale will still use local
        /// </summary>
        public static void CopyWorld(this Transform tr, Transform from)
        {
            tr.position = from.position;
            tr.rotation = from.rotation;
            tr.localScale = from.localScale;
        }

        public static void CopyWorldPR(this Transform tr, Transform from)
        {
            tr.position = from.position;
            tr.rotation = from.rotation;
        }

        /// <summary>
        /// 6 lookat functions
        /// </summary>
        public static void LookAtZY(this Transform tr, Vector3 targetPos, Vector3 newY)
        {
            tr.LookAt(targetPos, newY);
        }
        public static void LookAtZX(this Transform tr, Vector3 targetPos, Vector3 newX)
        {
            Vector3 newZ = targetPos - tr.position;
            Quaternion q = MH.QUtil.LookAtZX(newZ, newX);
            tr.rotation = q;
        }
        public static void LookAtXY(this Transform tr, Vector3 targetPos, Vector3 newY)
        {
            Vector3 newX = targetPos - tr.position;
            Quaternion q = MH.QUtil.LookAtXY(newX, newY);
            tr.rotation = q;
        }
        
        public static void LookAtXZ(this Transform tr, Vector3 targetPos, Vector3 newZ)
        {
            Vector3 newX = targetPos - tr.position;
            Quaternion q = MH.QUtil.LookAtXZ(newX, newZ);
            tr.rotation = q;
        }
        public static void LookAtYX(this Transform tr, Vector3 targetPos, Vector3 newX)
        {
            Vector3 newY = targetPos - tr.position;
            Quaternion q = MH.QUtil.LookAtYX(newY, newX);
            tr.rotation = q; 
        }
        public static void LookAtYZ(this Transform tr, Vector3 targetPos, Vector3 newZ)
        {
            Vector3 newY = targetPos - tr.position;
            Quaternion q = MH.QUtil.LookAtYZ(newY, newZ);
            tr.rotation = q; 
        }

        /// <summary>
        /// Rotate this transform, by the from->to rotation, under specified Space
        /// </summary>
        public static void RotateFromTo(this Transform tr, Vector3 from, Vector3 to, Space space = Space.Self)
        {
            Quaternion q = Quaternion.FromToRotation(from, to);
            if (space == Space.World)
            {
                tr.rotation = q * tr.rotation;
            }
            else
            {
                tr.localRotation = q * tr.localRotation;
            }
        }

        /// <summary>
        /// check if tr has 'pCnt' or more level of parent 
        /// </summary>
        public static bool HasParentLevel(this Transform tr, int pCnt)
        {
            bool bOK = true;
            Dbg.Assert(tr != null, "Transform.HasParentLevel: transform == null");

            for (int i = 0; i < pCnt; ++i)
            {
                tr = tr.parent;
                if (tr == null)
                {
                    bOK = false;
                    break;
                }
            }

            return bOK;
        }

        /// <summary>
        /// check if tr has 'expectLevel' or more level of parent,
        /// if not, 'retLevel' will be filled for actual parent level
        /// </summary>
        public static bool HasParentLevel(this Transform tr, int expectLevel, out int retLevel)
        {
            bool bOK = true;
            Dbg.Assert(tr != null, "Transform.HasParentLevel: transform == null");

            int i = 0;
            for (; i < expectLevel; ++i)
            {
                tr = tr.parent;
                if (tr == null)
                {
                    bOK = false;
                    break;
                }
            }

            retLevel = i;
            return bOK;
        }

        /// <summary>
        /// like Transform.Find, but will not take `/' as hierachy separator.
        /// will just recursive find all children for exact matching name,
        /// if not found, return null
        /// </summary>
        public static Transform FindByName(this Transform tr, string name)
        {
            if (tr.name == name)
                return tr;

            for( int idx = 0; idx < tr.childCount; ++idx )
            {
                Transform ctr = tr.GetChild(idx);
                Transform retTr = FindByName(ctr, name);
                if( retTr != null )
                {
                    return retTr;
                }
            }

            return null;
        }

        /// <summary>
        /// find the first transform that has given tag, and is child or self of given `parent'
        /// </summary>
        public static Transform
        FindWithTag(this Transform parent, string tag)
        {
            GameObject[] withTags = GameObject.FindGameObjectsWithTag(tag);
            for (int i = 0; i < withTags.Length; ++i)
            {
                Transform tr = withTags[i].transform;
                if (tr.IsChildOf(parent))
                {
                    return tr;
                }
            }
            return null;
        }

        /// <summary>
        /// find the first transform of self and self's ancestors, which has the given tag
        /// </summary>
        public static Transform
        FindParentWithTag(this Transform self, string tag)
        {
            Transform tr = self;
            while (tr != null )
            {
                if( tr.tag == tag )
                {
                    return tr;
                }
                else
                {
                    tr = tr.parent;
                }
            }
            return null;
        }

        public static T
        GetChildComponent<T>(this Transform self, string childName) where T : Component
        {
            Transform childTr = self.Find(childName);
            Dbg.CAssert(self, childTr!=null, "TransformExt.GetChildComponent: failed to get child: {0}", childName);

            T cp = childTr.GetComponent<T>();

            return cp;
        }

        public static Bounds TransformBounds(this Transform self, Bounds bounds)
        {
            var center = self.TransformPoint(bounds.center);
            var points = bounds.GetCorners();

            var result = new Bounds(center, Vector3.zero);
            for (int i=0; i<8; ++i)
            {
                Vector3 point = points[i];
                result.Encapsulate(self.TransformPoint(point));
            }
            return result;
        }

        public static Bounds InverseTransformBounds(this Transform self, Bounds bounds)
        {
            var center = self.InverseTransformPoint(bounds.center);
            var points = bounds.GetCorners();

            var result = new Bounds(center, Vector3.zero);
            for (int i=0; i < 8; ++i)
            {
                Vector3 point = points[i];
                result.Encapsulate(self.InverseTransformPoint(point));
            }
            return result;
        }

        public static TrRecurEnumerator
        GetRecurEnumerator(this Transform tr)
        {
            return new TrRecurEnumerator(tr);
        }

        public struct TrRecurEnumerator
        {
            private MH.SList<int> m_stack;
            private Transform m_curTr;
            private Transform m_retTr;

            public TrRecurEnumerator(Transform tr)
            {
                m_curTr = tr;
                m_retTr = null;

                m_stack = new MH.SList<int>();
                m_stack.Add(0);
            }

            public bool MoveNext()
            {
                if (m_curTr == null)
                    return false;

                m_retTr = m_curTr;

                // set curTr's next position
                while (true)
                {
                    int idx = m_stack[m_stack.Count - 1]; m_stack.RemoveAt(m_stack.Count - 1); //pop
                    if (idx >= m_curTr.childCount)
                    { // end for this transform, if stack is empty then over, or back upward
                        if (m_stack.Count == 0)
                        {
                            m_curTr = null;
                            break;
                        }
                        else
                        {
                            m_curTr = m_curTr.parent;
                        }
                    }
                    else
                    { //dive
                        m_curTr = m_curTr.GetChild(idx);
                        m_stack.Add(idx + 1);
                        m_stack.Add(0);
                        break;
                    }
                }

                return m_retTr != null;
            }

            public Transform Current
            {
                get { return m_retTr; }
            }
        }

    }

    public static class BoundsExt
    {
        public static Vector3[] GetCorners(this Bounds bd, bool addCenter = true)
        {
            Vector3[] pts = new Vector3[8];
            int idx = 0;
            Vector3 o = addCenter ? bd.center : Vector3.zero;
            for(int x = -1; x <= 1; x+=2)
                for (int y = -1; y <=1; y += 2)
                    for(int z = -1; z <=1; z+= 2)
                    {
                        Vector3 off = bd.extents;
                        off.Scale(new Vector3(x, y, z));
                        pts[idx++] = o + off;
                    }

            return pts;
        }

        public static void GetCorners(this Bounds bd, Vector3[] pts, bool addCenter = true)
        {
            Dbg.Assert(pts.Length >= 8, "BoundsExt.GetCorners: lst length must >= 8: got {0}", pts.Length);
            int idx = 0;
            Vector3 o = addCenter ? bd.center : Vector3.zero;
            for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 off = bd.extents;
                        off.Scale(new Vector3(x, y, z));
                        pts[idx++] = o + off;
                    }
        }
    }

    public static class GOExt
    {
        public enum FIELD{NONE = 0, X=1, Y=2, XY=3, Z=4, XZ=5, YZ=6, XYZ=7};
        private static int[] FIELDCNT = {0, 1, 1, 2, 1, 2, 2, 3 };

        public static T SafeGetComponent<T>(this GameObject go) where T : Component
        {
            if (go == null)
                return null;

            return go.GetComponent<T>();
        }

        public static T ForceGetComponent<T>(this GameObject go) where T : Component
        {
            T comp = go.GetComponent<T>();
            if( comp == null )
            {
                comp = go.AddComponent<T>();
            }
            return comp;
        }

        public static Component ForceGetComponent(this GameObject go, Type tp)
        {
            var comp = go.GetComponent(tp);
            if( comp == null )
            {
                comp = go.AddComponent(tp);
            }
            return comp;
        }

        public static T AssertGetComponent<T>(this GameObject go) where T : Component
        {
            T comp = go.GetComponent<T>();
            Dbg.CAssert(go, comp != null, "GOExt.AssertGetComponent: failed to get component");
            return comp;
        }

        public static void SilentDestroyComponent<T> (this GameObject go) where T : Component
        {
            T comp = go.GetComponent<T>();
            if( comp != null )
            {
                GameObject.Destroy(comp);
            }
        }

        public static Vector3 GetColliderExtents(this GameObject go)
        {
            return GetColliderExtents(go, Vector3.zero);
        }
        public static Vector3 GetColliderExtents(this GameObject go, Vector3 defExt)
        {
            Collider c = go.GetComponent<Collider>();
            if( c == null )
            {
                return defExt;
            }
            else
            {
                return c.bounds.extents;
            }
        }

        public static void SetLocalPosition(this GameObject go, FIELD f, params float[] p)
        {
            Vector3 pos = go.transform.localPosition;
            Vector3 oldPos = pos;
            int fieldCnt = FIELDCNT[(uint)f];
            if (fieldCnt == 0)
                return;
            Dbg.Assert(p.Length == fieldCnt, "GOExt.SetLocalPosition: param cnt not fit: {0} != {1}", p.Length, fieldCnt);

            int idx = 0;
            int bits = (int)f;
            if( (bits & (int)FIELD.X) != 0 )
            {
                pos.x = p[idx++];
            }
            if( (bits & (int)FIELD.Y) != 0)
            {
                pos.y = p[idx++];
            }
            if( (bits & (int)FIELD.Z) != 0)
            {
                pos.z = p[idx++];
            }

            if( oldPos == pos )
            {
                go.transform.localPosition = pos;
            }
        }

        public static void SetLocalScale(this GameObject go, FIELD f, params float[] p)
        {
            Transform trans = go.transform;
            Vector3 oldscale = trans.localScale;
            Vector3 scale = oldscale;

            int fieldCnt = FIELDCNT[(uint)f];
            if (fieldCnt == 0)
                return;
            Dbg.Assert(p.Length == fieldCnt, "GOExt.SetLocalScale: param cnt not fit: {0} != {1}", p.Length, fieldCnt);

            int idx = 0;
            int bits = (int)f;
            if ((bits & (int)FIELD.X) != 0)
            {
                scale.x = p[idx++];
            }
            if ((bits & (int)FIELD.Y) != 0)
            {
                scale.y = p[idx++];
            }
            if ((bits & (int)FIELD.Z) != 0)
            {
                scale.z = p[idx++];
            }

            if( scale != oldscale )
            {
                go.transform.localScale = scale;
            }
        }
    }

    public static class CompExt
    {
        public static T SafeGetComponent<T>(this Component cp) where T : Component
        {
            if (cp == null)
                return null;

            return cp.GetComponent<T>();
        }

        public static T ForceGetComponent<T>(this Component cp) where T : Component
        {
            T comp = cp.GetComponent<T>();
            if (comp == null)
            {
                comp = cp.gameObject.AddComponent<T>();
            }
            return comp;
        }

        public static T AssertGetComponent<T>(this Component cp) where T : Component
        {
            T comp = cp.GetComponent<T>();
            Dbg.CAssert(cp, comp != null, "CompExt.AssertGetComponent: failed to get component");
            return comp;
        }

        public static T AssertGetComponentInChildren<T>(this Component cp) where T : Component
        {
            T comp = cp.GetComponentInChildren<T>();
            Dbg.CAssert(cp, comp != null, "CompExt.AssertGetComponentInChildren: failed to get component");
            return comp;
        }

        public static T AssertGetComponentInParent<T>(this Component cp) where T : Component
        {
            T comp = cp.GetComponentInParent<T>();
            Dbg.CAssert(cp, comp != null, "CompExt.AssertGetComponentInParent: failed to get component");
            return comp;
        }
    }

    public static class MonoBehaviourExt
    {
        public static Coroutine DelayExecute(this MonoBehaviour mb, System.Action op, float time)
        {
            return mb.StartCoroutine(Job_DelayExec(op, time));
        }
        public static Coroutine DelayExecute<T>(this MonoBehaviour mb, System.Action<T> op, T arg, float time)
        {
            return mb.StartCoroutine(Job_DelayExec(op, arg, time));
        }
        public static Coroutine DelayExecute<T1, T2>(this MonoBehaviour mb, System.Action<T1, T2> op, T1 arg1, T2 arg2, float time)
        {
            return mb.StartCoroutine(Job_DelayExec(op, arg1, arg2, time));
        }

        public static Job Job_DelayExec(System.Action op, float time)
        {
            yield return new WaitForSeconds(time);
            op();
        }
        public static Job Job_DelayExec<T>(System.Action<T> op, T arg, float time)
        {
            yield return new WaitForSeconds(time);
            op(arg);
        }
        public static Job Job_DelayExec<T1, T2>(System.Action<T1, T2> op, T1 arg1, T2 arg2, float time)
        {
            yield return new WaitForSeconds(time);
            op(arg1, arg2);
        } 

        public static Coroutine DestroyAfterSeconds(MonoBehaviour mb, Object obj, float sec)
        {
            return mb.StartCoroutine(Job_DestroyAfterSeconds(obj, sec));
        }

        public static Job Job_DestroyAfterSeconds(Object go, float sec)
        {
            yield return new WaitForSeconds(sec);
            GameObject.Destroy(go);
        }

        public static Coroutine DestroyAtFrameEnd(MonoBehaviour mb, Object obj)
        {
            return mb.StartCoroutine(Job_DestroyAtFrameEnd(obj));
        }

        public static Job Job_DestroyAtFrameEnd(Object obj)
        {
            yield return new WaitForEndOfFrame();
            Object.Destroy(obj);
        }
    }

    public static class EnumerableExt
    {
        public static float WeightedAverage<T>(this IEnumerable<T> cont, Func<T, float> funcV, Func<T, float> funcW)
        {
            float totalWV = 0, totalW = 0;
            for (var ie = cont.GetEnumerator(); ie.MoveNext(); )
            {
                var elem = ie.Current;
                var v = funcV(elem);
                var w = funcW(elem);
                totalWV += w * v;
                totalW += w;
            }

            float r = totalWV / totalW;
            return r;
        }
    }

    public static class ListExt
    {
        /// <summary>
        /// just use default(T) to fill the new space
        /// </summary>
        public static void Resize<T>(this List<T> lst, int newsz)
        {
            int cnt = lst.Count;
            if( cnt > newsz )
            {
                lst.RemoveRange(newsz, cnt - newsz);
            }
            else if (cnt < newsz )
            {
                for( int idx = 0; idx < newsz - cnt; ++idx )
                {
                    lst.Add(default(T));
                }
            }
            //do nothing if equal
        }

        /// <summary>
        /// T is class and new-able()
        /// </summary>
        public static void ResizeC<T>(this List<T> lst, int newsz) where T : class, new()
        {
            int cnt = lst.Count;
            if (cnt > newsz)
            {
                lst.RemoveRange(newsz, cnt - newsz);
            }
            else if (cnt < newsz)
            {
                for (int idx = 0; idx < newsz - cnt; ++idx)
                {
                    lst.Add(new T());
                }
            }
            //do nothing if equal
        }

        public static void EnsureSize<T>(this List<T> lst, int minSz)
        {
            if (lst.Count < minSz)
                lst.Resize(minSz);
        }

        public static void AddMany<T>(this List<T> lst, params T[] objs)
        {
            for(int idx = 0; idx < objs.Length; ++idx)
            {
                lst.Add(objs[idx]);
            }
        }

        public static void AddMulti<T>(this IList<T> lst, T v0, T v1)
        {
            lst.Add(v0);
            lst.Add(v1);
        }
        public static void AddMulti<T>(this IList<T> lst, T v0, T v1, T v2)
        {
            lst.Add(v0);
            lst.Add(v1);
            lst.Add(v2);
        }
        public static void AddMulti<T>(this IList<T> lst, T v0, T v1, T v2, T v3)
        {
            lst.Add(v0);
            lst.Add(v1);
            lst.Add(v2);
            lst.Add(v3);
        }
        public static void AddMulti<T>(this IList<T> lst, T v0, T v1, T v2, T v3, T v4)
        {
            lst.Add(v0);
            lst.Add(v1);
            lst.Add(v2);
            lst.Add(v3);
            lst.Add(v4);
        }
        public static void AddMulti<T>(this IList<T> lst, T v0, T v1, T v2, T v3, T v4, T v5)
        {
            lst.Add(v0);
            lst.Add(v1);
            lst.Add(v2);
            lst.Add(v3);
            lst.Add(v4);
            lst.Add(v5);
        }

        public static void AddRange<T>(this IList<T> lst, IList<T> from, int idx, int count)
        {
            for(int i= idx; i< idx+count; ++i)
            {
                lst.Add(from[i]);
            }
        }

        /// <summary>
        /// enable to get element by negative index
        /// </summary>
        public static T ExtGet<T>(this IList<T> lst, int idx)
        {
            int cnt = lst.Count;
            if (cnt == 0)
                return default(T);

            if( idx < 0 ) { idx = (idx % cnt) + cnt; }
            if (idx >= cnt) { idx = idx % cnt; }
            //Dbg.Assert(0 <= idx && idx < cnt, "ListExt.ExtGet: invalid idx: {0}, count: {1}", idx, cnt);
            return lst[idx];
        }

        /// <summary>
        /// add a int list to be from 'from' to 'to', both inclusive
        /// </summary>
        public static void AddRangeValue(this IList<int> lst, int from, int to)
        {
            lst.Clear();
            int inc = Math.Sign(to - from);
            for( int i = from; i <= to; i+=inc )
            {
                lst.Add(i);
            }
        }

        public static void SetRangeValue(this IList<int> lst, int from, int to)
        {
            int cnt = Mathf.Abs(to-from) + 1;
            Dbg.Assert(lst.Count == cnt, "SetRangeValue: length not match");
            int inc = Math.Sign(to - from);
            int v = from;
            for( int i=0; i<cnt; i++)
            {
                lst[i] = v;
                v += inc;
            }
        }

        /// <summary>
        /// shuffle given list
        /// </summary>
        public static void ShuffleList<T>(this IList<T> lst)
        {
            int sz = lst.Count;
            for (int i = 0; i < sz; ++i)
            {
                int tgtIdx = Random.Range(i, sz);
                if (tgtIdx != i)
                {
                    T tmp = lst[i];
                    lst[i] = lst[tgtIdx];
                    lst[tgtIdx] = tmp;
                }
            }
        }

        /// <summary>
        /// will make specified list to be filled with [0, size-1], 
        /// and shuffle it
        /// </summary>
        public static void GenShuffledIntList(this List<int> toFillLst, int size)
        {
            toFillLst.AddRangeValue(0, size-1);
            ShuffleList(toFillLst);
        }

        public static bool Contains<T>(this IList<T> lst, T o)
        {
            int sz = lst.Count;
            for(int i=0; i<sz; ++i)
            {
                if (lst[i].Equals(o))
                    return true;
            }
            return false;
        }

        public static void SetValue<T>(this IList<T> lst, int fromIdx, int sz, T v)
        {
            for (int i = fromIdx; i < fromIdx + sz; ++i)
            {
                lst[i] = v;
            }
        }
        public static void SetAllValue<T>(this IList<T> lst, T v)
        {
            int sz = lst.Count;
            for(int i=0; i<sz; ++i)
            {
                lst[i] = v;
            }
        }

        /// <summary>
        /// randomly get 'cnt' elements from lst, added into outLst
        /// </summary>
        public static void RandomGetElem<T>(this IList<T> lst, IList<T> outLst, int cnt)
        {
            Dbg.Assert(lst.Count >= cnt, "RandomGetElem: not enough element in list: expect {0}, has {1}", cnt, lst.Count);
            int total = lst.Count;
            int left = cnt;

            for(int i=0; i<total; ++i)
            {
                float v = Random.value;
                if( v < left / (float)(total-i) )
                { 
                    outLst.Add(lst[i]);
                    left--;
                    if (left <= 0)
                        break;
                }
            }
        }

        public static void RandomGetElem<T>(this IList<T> lst, IList<T> outLst, int cnt, Predicate<T> pred)
        {
            var tmpLst = MH.Mem.New<List<T>>();

            for(int i=0; i<lst.Count; ++i)
            {
                if( pred(lst[i]) )
                {
                    tmpLst.Add(lst[i]);
                }
            }

            int newCnt = Mathf.Min(cnt, tmpLst.Count);
            RandomGetElem(tmpLst, outLst, newCnt);

            tmpLst.Clear();
            MH.Mem.Del(tmpLst);
        }

        public static T RandomGetElem<T>(this IList<T> lst)
        {
            int cnt = lst.Count;
            if (cnt == 0)
                return default(T);

            int idx = Random.Range(0, cnt);
            return lst[idx];
        }

        public static T RandomGetElemByWeight<T>(this IList<T> lst, Func<T, float> weightFunc)
        {
            float totalWeight = 0;
            for (int i = 0; i < lst.Count; ++i) { totalWeight += weightFunc(lst[i]); }

            float acc = 0;
            float w = Random.Range(0, totalWeight);
            for (int i = 0; i < lst.Count; ++i)
            {
                var minfo = lst[i];
                acc += weightFunc(minfo);
                if (w <= acc)
                    return minfo;
            }

            Dbg.LogErr("ListExt.RandomGetElemByWeight: ain't be here: rand: {0}, total: {1}, lstCnt: {2}", w, totalWeight, lst.Count);
            return default(T);
        }

        public static T Last<T>(this IList<T> lst)
        {
            if (lst.Count == 0)
                return default(T);
            else
                return lst[lst.Count - 1];
        }

        public static T First<T>(this IList<T> lst)
        {
            if (lst.Count == 0)
                return default(T);
            else
                return lst[0];
        }

        public static bool IsEmpty<T>(this IList<T> lst)
        {
            return lst.Count == 0;
        }

        /// <summary>
        /// return true iff the param is unique;
        /// </summary>
        public static bool UniqAdd<T>(this IList<T> lst, T v)
        {
            if (lst.Contains(v))
                return false;

            lst.Add(v);
            return true;
        }

        /// <summary>
        /// return true iff all elements are unique
        /// </summary>
        public static bool IsAllUnique<T>(this IList<T> lst, Comparison<T> comp)
        {
            var tmpLst = MH.Mem.New<List<T>>();
            tmpLst.Clear();
            tmpLst.AddRange(lst);
            tmpLst.Sort(comp);
            bool bUniq = true;
            for(int i=0; i<tmpLst.Count-1; ++i)
            {
                if( comp(tmpLst[i], tmpLst[i+1]) == 0 )
                {
                    bUniq = false;
                    break;
                }
            }
            tmpLst.Clear();
            MH.Mem.Del(tmpLst);
            return bUniq;
        }

        public static bool IsAllUnique<T>(this IList<T> lst) where T : IComparable<T>
        {
            var tmpLst = MH.Mem.New<List<T>>();
            tmpLst.Clear();
            tmpLst.AddRange(lst);
            tmpLst.Sort();
            bool bUniq = true;
            for (int i = 0; i < tmpLst.Count - 1; ++i)
            {
                if (tmpLst[i].CompareTo(tmpLst[i + 1]) == 0)
                {
                    bUniq = false;
                    break;
                }
            }
            tmpLst.Clear();
            MH.Mem.Del(tmpLst);
            return bUniq;
        }

        /// <summary>
        /// release all elements in list, and clear the list
        /// </summary>
        public static void ReleaseAndClear<T>(this IList<T> lst) where T : MH.IRelease
        {
            for(int i=0; i<lst.Count; ++i)
            {
                lst[i].Release();
            }
            lst.Clear();
        }

        public static bool IsSorted<T>(this IList<T> lst) where T : IComparable<T>
        {
            if (lst.Count < 2)
                return true;

            bool isSorted = true;
            int flag = 0; //sort-order
            int i = 0;

            //confirm less / greater sort first
            for(; i<lst.Count-1; ++i)
            {
                flag = lst[i].CompareTo(lst[i + 1]);
                if( flag != 0 )
                {
                    ++i;
                    break;
                }
            }
            // check if all elements conform to the sort order
            for(; i<lst.Count-1; ++i)
            {
                int v = lst[i].CompareTo(lst[i + 1]);
                if( (v ^ flag) < 0 ) //check if different sign
                {
                    isSorted = false;
                    break;
                }
            }

            return isSorted;
        }
    }

    public static class BitArrayExt
    {
        //private static byte[] _cache = new byte[8]; //64bit

        //private static void _ClearCache()
        //{
        //    Array.Clear(_cache, 0, 8);
        //}

        /// <summary>
        /// test if all value in `arr' are all `bVal'
        /// </summary>
        public static bool IsAll(this BitArray arr, bool bVal)
        {
            int cnt = arr.Length;
            for( int idx = 0; idx < cnt; ++idx )
            {
                if (bVal != arr.Get(idx))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// get the integer made of bits from 'idxFrom' to 'idxTo', inclusive;
        /// the highest bit is sign bit
        /// </summary>
        public static int GetInt32(this BitArray arr, int idxFrom, int cnt)
        {
            Dbg.Assert(cnt <= 32, "BitArrayExt.GetInt32: maximum 32 bits: {0}, {1}", idxFrom, cnt);

            if (cnt <= 0)
                return 0;

            int v = 0;
            int idxTo = idxFrom + cnt - 1;
            if( arr[idxTo] )
            { //negative
                for (int i = idxTo - 1; i >= idxFrom; --i)
                {
                    v <<= 1;
                    if (!arr[i])
                        v |= 1;
                }
                v += 1;
                if (v == (1 << (idxFrom - idxTo)))
                    v = 0;
                v = -v;
            }
            else
            { //positive
                for (int i = idxTo-1; i >= idxFrom; --i)
                {
                    v <<= 1;
                    if (arr[i])
                        v |= 1;
                }
            }

            return v;
        }

        /// <summary>
        /// get the integer made of bits from 'idxFrom' to 'idxTo', inclusive;
        /// </summary>
        public static uint GetUInt32(this BitArray arr, int idxFrom, int cnt)
        {
            Dbg.Assert(cnt <= 32, "BitArrayExt.GetUInt32: maximum 32 bits: {0}, {1}", idxFrom, cnt);

            uint v = 0;
            int idxTo = idxFrom + cnt - 1;

            for (int i = idxTo; i >= idxFrom; --i)
            {
                v <<= 1;
                if (arr[i])
                    v |= 1;
            }

            return v;
        }

    }

    public static class EnumExtension
    {
        public static bool HasFlag(this Enum e, Enum flag)
        {
            int ve = Convert.ToInt32(e);
            int fe = Convert.ToInt32(flag);
            return (ve & fe) == fe;
        }

        public static bool HasAnyFlag(this Enum e, Enum flag)
        {
            int ve = Convert.ToInt32(e);
            int fe = Convert.ToInt32(flag);
            return (ve & fe) != 0;
        }
    }
}