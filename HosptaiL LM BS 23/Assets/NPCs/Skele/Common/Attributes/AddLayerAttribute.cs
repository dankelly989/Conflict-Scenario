using System;
using System.Collections.Generic;
using UnityEngine;

namespace MH
{
    /// <summary>
    /// will try to add the layer, if already present, do nothing;
    /// if specified slot, and the slot is occupied, will do nothing
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class AddLayerAttribute : Attribute
    {
        private string m_layerName;
        private int m_slotIdx;

        /// <summary>
        /// init the attribute
        /// </summary>
        /// <param name="layerName">the layer name</param>
        /// <param name="specifySlot">if lt 0, then ignore the parameter and use any possible slot</param>
        public AddLayerAttribute(string layerName, int specifySlot = -1)
        {
            m_layerName = layerName;
            m_slotIdx = specifySlot;
        }

        public string layerName
        {
            get { return m_layerName; }
        }

        public int slotIdx
        {
            get { return m_slotIdx; }
        }
    }
}
