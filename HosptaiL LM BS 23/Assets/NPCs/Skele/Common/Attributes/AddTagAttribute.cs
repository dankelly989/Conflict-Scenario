using System;
using System.Collections.Generic;
using UnityEngine;

namespace MH
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class AddTagAttribute : Attribute
    {
        private string m_tagName;

        /// <summary>
        /// init the attribute
        /// </summary>
        /// <param name="tagName">the tag name</param>
        public AddTagAttribute(string tagName)
        {
            m_tagName = tagName;
        }

        public string tagName
        {
            get { return m_tagName; }
        }
    }
}
