using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH
{
    public class EDOFloat : EDataObj
    {
        public float val;

        public override string ToString()
        {
            return val.ToString();
        }

        public static EDOFloat DFGet(string id, float defVal)
        {
            bool isNew = true;
            var edo = EData.FGet<EDOFloat>(id, out isNew);
            if (isNew) edo.val = defVal;
            return edo;
        }
    }
}
