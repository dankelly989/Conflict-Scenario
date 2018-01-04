using System;
using System.Collections.Generic;

namespace MH
{


    #region "Stack list"
 

    /// <summary>
    /// stack-alloc fixed-maxlen list
    /// </summary>
    public struct SFList<T>
    {
        private const int MAXSIZE = 8;
        private T v0, v1, v2, v3, v4, v5, v6, v7;
        private int size;

        //public SFList() //struct cannot contain explicit parameter-less constructor
        //{
        //    size = 0;
        //    v0 = v1 = v2 = v3 = v4 = v5 = v6 = v7 = default(T);
        //}
        //public static SFList<T> Create(T v0) { SFList<T> lst = new SFList<T>(1); lst.v0 = v0; return lst; }
        //public static SFList<T> Create(T v0, T v1) { SFList<T> lst = new SFList<T>(2); lst.v0 = v0; lst.v1 = v1; return lst; }
        //public static SFList<T> Create(T v0, T v1, T v2) { SFList<T> lst = new SFList<T>(3); lst.v0 = v0; lst.v1 = v1; lst.v2 = v2; return lst; }
        //public static SFList<T> Create(T v0, T v1, T v2, T v3) { SFList<T> lst = new SFList<T>(4); lst.v0 = v0; lst.v1 = v1; lst.v2 = v2; lst.v3 = v3; return lst; }
        //public static SFList<T> Create(T v0, T v1, T v2, T v3, T v4) { SFList<T> lst = new SFList<T>(5); lst.v0 = v0; lst.v1 = v1; lst.v2 = v2; lst.v3 = v3; lst.v4 = v4; return lst; }
        //public static SFList<T> Create(T v0, T v1, T v2, T v3, T v4, T v5) { SFList<T> lst = new SFList<T>(6); lst.v0 = v0; lst.v1 = v1; lst.v2 = v2; lst.v3 = v3; lst.v4 = v4; lst.v5 = v5; return lst; }
        //public static SFList<T> Create(T v0, T v1, T v2, T v3, T v4, T v5, T v6) { SFList<T> lst = new SFList<T>(7); lst.v0 = v0; lst.v1 = v1; lst.v2 = v2; lst.v3 = v3; lst.v4 = v4; lst.v5 = v5; lst.v6 = v6; return lst; }
        //public static SFList<T> Create(T v0, T v1, T v2, T v3, T v4, T v5, T v6, T v7) { SFList<T> lst = new SFList<T>(8); lst.v0 = v0; lst.v1 = v1; lst.v2 = v2; lst.v3 = v3; lst.v4 = v4; lst.v5 = v5; lst.v6 = v6; lst.v7 = v7; return lst; }
        
        public void Add(T newElem)
        {
            if (size >= MAXSIZE) throw new ArgumentOutOfRangeException();
            ++size;

            this[size - 1] = newElem;
        }
        public void RemoveAt(int idx)
        {
            if (idx >= size || idx < 0) throw new ArgumentOutOfRangeException();
            for (int i = idx; i < size-1; ++i)
            {
                this[i] = this[i + 1];
            }
            size--;
        }
        public int FindIndex(T elem)
        {
            for (int i = 0; i < size; ++i)
            {
                if (EqualityComparer<T>.Default.Equals(this[i], elem))
                {
                    return i;
                }
            }
            return -1;
        }
        public bool Remove(T elem)
        {
            int idx = FindIndex(elem);
            if (-1 == idx)
                return false;

            RemoveAt(idx);
            return true;
        }

        public void Clear()
        {
            size = 0;
        }

        public int Count { get { return size; } }
        public T this[int i]
        {
            get
            {
                if (i >= size) { throw new ArgumentOutOfRangeException(); }
                switch (i)
                {
                    case 0: return v0;
                    case 1: return v1;
                    case 2: return v2;
                    case 3: return v3;
                    case 4: return v4;
                    case 5: return v5;
                    case 6: return v6;
                    case 7: return v7;
                    default: throw new ArgumentOutOfRangeException();
                }
            }
            set
            {
                if (i >= size) { throw new ArgumentOutOfRangeException(); }
                switch (i)
                {
                    case 0: v0 = value; return;
                    case 1: v1 = value; return;
                    case 2: v2 = value; return;
                    case 3: v3 = value; return;
                    case 4: v4 = value; return;
                    case 5: v5 = value; return;
                    case 6: v6 = value; return;
                    case 7: v7 = value; return;
                    default: throw new ArgumentOutOfRangeException();
                }
            }
        }
    }


    /// <summary>
    /// stack-alloc list
    /// </summary>
    public struct SList<T>
    {
        private const int STANDARD_LEN = 8;
        private T v0, v1, v2, v3, v4, v5, v6, v7;
        private int size;

        private List<T> heapLst; // list on heap, only alloc when surpass the STANDARD_LEN

        //public SList()
        //{
        //    size = 0;
        //    v0 = v1 = v2 = v3 = v4 = v5 = v6 = v7 = default(T);
        //    heapLst = null;
        //}
        
        public void Add(T newElem)
        {
            ++size;

            if( size == STANDARD_LEN + 1 && heapLst == null )
            {
                heapLst = new List<T>();
            }

            if( size > STANDARD_LEN )
            {
                heapLst.Add(newElem);
            }
            else
            {
                this[size - 1] = newElem;
            }
        }
        public void RemoveAt(int idx)
        {
            if (idx >= size || idx < 0) throw new ArgumentOutOfRangeException();

            if( size <= STANDARD_LEN) // no heapLst
            {
                for (int i = idx; i < size - 1; ++i)
                {
                    this[i] = this[i + 1];
                }
            }
            else //has heapLst
            {
                if( idx >= STANDARD_LEN ) //start at heapLst
                {
                    heapLst.RemoveAt(idx - STANDARD_LEN);
                }
                else //start at fixed section
                {
                    for (int i = idx; i < STANDARD_LEN; ++i)
                    {
                        this[i] = this[i + 1];
                    }
                    heapLst.RemoveAt(0);
                }
            }

            size--;
        }
        public int FindIndex(T elem)
        {
            for (int i = 0; i < size; ++i)
            {
                if (EqualityComparer<T>.Default.Equals(this[i], elem))
                {
                    return i;
                }
            }
            return -1;
        }
        public bool Remove(T elem)
        {
            int idx = FindIndex(elem);
            if (-1 == idx)
                return false;

            RemoveAt(idx);
            return true;
        }

        public void Clear()
        {
            if (heapLst != null)
                heapLst.Clear();
            size = 0;
        }

        public int Count { get { return size; } }
        public T this[int i]
        {
            get
            {
                if (i >= size) { throw new ArgumentOutOfRangeException(); }
                switch (i)
                {
                    case 0: return v0;
                    case 1: return v1;
                    case 2: return v2;
                    case 3: return v3;
                    case 4: return v4;
                    case 5: return v5;
                    case 6: return v6;
                    case 7: return v7;
                    default: return heapLst[i - 8];
                }
            }
            set
            {
                if (i >= size) { throw new ArgumentOutOfRangeException(); }
                switch (i)
                {
                    case 0: v0 = value; return;
                    case 1: v1 = value; return;
                    case 2: v2 = value; return;
                    case 3: v3 = value; return;
                    case 4: v4 = value; return;
                    case 5: v5 = value; return;
                    case 6: v6 = value; return;
                    case 7: v7 = value; return;
                    default: heapLst[i - 8] = value; return;
                }
            }
        }
    }

    #endregion
    
}
