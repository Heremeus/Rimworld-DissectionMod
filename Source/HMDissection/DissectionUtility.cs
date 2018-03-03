using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace HMDissection
{
    internal static class DissectionUtility
    {
        public static bool IsDisease(this Hediff hediff)
        {
            return hediff.def.makesSickThought;
        }
    }
}
