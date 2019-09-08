using Harmony;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
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
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.roxxploxx.unificamagica");

            // find the MakeRecipeProducts method of the class GenRecipe
            MethodInfo targetMethod = AccessTools.Method(typeof(WorkGiver_DoBill), "TryStartNewDoBillJob");
            // find the static method to call after (i.e. postfix) the targetmethod
            HarmonyMethod postfixmethod = new HarmonyMethod(typeof(WorkGiver_DoDissectionBill).GetMethod("TryStartNewDoBillJob_Postfix"));
            // patch the targetmethod, by calling postfixmethod after it ran, with no prefixmethod (i.e. null)
            harmony.Patch(targetMethod, null, postfixmethod);
        }
    }
}