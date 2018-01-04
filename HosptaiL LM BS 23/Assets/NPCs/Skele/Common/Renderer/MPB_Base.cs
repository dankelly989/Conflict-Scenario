using System;
using System.Collections.Generic;
using UnityEngine;

namespace MH
{
    public class MPB_Base
    {
        private static MaterialPropertyBlock s_block;

        public static MaterialPropertyBlock propBlock
        {
            get {
                if( s_block == null )
                {
                    s_block = new MaterialPropertyBlock();
                }
                s_block.Clear();
                return s_block;
            }
        }
    }
}
