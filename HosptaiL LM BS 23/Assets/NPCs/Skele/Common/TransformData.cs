using System;
using System.Collections.Generic;
using UnityEngine;

namespace MH
{
    /// <summary>
    /// used to record the transform info,
    /// default take local info,
    /// the -W postfix functions take the world info.
    /// 
    /// NOTE: scale are always taken by local
    /// </summary>
    [Serializable]
	public class XformData
    {
        public Vector3 pos;
        public Quaternion rot;
        public Vector3 scale;

        public static XformData Create(Transform srcTr)
        {
            XformData trData = new XformData();
            trData.CopyFrom(srcTr);
            return trData;
        }

        public static XformData CreateW(Transform srcTr)
        {
            XformData trData = new XformData();
            trData.CopyFromW(srcTr);
            return trData;
        }

        public void CopyFrom(Transform tr)
        {
            pos = tr.localPosition;
            rot = tr.localRotation;
            scale = tr.localScale;
        }

        public void CopyFromW(Transform tr)
        {
            pos = tr.position;
            rot = tr.rotation;
            scale = tr.localScale;
        }

        public void CopyFrom(XformData data)
        {
            pos = data.pos;
            rot = data.rot;
            scale = data.scale;
        }

        public void Apply(Transform tr)
        {
            tr.localPosition = pos;
            tr.localRotation = rot;
            tr.localScale = scale;
        }

        public void ApplyW(Transform tr)
        {
            tr.position = pos;
            tr.rotation = rot;
            tr.localScale = scale;
        }

        public void Clear()
        {
            pos = Vector3.zero;
            rot = Quaternion.identity;
            scale = Vector3.zero;
        }
    }

    /// <summary>
    /// include the xform 
    /// </summary>
    [Serializable]
    public class XformData2
    {
        public Transform tr;
        public Vector3 pos;
        public Quaternion rot;
        public Vector3 scale;

        public static XformData2 Create(Transform srcTr)
        {
            XformData2 trData = new XformData2();
            trData.tr = srcTr;
            trData.CopyFrom();
            return trData;
        }

        public static XformData2 CreateW(Transform srcTr)
        {
            XformData2 trData = new XformData2();
            trData.tr = srcTr;
            trData.CopyFromW();
            return trData;
        }

        public void CopyFrom()
        {
            pos = tr.localPosition;
            rot = tr.localRotation;
            scale = tr.localScale;
        }

        public void CopyFromW()
        {
            pos = tr.position;
            rot = tr.rotation;
            scale = tr.localScale;
        }

        public void CopyFrom(XformData2 data)
        {
            tr = data.tr;
            pos = data.pos;
            rot = data.rot;
            scale = data.scale;
        }

        public void Apply()
        {
            tr.localPosition = pos;
            tr.localRotation = rot;
            tr.localScale = scale;
        }

        public void ApplyW()
        {
            tr.position = pos;
            tr.rotation = rot;
            tr.localScale = scale;
        }

        public void Clear()
        {
            tr = null;
            pos = Vector3.zero;
            rot = Quaternion.identity;
            scale = Vector3.zero;
        }

        public bool HasDiff()
        {
            if (tr.localPosition != pos)
                return true;
            if (tr.localRotation != rot)
                return true;
            if (tr.localScale != scale)
                return true;

            return false;
        }

        public bool HasDiffW()
        {
            if (tr.position != pos)
                return true;
            if (tr.rotation != rot)
                return true;
            if (tr.localScale != scale)
                return true;

            return false;
        }
    }
}
