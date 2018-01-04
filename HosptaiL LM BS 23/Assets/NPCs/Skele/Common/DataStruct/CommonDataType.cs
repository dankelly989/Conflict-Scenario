using System;
using System.Collections.Generic;

namespace MH
{
    [Serializable]
    public struct Int2
    {
        public int x;
        public int y;

        public Int2(int _x, int _y) { x = _x; y = _y; }

        public override string ToString()
        {
            return string.Format("({0}, {1})", x, y);
        }
        public override bool Equals(object obj)
        {
            Int2 rhs = (Int2)obj;
            return this == rhs;
        }
        public override int GetHashCode()
        {
            return x << 16 + (y & 0xFFFF);
        }

        public static bool operator==(Int2 lhs, Int2 rhs)
        {
            return lhs.x == rhs.x && lhs.y == rhs.y;
        }
        public static bool operator !=(Int2 lhs, Int2 rhs)
        {
            return lhs.x != rhs.x || lhs.y != rhs.y;
        }

    }

    [Serializable]
    public struct Int3 
    {
        public int x;
        public int y;
        public int z;

        public Int3(int _x, int _y, int _z) { x = _x; y = _y; z = _z; }

        public override string ToString()
        {
            return string.Format("({0}, {1}, {2})", x, y, z);
        }
        public override bool Equals(object obj)
        {
            Int3 rhs = (Int3)obj;
            return this == rhs;
        }
        public override int GetHashCode()
        {
            return (x << 20) + ((y << 10) & 0xFFC00) + (z & 0x3FF);
        }
        public static bool operator ==(Int3 lhs, Int3 rhs)
        {
            return lhs.x == rhs.x && lhs.y == rhs.y && lhs.z == rhs.z;
        }
        public static bool operator !=(Int3 lhs, Int3 rhs)
        {
            return lhs.x != rhs.x || lhs.y != rhs.y || lhs.z != rhs.z;
        }
    }
}
