using System;
using System.Collections.Generic;

namespace MH
{
    /// <summary>
    /// struct pair
    /// </summary>
    [Serializable]
    public struct SPair<P, T>
    {
        public P v0;
        public T v1;

        public SPair(P _v0, T _v1) { v0 = _v0; v1 = _v1; }
    }
}
