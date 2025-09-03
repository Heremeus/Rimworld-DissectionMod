using HarmonyLib;
using Verse;

namespace HMDissection
{
    [HarmonyPatch(typeof(Pawn_HealthTracker), "Notify_Resurrected")]
    public static class Pawn_HealthTracker_Notify_Resurrected_Patch
    {
        public static void Prefix(Pawn_HealthTracker __instance)
        {
            __instance.hediffSet.hediffs.RemoveAll((Hediff x) => x.def == DissectionDefOf.DissectedHediff);
        }
        
    }
}
