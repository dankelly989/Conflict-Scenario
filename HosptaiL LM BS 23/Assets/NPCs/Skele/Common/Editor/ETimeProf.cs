using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH
{
    /// <summary>
    /// editor time profiler
    /// </summary>
	public class ETimeProf
	{
        public double m_Rec;

        public double[] m_SecR;
        public double[] m_Sections;

        public ETimeProf()
        {
            Reset();
        }

        public void Reset()
        {
            m_Rec = EditorApplication.timeSinceStartup;
            m_Sections = new double[SEC_CNT];
            m_SecR = new double[SEC_CNT];
        }

        public double Click(string prompt, bool doReset = true)
        {
            double r = EditorApplication.timeSinceStartup - m_Rec;
            if( doReset )
                Reset();

            Dbg.Log("{0}: {1:F6}", prompt, r);
            return r;
        }

        public void SecStart(int sectionIdx)
        {
            m_SecR[sectionIdx] = EditorApplication.timeSinceStartup;
        }

        public void SecEnd(int sectionIdx)
        {
            m_Sections[sectionIdx] += EditorApplication.timeSinceStartup - m_SecR[sectionIdx];
        }

        public void SecShow(int sectionIdx, string prompt)
        {
            Dbg.Log("{0}: {1:F6}", prompt, m_Sections[sectionIdx]);
        }

        public void SecShowAll()
        {
            for(int i=0; i<SEC_CNT; ++i)
            {
                SecShow(i, "sec" + i);
            }
        }


        private const int SEC_CNT = 16;
	}
}
