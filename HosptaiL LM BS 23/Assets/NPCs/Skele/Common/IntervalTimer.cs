using System;
using System.Collections.Generic;
using UnityEngine;

namespace MH
{
    [Serializable]
	public class IntervalTimer
	{
        private float m_prevTime = float.MinValue * 0.1f;
        [SerializeField]
        private float m_interval = 1f;

        public IntervalTimer(float interval)
        {
            m_interval = interval;
        }

        public float interval
        {
            get { return m_interval; }
            set { m_interval = value; }
        }

        public float prevTime
        {
            get { return m_prevTime; }
            set { m_prevTime = value; }
        }

        public void Reset(float newInterval)
        {
            m_interval = newInterval;
            m_prevTime = float.MinValue * 0.1f;
        }

        public bool Peek()
        {
            float curTime = Time.time;
            return curTime - m_prevTime > m_interval;
        }

        public bool Check()
        {
            float curTime = Time.time;
            if( curTime - m_prevTime > m_interval )
            {
                m_prevTime = curTime;
                return true;
            }
            else
            {
                return false;
            }
        }
	}
}
