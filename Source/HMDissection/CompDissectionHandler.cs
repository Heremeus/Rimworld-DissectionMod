using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace HMDissection
{
    public class CompDissectionHandler : ThingComp
    {
        private const float O_TICKS_PER_SECOND = 1f/60f;

        private float leftoverNutritionToDissect = 0f;

        public CompProperties_DissectionHandler Props
        {
            get
            {
                return (CompProperties_DissectionHandler)props;
            }
        }

        //Class="HMDissection.CompProperties_ExpHandler"><compClass>HMDissection.CompExpHandler</compClass><baseExpPerSecond>0.1</baseExpPerSecond></li> doesn't correspond to any field in type ThingDef.

        public override void CompTick()
        {
            base.CompTick();
            
            if (!parent.Spawned)
            {
                return;
            }
            if (parent.TryGetComp<CompPowerTrader>() != null && !parent.TryGetComp<CompPowerTrader>().PowerOn)
            {
                return;
            }
            
            Pawn interactingPawn = parent.InteractionCell.GetThingList(parent.Map)
                .Select(thing => thing as Pawn)
                .FirstOrDefault(pawn => pawn != null && pawn.CurJob?.targetA == parent);
            if (interactingPawn != null)
            {
                // Determine the amount of exp
                float exp = Props.baseExpPerSecond * O_TICKS_PER_SECOND;
                interactingPawn.skills.GetSkill(interactingPawn.CurJob.RecipeDef.workSkill).Learn(exp, false);
                
                if (leftoverNutritionToDissect <= 0f)
                {
                    // Take off parts from the corpse if there is nothing left to dissect
                    Building_WorkTable table = parent as Building_WorkTable;
                    Corpse corpse = parent.Position.GetThingList(parent.Map).FirstOrDefault(thing => thing is Corpse) as Corpse;
                    if (corpse != null)
                    {
                        leftoverNutritionToDissect = Dissected(corpse, interactingPawn);
                        Log.Message("Got " + leftoverNutritionToDissect + " nutrition from corpse.");
                    }
                }
                leftoverNutritionToDissect -= Props.nutritionDissectedPerSecond * O_TICKS_PER_SECOND;
            }
        }

        private float Dissected(Corpse corpse, Pawn actor)
        {
            if (corpse.Destroyed)
            {
                Log.Error(actor + " dissected destroyed thing " + corpse);
                return 0f;
            }
            actor.mindState.lastIngestTick = Find.TickManager.TicksGame;
            if (actor.needs.mood != null)
            {
                List<ThoughtDef> list = ThoughtsFromDissection(actor, corpse, corpse.def);
                for (int j = 0; j < list.Count; j++)
                {
                    actor.needs.mood.thoughts.memories.TryGainMemory(list[j], null);
                }
            }
            if (actor.IsColonist)
            {
                // TODO: custom tale
                //TaleRecorder.RecordTale(TaleDefOf.AteRawHumanlikeMeat, new object[]
                //{
                //    actor
                //});
            }
            int num;
            float result;
            DissectedCalculateAmounts(corpse, actor, out num, out result);
            // TODO: Joy gain from medical operation? Or already handled by recipe?
            //if (!actor.Dead && actor.needs.joy != null && Mathf.Abs(corpse.def.ingestible.joy) > 0.0001f && num > 0)
            //{
            //    JoyKindDef joyKind = (corpse.def.ingestible.joyKind == null) ? JoyKindDefOf.Gluttonous : corpse.def.ingestible.joyKind;
            //    actor.needs.joy.GainJoy((float)num * corpse.def.ingestible.joy, joyKind);
            //}
            if (num > 0)
            {
                if (num == corpse.stackCount)
                {
                    corpse.Destroy(DestroyMode.Vanish);
                }
                else
                {
                    corpse.SplitOff(num);
                }
            }
            return result;
        }

        private void DissectedCalculateAmounts(Corpse corpse, Pawn actor, out int numTaken, out float nutritionDissected)
        {
            BodyPartRecord bodyPartRecord = GetBestBodyPartToDissect(corpse, actor);
            if (bodyPartRecord == null)
            {
                Log.Error(string.Concat(new object[]
                {
                    actor,
                    " dissected ",
                    this,
                    " but no body part was found. Replacing with core part."
                }));
                bodyPartRecord = corpse.InnerPawn.RaceProps.body.corePart;
            }
            float bodyPartNutrition = FoodUtility.GetBodyPartNutrition(corpse.InnerPawn, bodyPartRecord);
            if (bodyPartRecord == corpse.InnerPawn.RaceProps.body.corePart)
            {
                if (PawnUtility.ShouldSendNotificationAbout(corpse.InnerPawn) && corpse.InnerPawn.RaceProps.Humanlike)
                {
                    Messages.Message("MessageDissectedByMedic".Translate(new object[]
                    {
                        corpse.InnerPawn.LabelShort,
                        actor.LabelIndefinite()
                    }).CapitalizeFirst(), actor, MessageTypeDefOf.NegativeEvent);
                }
                numTaken = 1;
            }
            else
            {
                Hediff_MissingPart hediff_MissingPart = (Hediff_MissingPart)HediffMaker.MakeHediff(HediffDefOf.MissingBodyPart, corpse.InnerPawn, bodyPartRecord);
                hediff_MissingPart.lastInjury = HediffDefOf.SurgicalCut;
                hediff_MissingPart.IsFresh = true;
                corpse.InnerPawn.health.AddHediff(hediff_MissingPart, null, null);
                numTaken = 0;
            }
            if (actor.RaceProps.Humanlike && Rand.Value < 0.001f)
            {
                // TODO: Add (custom?) disease while dissecting?
                //FoodUtility.AddFoodPoisoningHediff(actor, corpse);
            }
            nutritionDissected = bodyPartNutrition;
        }

        /// <summary>
        /// Returns the body part with the fewest nutrition first
        /// </summary>
        /// <param name="corpse"></param>
        /// <param name="ingester"></param>
        /// <returns></returns>
        private BodyPartRecord GetBestBodyPartToDissect(Corpse corpse, Pawn ingester)
        {
            IEnumerable<BodyPartRecord> source = from x in corpse.InnerPawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined)
                                                 where x.depth == BodyPartDepth.Outside && FoodUtility.GetBodyPartNutrition(corpse.InnerPawn, x) > 0.001f
                                                 select x;
            if (!source.Any<BodyPartRecord>())
            {
                return null;
            }
            return source.MinBy((BodyPartRecord x) => FoodUtility.GetBodyPartNutrition(corpse.InnerPawn, x));
        }

        private List<ThoughtDef> ThoughtsFromDissection(Pawn dissector, Corpse corpse, ThingDef thingDef)
        {
            return new List<ThoughtDef>();
            /*FoodUtility.ingestThoughts.Clear();
            if (dissector.needs == null || dissector.needs.mood == null)
            {
                return FoodUtility.ingestThoughts;
            }
            if (!dissector.story.traits.HasTrait(TraitDefOf.Ascetic) && foodDef.ingestible.tasteThought != null)
            {
                FoodUtility.ingestThoughts.Add(foodDef.ingestible.tasteThought);
            }
            CompIngredients compIngredients = corpse.TryGetComp<CompIngredients>();
            Building_NutrientPasteDispenser building_NutrientPasteDispenser = corpse as Building_NutrientPasteDispenser;
            if (FoodUtility.IsHumanlikeMeat(foodDef) && dissector.RaceProps.Humanlike)
            {
                FoodUtility.ingestThoughts.Add((!dissector.story.traits.HasTrait(TraitDefOf.Cannibal)) ? ThoughtDefOf.AteHumanlikeMeatDirect : ThoughtDefOf.AteHumanlikeMeatDirectCannibal);
            }
            else if (compIngredients != null)
            {
                for (int i = 0; i < compIngredients.ingredients.Count; i++)
                {
                    FoodUtility.AddIngestThoughtsFromIngredient(compIngredients.ingredients[i], dissector, FoodUtility.ingestThoughts);
                }
            }
            else if (building_NutrientPasteDispenser != null)
            {
                Thing thing = building_NutrientPasteDispenser.FindFeedInAnyHopper();
                if (thing != null)
                {
                    FoodUtility.AddIngestThoughtsFromIngredient(thing.def, dissector, FoodUtility.ingestThoughts);
                }
            }
            if (foodDef.ingestible.specialThoughtDirect != null)
            {
                FoodUtility.ingestThoughts.Add(foodDef.ingestible.specialThoughtDirect);
            }
            if (corpse.IsNotFresh())
            {
                FoodUtility.ingestThoughts.Add(ThoughtDefOf.AteRottenFood);
            }
            return FoodUtility.ingestThoughts;*/
        }
    }
}
