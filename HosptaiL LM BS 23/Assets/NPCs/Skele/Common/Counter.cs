using UnityEngine;
using System.Collections;

[System.Serializable]
public class Counter
{
    [SerializeField]
	private float m_Thres = 0;
    [SerializeField]
	private float m_val = 0;
	private bool m_bRunning;
	private bool m_bAutoRewind;
    private bool m_bAutoStop;

    public Counter() : this(0)
    {}

    public Counter(float thres)
    {
        m_Thres = thres;
        m_val = 0;
        m_bRunning = true;
        m_bAutoRewind = true;
        m_bAutoStop = false;
    }

    public float Threshold
    {
        get { return m_Thres; }
        set { m_Thres = value; }
    }

    public float CurVal
    {
        get { return m_val; }
        set { m_val = value; }
    }
	
	/**
	 * return true iff time up
	 */ 
	public bool Update(float v)
	{
		if( ! m_bRunning )
			return false;
		
		m_val += v;
		if( m_val > m_Thres )
		{
			if( m_bAutoRewind )
				m_val = 0;
            if (m_bAutoStop)
                m_bRunning = false;
			return true;
		}
		else
		{
			return false;
		}
	}
	
	public bool SetRunning(bool bRun){
		bool bPrev = m_bRunning;
		m_bRunning = bRun;
		return bPrev;
	}
	
	public void Stop(){
		SetRunning(true);
		Rewind();
	}
	
	public void Rewind(){
		m_val = 0;
	}

    /// <summary>
    /// set val to thres, so next update will return true
    /// </summary>
    public void SetToMax(){
        m_val = m_Thres;
    }
	
	public void SetAutoRewind(bool bVal){
		m_bAutoRewind = bVal;
	}

    /// <summary>
    /// if true, when reach threshold, bRunning will be set to false
    /// you need to manually set running again
    /// </summary>
    public void SetAutoStop(bool bVal)
    {
        m_bAutoStop = bVal;
    }
	
	public float GetLeftTime(){
		return m_Thres - m_val;
	}
} 
