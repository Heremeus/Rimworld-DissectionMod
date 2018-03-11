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

            // find the EndCurrentJob method of the class Verse.AI.Pawn_JobTracker
            targetmethod = AccessTools.Method(typeof(GenRecipe), "MakeRecipeProducts");
            // find the static method to call after (i.e. postfix) the targetmethod
            HarmonyMethod postfixethod = new HarmonyMethod(typeof(HMDissection.HarmonyPatches).GetMethod("MakeRecipeProducts_PostFix"));
            // patch the targetmethod, by calling prefixmethod before it runs, with no postfixmethod (i.e. null)
            harmony.Patch(targetmethod, null, postfixethod);

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

        public static void MakeRecipeProducts_PostFix(RecipeDef recipeDef, Pawn worker, List<Thing> ingredients, Thing dominantIngredient, ref IEnumerable<Thing> __result)
        {
            if (recipeDef == DissectionDefOf.DissectHumanRecipe)
            {
                float efficiency = worker.GetStatValue(StatDefOf.MedicalSurgerySuccessChance, false) * 0.666f;
                var products = DissectionProducts(worker, ((Corpse)dominantIngredient).InnerPawn, efficiency, efficiency * 0.5f).ToList();
                __result = products;
#if DEBUG
                Log.Message("Changed products to:");
                for(int i = 0; i < products.Count; ++i)
                {
                    Log.Message(i + " - " + products[i] + " (" + products[i].def.defName + ")");
                }
#endif
            }
        }

        public static IEnumerable<Thing> DissectionProducts(Pawn dissector, Pawn corpse, float meatEfficiency, float leatherEfficiency)
        {
            if (corpse.RaceProps.meatDef != null)
            {
                int meatCount = GenMath.RoundRandom(corpse.GetStatValue(StatDefOf.MeatAmount, true) * meatEfficiency);
                if (meatCount > 0)
                {
                    Thing meat = ThingMaker.MakeThing(corpse.RaceProps.meatDef, null);
                    meat.stackCount = meatCount;
                    yield return meat;
                }
            }
            if (corpse.RaceProps.leatherDef != null)
            {
                int leatherCount = GenMath.RoundRandom(corpse.GetStatValue(StatDefOf.LeatherAmount, true) * leatherEfficiency);
                if (leatherCount > 0)
                {
                    Thing leather = ThingMaker.MakeThing(corpse.RaceProps.leatherDef, null);
                    leather.stackCount = leatherCount;
                    yield return leather;
                }
            }
            //if (!corpse.RaceProps.Humanlike)
            //{
            //    PawnKindLifeStage lifeStage = corpse.ageTracker.CurKindLifeStage;
            //    if (lifeStage.butcherBodyPart != null && (corpse.gender == Gender.None || (corpse.gender == Gender.Male && lifeStage.butcherBodyPart.allowMale) || (corpse.gender == Gender.Female && lifeStage.butcherBodyPart.allowFemale)))
            //    {
            //        for (;;)
            //        {
            //            BodyPartRecord record = (from x in corpse.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined)
            //                                     where x.IsInGroup(lifeStage.butcherBodyPart.bodyPartGroup)
            //                                     select x).FirstOrDefault<BodyPartRecord>();
            //            if (record == null)
            //            {
            //                break;
            //            }
            //            corpse.health.AddHediff(HediffMaker.MakeHediff(HediffDefOf.MissingBodyPart, corpse, record), null, null);
            //            Thing thing;
            //            if (lifeStage.butcherBodyPart.thing != null)
            //            {
            //                thing = ThingMaker.MakeThing(lifeStage.butcherBodyPart.thing, null);
            //            }
            //            else
            //            {
            //                thing = ThingMaker.MakeThing(record.def.spawnThingOnRemoved, null);
            //            }
            //            yield return thing;
            //        }
            //    }
            //}
            yield break;
        }

    }
}