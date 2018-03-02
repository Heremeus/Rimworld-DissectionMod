using Harmony;
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
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.roxxploxx.unificamagica");

            // find the ConsumeIngredients method of the class Verse.AI.Toils_Recipe
            MethodInfo targetmethod = AccessTools.Method(typeof(Toils_Recipe), "ConsumeIngredients");

            // find the static method to call before (i.e. Prefix) the targetmethod
            HarmonyMethod prefixmethod = new HarmonyMethod(typeof(HMDissection.HarmonyPatches).GetMethod("ConsumeIngredients_Prefix"));

            // patch the targetmethod, by calling prefixmethod before it runs, with no postfixmethod (i.e. null)
            harmony.Patch(targetmethod, prefixmethod, null);
        }


        // No __instance because ConsumeIngredients is static.
        // Set ingredients list to an empty list to stop the game from removing the corpse
        public static void ConsumeIngredients_Prefix(ref List<Thing> ingredients, ref RecipeDef recipe, ref Map map)
        {
            if(recipe is Recipes_DissectHuman)
            {
                ingredients = new List<Thing>();
            }
        }
    }
}