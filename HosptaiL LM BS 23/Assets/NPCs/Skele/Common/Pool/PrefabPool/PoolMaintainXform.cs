using System;
using System.Collections.Generic;
using UnityEngine;
using ExtMethods;

namespace MH
{
    /// <summary>
    /// 1. when OnDespawn is called, recorded requested xform data;
    /// 2. when OnSpawn is called, resume requested xform data;
    /// </summary>
    [ScriptOrder(-100)]
    public class PoolMaintainXform : SpawnableMonoBehaviour
    {
        public ESaveData _eSaveData = ESaveData.LocalPos;
        public XformData _data = new XformData();

        private Transform _tr;
        private bool _firstSpawn = true;

        protected override void _OnStart()
        {
            base._OnStart();
            _tr = transform;
        }

        protected override void _OnSpawn()
        {
            base._OnSpawn();

            if( !_firstSpawn ) //don't resume data on firstSpawn
            {
                if ((_eSaveData & ESaveData.LocalPos) != 0)
                {
                    _tr.localPosition = _data.pos;
                }
                if ((_eSaveData & ESaveData.LocalRot) != 0)
                {
                    _tr.localRotation = _data.rot;
                }
                if ((_eSaveData & ESaveData.LocalScale) != 0)
                {
                    _tr.localScale = _data.scale;
                }
            }
            else
            {
                _firstSpawn = false;
            }            
        }

        protected override void _OnDespawn()
        {
            base._OnDespawn();
            _data.CopyFrom(_tr);
        }

        public enum ESaveData
        {
            LocalPos = 1,
            LocalRot = 1 << 1,
            LocalScale = 1 << 2,
        }
    }
}
