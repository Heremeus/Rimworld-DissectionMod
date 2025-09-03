using HarmonyLib;
using RimWorld;
using Verse;

namespace HMDissection
{
    [HarmonyPatch(typeof(Pawn_WorkSettings), "EnableAndInitialize")]
    public static class Pawn_WorkSettings_EnableAndInitialize_Patch
    {
        public static void Postfix(Pawn ___pawn)
        {
            WorkSettingsPatches.SetDissectionPriorityForPassionatedPawns(___pawn);
        }
        
    }
}
