using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace HMDissection
{
    internal static class DissectionUtility
    {
        public static readonly HashSet<BodyPartDef> DissectableBodyParts = new HashSet<BodyPartDef>
        {
            DissectionDefOf.Arm,
            DissectionDefOf.Leg,
            DissectionDefOf.Jaw,
            DissectionDefOf.Brain,
            DissectionDefOf.Eye,
            DissectionDefOf.Heart,
            DissectionDefOf.Liver,
            DissectionDefOf.Lung,
            DissectionDefOf.Stomach
        };
    }
}
