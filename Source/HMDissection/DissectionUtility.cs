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
            BodyPartDefOf.Arm,
            BodyPartDefOf.Leg,
            BodyPartDefOf.Jaw,
            BodyPartDefOf.Brain,
            BodyPartDefOf.Eye,
            BodyPartDefOf.Heart,
            BodyPartDefOf.Liver,
            DissectionDefOf.Lung,
            BodyPartDefOf.Stomach
        };
    }
}
