using System;
using System.Collections.Generic;

namespace MH
{
    /// <summary>
    /// the attribute used to mark script order
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class ScriptOrderAttribute : Attribute
    {
        private int m_order = 0;

        public ScriptOrderAttribute(int order)
        {
            m_order = order;
        }

        public int order
        {
            get { return m_order; }
        }
    }
}
