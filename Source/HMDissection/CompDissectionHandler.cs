using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using System;

namespace HMDissection
{
    public class CompDissectionHandler : ThingComp
    {
        private const float O_TICKS_PER_SECOND = 1f/60f;

        private BodyPartRecord currentDissectedPart;
        private float leftoverNutritionToDissect = 0f;

        private string tickLog = "";

        public CompProperties_DissectionHandler Props
        {
            get
            {
                return (CompProperties_DissectionHandler)props;
            }
        }

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
            Corpse corpse = parent.Position.GetThingList(parent.Map).FirstOrDefault(thing => thing is Corpse) as Corpse;
            if (interactingPawn != null && corpse != null)
            {
                if (leftoverNutritionToDissect <= 0f)
                {
                    // Destroy the part that was dissected
                    if (currentDissectedPart != null)
                    {
                        DestroyPart(interactingPawn, corpse, currentDissectedPart);
                    }
#if DEBUG
                    tickLog += ("Destroyed " + currentDissectedPart + " during dissection") + Environment.NewLine;
#endif

                    // Get next part from the corpse to dissect
                    if (corpse != null)
                    {
                        leftoverNutritionToDissect = GetNextDissectionPart(corpse, interactingPawn, out currentDissectedPart);
#if DEBUG
                        tickLog += ("Got " + leftoverNutritionToDissect + " nutrition from corpse.") + Environment.NewLine;
#endif
                    }
                }
                leftoverNutritionToDissect -= Props.nutritionDissectedPerSecond * O_TICKS_PER_SECOND;


                // Determine the amount of exp
                float exp = Props.baseExpPerSecond * O_TICKS_PER_SECOND * GetExpMultiplierForCorpse(corpse, currentDissectedPart);
                interactingPawn.skills.GetSkill(interactingPawn.CurJob.RecipeDef.workSkill).Learn(exp, false);
            }
#if DEBUG
            if(!string.IsNullOrEmpty(tickLog))
            {
                Log.Message(tickLog);
                tickLog = "";
            }
#endif
        }

        private static void DestroyPart(Pawn actor, Corpse corpse, BodyPartRecord part)
        {
            int numTaken;
            if (part == corpse.InnerPawn.RaceProps.body.corePart)
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
                Hediff_MissingPart hediff_MissingPart = (Hediff_MissingPart)HediffMaker.MakeHediff(HediffDefOf.MissingBodyPart, corpse.InnerPawn, part);
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
            if (numTaken > 0)
            {
                if (numTaken == corpse.stackCount)
                {
                    corpse.Destroy(DestroyMode.Vanish);
                }
                else
                {
                    corpse.SplitOff(numTaken);
                }
            }
        }

        private float GetNextDissectionPart(Corpse corpse, Pawn actor, out BodyPartRecord part)
        {
            if (corpse.Destroyed)
            {
                Log.Error(actor + " dissected destroyed thing " + corpse);
                part = null;
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
            DissectedCalculateAmounts(corpse, actor, out result, out part);
            return result;
        }

        private void DissectedCalculateAmounts(Corpse corpse, Pawn actor, out float nutritionDissected, out BodyPartRecord dissectedPart)
        {
            dissectedPart = GetNextBodyPartToDissect(corpse, actor);
            if (dissectedPart == null)
            {
                Log.Error(string.Concat(new object[]
                {
                    actor,
                    " dissected ",
                    this,
                    " but no body part was found. Replacing with core part."
                }));
                dissectedPart = corpse.InnerPawn.RaceProps.body.corePart;
            }
            float bodyPartNutrition = FoodUtility.GetBodyPartNutrition(corpse.InnerPawn, dissectedPart);
            nutritionDissected = bodyPartNutrition;
        }

        /// <summary>
        /// Returns the body part with the fewest nutrition first
        /// </summary>
        /// <param name="corpse"></param>
        /// <param name="actor"></param>
        /// <returns></returns>
        private BodyPartRecord GetNextBodyPartToDissect(Corpse corpse, Pawn actor)
        {
            IEnumerable<BodyPartRecord> source = from x in corpse.InnerPawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined)
                                                 where x.depth == BodyPartDepth.Outside && FoodUtility.GetBodyPartNutrition(corpse.InnerPawn, x) > 0.00001f
                                                 select x;
            if (!source.Any<BodyPartRecord>())
            {
                return null;
            }
            BodyPartRecord bodyPart = source.RandomElementByWeight(part => 1f / FoodUtility.GetBodyPartNutrition(corpse.InnerPawn, part));
            var notMissingChildParts = bodyPart.GetDirectChildParts().Where(part => !corpse.InnerPawn.health.hediffSet.PartIsMissing(part));
            while (notMissingChildParts.Any())
            {
                bodyPart = notMissingChildParts.RandomElementByWeight(part => 1f / FoodUtility.GetBodyPartNutrition(corpse.InnerPawn, part));
                notMissingChildParts = bodyPart.GetDirectChildParts().Where(part => !corpse.InnerPawn.health.hediffSet.PartIsMissing(part));
            }
            return bodyPart;
            //return source.MinBy((BodyPartRecord x) => FoodUtility.GetBodyPartNutrition(corpse.InnerPawn, x));
        }

        
        private List<ThoughtDef> ThoughtsFromDissection(Pawn actor, Corpse corpse, ThingDef thingDef)
        {
            List<ThoughtDef> thoughts = new List<ThoughtDef>();
            if(actor.skills.GetSkill(SkillDefOf.Medicine).passion == Passion.None && !actor.story.traits.HasTrait(TraitDefOf.Cannibal))
            {
                thoughts.Add(DissectionDefOf.DissectionNoPassion);
            }
            else if(actor.skills.GetSkill(SkillDefOf.Medicine).passion == Passion.Minor)
            {
                thoughts.Add(DissectionDefOf.DissectionMinorPassion);
            }
            else if(actor.skills.GetSkill(SkillDefOf.Medicine).passion == Passion.Major)
            {
                thoughts.Add(DissectionDefOf.DissectionMajorPassion);
            }
            return thoughts;
        }

        private float GetExpMultiplierForCorpse(Corpse corpse, BodyPartRecord part)
        {
            float multiplier = 1.0f;
            int numberOfDiseases = Mathf.Min(corpse.InnerPawn.health.hediffSet.hediffs.Count(hediff => hediff.IsDisease()), Props.diseaseStackLimit);
            for (int i = 0; i < numberOfDiseases; ++i)
            {
                multiplier += Props.baseDiseasesBonusPercent * Mathf.Pow(Props.stackedDiseaseMultiplier, i);
            }
            // Lower experience if currently dissecting part is damaged
            int numberOfDamages = Math.Min(corpse.InnerPawn.health.hediffSet.hediffs.Count(hediff => !hediff.IsDisease() && hediff.Part != null && hediff.Part.def.defName == part.def.defName), Props.damageStackLimit);
#if DEBUG
            tickLog += ("Number of damages on " + part.def.defName + ": " + numberOfDamages) + Environment.NewLine;
#endif
            for (int i = 0; i < numberOfDamages; ++i)
            {
                multiplier -= Props.baseDamageMalusPercent * Mathf.Pow(Props.stackedDamageMultiplier, i);
            }
#if DEBUG
            tickLog += ("EXP Multiplier for corpse: " + multiplier) + Environment.NewLine;
#endif
            return Mathf.Max(multiplier, 0.333f);
        }
    }
}
