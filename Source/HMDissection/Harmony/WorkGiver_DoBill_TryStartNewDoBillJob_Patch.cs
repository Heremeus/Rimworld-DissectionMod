using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse.AI;
using Verse;

namespace HMDissection
{
    [HarmonyPatch(typeof(WorkGiver_DoBill), "TryStartNewDoBillJob")]
    public static class WorkGiver_DoBill_TryStartNewDoBillJob_Patch
    {
        public static void Postfix(Pawn pawn, Bill bill, IBillGiver giver, List<ThingCount> chosenIngThings, Job haulOffJob, bool dontCreateJobIfHaulOffRequired, ref Job __result)
        {
            if (__result.def == JobDefOf.DoBill && bill.recipe == DissectionDefOf.DissectHumanRecipe)
            {
                __result.def = DissectionDefOf.DoDissectionBill;
            }
        }
    }
}
