using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH
{
    public class EDOInt : EDataObj
    {
        public int val;

        public override string ToString()
        {
            return val.ToString();
        }

        public static EDOInt DFGet(string id, int defVal)
        {
            bool isNew = true;
            var edo = EData.FGet<EDOInt>(id, out isNew);
            if (isNew) edo.val = defVal;
            return edo;
        }
    }
}
