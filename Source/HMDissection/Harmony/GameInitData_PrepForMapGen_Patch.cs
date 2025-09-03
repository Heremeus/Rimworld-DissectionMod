using HarmonyLib;
using System.Collections.Generic;
using Verse;

namespace HMDissection
{
    [HarmonyPatch(typeof(GameInitData), "PrepForMapGen")]
    public static class GameInitData_PrepForMapGen_Patch
    {
        public static void Postfix(List<Pawn> ___startingAndOptionalPawns)
        {
            // Set priority for medical training if the pawn has a passion for medicine
            // This way the pawn will practice medicine by default, even if it has 0 medicine skill
            foreach (Pawn pawn in ___startingAndOptionalPawns)
            {
                WorkSettingsPatches.SetDissectionPriorityForPassionatedPawns(pawn);
            }
        }
        
    }
}
