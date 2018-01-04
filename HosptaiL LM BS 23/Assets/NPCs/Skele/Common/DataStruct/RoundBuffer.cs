using System;
using System.Collections.Generic;

/**
 * this class is used to store any object in a round-buffer fashion
 */
public class RoundBuffer<T>
{
	#region "data"
    // data
    private T[] m_buffer = null;
    private int m_frontIdx = 0, m_backIdx = 0;
    private int m_Count = 0;
    #endregion

	#region "public method"
    // public method
    public RoundBuffer(int len)
    {
        Dbg.Assert(len >= 1, "RoundBuffer.ctor: len must be larger than 0");
        m_buffer = new T[len];
        for (int idx = 0; idx < len; ++idx)
        {
            m_buffer[idx] = default(T);
        }
    }

    /// <summary>
    /// return the count of elements in roundbuffer
    /// </summary>
    public int Count { get { return m_Count; } }

    /// <summary>
    /// return the capacity of round buffer
    /// </summary>
    public int Capacity { get { return m_buffer.Length; } }

    /// <summary>
    /// check if is full already
    /// </summary>
    public bool Full { get { return Count == Capacity; } }

    /// <summary>
    /// Push the specified obj at back, pop from front if needed
    /// </summary>
    public void PushBack(T obj)
    {
        if (Count == Capacity)
            m_frontIdx = (m_frontIdx + 1) % m_buffer.Length;

        m_buffer[m_backIdx] = obj;
        m_backIdx = (m_backIdx + 1) % m_buffer.Length;
        m_Count = (m_Count + 1) > Capacity ? Capacity : m_Count + 1;
    }

    /// <summary>
    /// Pops the front.
    /// </summary>
    /// <returns>
    /// The front.
    /// </returns>
    public T PopFront()
    {
        Dbg.Assert(m_Count != 0, "RoundBuffer.PopFront: no element");
        
        T obj = m_buffer[m_frontIdx];
        m_buffer[m_frontIdx] = default(T);
        m_frontIdx = (m_frontIdx + 1) % m_buffer.Length;
        m_Count--;

        return obj;
    }

    /// <summary>
    /// get element by index, this index is for the roundbuffer, not for underlying buffer
    /// </summary>
    public T Get(int idx)
    {
        Dbg.Assert(idx < Count, "RoundBuffer.Get: idx out of bound: {0}, {1}", idx, Count );
        return m_buffer[(m_frontIdx + idx) % Capacity];
    }

    public Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }
    #endregion

	#region "private method"
    // private method

    #endregion

	#region "constant data"
    // constant data

    #endregion

    #region "Inner Struct"
    // "Inner Struct" 
    
    public struct Enumerator : IEnumerator<T>
    {
        private RoundBuffer<T> m_cont;
        private int m_Idx;

        public Enumerator(RoundBuffer<T> cont)
        {
            m_cont = cont;
            m_Idx = -1;
        }

        public bool MoveNext()
        {
            m_Idx++;
            return m_Idx < m_cont.Count;
        }

        public T Current
        {
            get { return m_cont.Get(m_Idx); }
        }
        object System.Collections.IEnumerator.Current
        {
            get { return Current; }
        }

        public void Dispose()
        {

        }

        public void Reset()
        {

        }
    }

    #endregion
}
