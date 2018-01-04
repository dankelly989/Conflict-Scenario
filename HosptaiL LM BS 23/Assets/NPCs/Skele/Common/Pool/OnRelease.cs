using System;
using System.Collections.Generic;
using UnityEngine;
using ExtMethods;

namespace MH
{
    public class OnRelease : SpawnableMonoBehaviour
    {
        private List<IRelease> _toRelease = new List<IRelease>();

        protected override void _OnDespawn()
        {
            base._OnDespawn();

            _toRelease.ForEach(x => x.Release());
        }

        public static void ForceAdd(GameObject go, IRelease o)
        {
            var comp = go.ForceGetComponent<OnRelease>();
            comp.Add(o);
        }

        public void Add(IRelease o)
        {
            _toRelease.Add(o);
        }

        public bool Remove(IRelease o)
        {
            return _toRelease.Remove(o);
        }
    }


    public interface IRelease
    {
        void Release();
    }
}
