using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using Verse.AI;

namespace HMDissection
{
    [StaticConstructorOnStartup]
    static class HarmonyPatches
    {
        // this static constructor runs to create a HarmonyInstance and install a patch.
        static HarmonyPatches()
        {

        }

        public static void Patch()
        {
            Harmony harmony = new Harmony("heremeus.rimworld.dissection.main");

            // find the MakeRecipeProducts method of the class GenRecipe
            MethodInfo targetMethod = AccessTools.Method(typeof(WorkGiver_DoBill), "TryStartNewDoBillJob");
            // find the static method to call after (i.e. postfix) the targetmethod
            HarmonyMethod postfixmethod = new HarmonyMethod(typeof(WorkGiver_DoDissectionBill).GetMethod("TryStartNewDoBillJob_Postfix"));
            // patch the targetmethod, by calling postfixmethod after it ran, with no prefixmethod (i.e. null)
            harmony.Patch(targetMethod, null, postfixmethod);

            // Remove dissected hediff on resurrection
            targetMethod = AccessTools.Method(typeof(Pawn_HealthTracker), "Notify_Resurrected");
            HarmonyMethod prefixmethod = new HarmonyMethod(typeof(HarmonyPatches).GetMethod("Notify_Resurrected_Prefix"));
            harmony.Patch(targetMethod, prefixmethod);

            // Patch HasJobOnThing for Harvest Organs Post Morten to stop duplicate work givers
            if (ModsConfig.ActiveModsInLoadOrder.Any(m => m.Name.Contains("Harvest Organs Post Mortem")))
            {
                targetMethod = AccessTools.Method(typeof(WorkGiver_DoBill), "JobOnThing");
                prefixmethod = new HarmonyMethod(typeof(WorkGiver_DoDissectionBill).GetMethod("JobOnThing_Prefix"));
                postfixmethod = new HarmonyMethod(typeof(WorkGiver_DoDissectionBill).GetMethod("JobOnThing_Postfix"));
                harmony.Patch(targetMethod, prefixmethod, postfixmethod);
            }
        }

        public static void Notify_Resurrected_Prefix(Pawn_HealthTracker __instance)
        {
            __instance.hediffSet.hediffs.RemoveAll((Hediff x) => x.def == DissectionDefOf.DissectedHediff);
        }
    }
}