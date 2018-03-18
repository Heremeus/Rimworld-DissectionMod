using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using System;
using Verse.AI;

namespace HMDissection
{
    public class CompDissectionHandler : ThingComp
    {
        private const float O_TICKS_PER_SECOND = 1f/60f;
        private const float DEFAULT_NUTR_PER_CORPS = 4.5f;
        private const float RECIPE_WORK_AMOUNT = 7500;

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
            
            Pawn dissector = parent.InteractionCell.GetThingList(parent.Map)
                .Select(thing => thing as Pawn)
                .FirstOrDefault(pawn => pawn != null && pawn.CurJob?.targetA == parent);
            Corpse corpse = parent.Position.GetThingList(parent.Map).FirstOrDefault(thing => thing is Corpse) as Corpse;
            if (dissector != null && corpse != null && !corpse.Destroyed)
            {
                var speed = parent.GetStatValue(StatDefOf.WorkTableWorkSpeedFactor, true);
                JobDriver_DoBill jobDriver_DoBill = (JobDriver_DoBill)dissector.jobs.curDriver;
                if (leftoverNutritionToDissect <= 0f)
                {
                    // Destroy the part that was dissected
                    if (currentDissectedPart != null)
                    {
                        if(currentDissectedPart.def.defName.ToLower().Contains("torso"))
                        {
                            // Prevent DoRecipeWork from failing
                            jobDriver_DoBill.ReadyForNextToil();
#if DEBUG
                            tickLog += ("Destroying torso, spawning products now.") + Environment.NewLine;
#endif
                        }

                        DestroyPart(dissector, corpse, currentDissectedPart);
                        currentDissectedPart = null;
#if DEBUG
                        tickLog += ("Destroyed " + currentDissectedPart + " during dissection") + Environment.NewLine;
#endif
                    }

                    // Check if the last part was destroyed
                    if (corpse.Destroyed)
                    {
                        if (dissector.needs.mood != null)
                        {
                            List<ThoughtDef> list = ThoughtsFromDissection(dissector, corpse, corpse.def);
                            for (int j = 0; j < list.Count; j++)
                            {
                                dissector.needs.mood.thoughts.memories.TryGainMemory(list[j], null);
                            }
                        }

                        // TODO different tales depending on who was dissected (colonist vs stranger)
                        if (dissector.IsColonist)
                        {
                            TaleRecorder.RecordTale(DissectionDefOf.Dissected, new object[]
                            {
                                dissector,
                                corpse.InnerPawn
                            });
                        }
                        return;
                    }

                    // Get next part from the corpse to dissect
                    leftoverNutritionToDissect = GetNextDissectionPart(corpse, dissector, out currentDissectedPart);
#if DEBUG
                    tickLog += ("Got " + leftoverNutritionToDissect + " nutrition from corpse for " + currentDissectedPart) + Environment.NewLine;
#endif
                }
                float dissectedPerSecond = DEFAULT_NUTR_PER_CORPS / Dissection.Singleton.SecondsPerCorpse;
                leftoverNutritionToDissect -= dissectedPerSecond * O_TICKS_PER_SECOND * speed;


                // Determine the amount of exp
                float expPerSecond = Dissection.Singleton.ExpPerCorpse / Dissection.Singleton.SecondsPerCorpse;
                float exp = expPerSecond * O_TICKS_PER_SECOND * GetExpMultiplierForCorpse(corpse, currentDissectedPart) * speed;
                dissector.skills.GetSkill(dissector.CurJob.RecipeDef.workSkill).Learn(exp, Dissection.Singleton.IgnoreDailyLimit);

                float bodyPartsLeftPercent;
                if (PawnOrCorpseStatUtility.TryGetPawnOrCorpseStat(StatRequest.For(corpse), (Pawn x) => x.health.hediffSet.GetCoverageOfNotMissingNaturalParts(x.RaceProps.body.corePart), (ThingDef x) => 1f, out bodyPartsLeftPercent))
                {
                    float workLeftPercent = bodyPartsLeftPercent - (currentDissectedPart.coverageAbs - currentDissectedPart.coverageAbs * (leftoverNutritionToDissect / FoodUtility.GetBodyPartNutrition(corpse.InnerPawn, currentDissectedPart)));
                    jobDriver_DoBill.workLeft = Mathf.Max(10f, workLeftPercent * RECIPE_WORK_AMOUNT);
                }
                else
                {
                    Log.Error("Could not retrieve missing parts %");
                }
            }

            if(!string.IsNullOrEmpty(tickLog))
            {
                Log.Message(tickLog);
                tickLog = "";
            }
        }

        private static void DestroyPart(Pawn dissector, Corpse corpse, BodyPartRecord part)
        {
            int numTaken;
            if (part == corpse.InnerPawn.RaceProps.body.corePart)
            {
                if (PawnUtility.ShouldSendNotificationAbout(corpse.InnerPawn) && corpse.InnerPawn.RaceProps.Humanlike)
                {
                    Messages.Message("Dissection_MessageDissectedByMedic".Translate(new object[]
                    {
                        corpse.InnerPawn.LabelShort,
                        dissector.LabelIndefinite()
                    }).CapitalizeFirst(), dissector, MessageTypeDefOf.NeutralEvent);
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
            if (dissector.RaceProps.Humanlike && Rand.Value < 0.001f)
            {
                // TODO: Add (custom?) disease while dissecting?
                //FoodUtility.AddFoodPoisoningHediff(dissector, corpse);
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

        private float GetNextDissectionPart(Corpse corpse, Pawn dissector, out BodyPartRecord part)
        {
            if (corpse.Destroyed)
            {
                Log.Error(dissector + " dissected destroyed thing " + corpse);
                part = null;
                return 0f;
            }
            float result;
            DissectedCalculateAmounts(corpse, dissector, out result, out part);
            return result;
        }

        private void DissectedCalculateAmounts(Corpse corpse, Pawn dissector, out float nutritionDissected, out BodyPartRecord dissectedPart)
        {
            dissectedPart = GetNextBodyPartToDissect(corpse, dissector);
            if (dissectedPart == null)
            {
                Log.Error(string.Concat(new object[]
                {
                    dissector,
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
        /// <param name="dissector"></param>
        /// <returns></returns>
        private BodyPartRecord GetNextBodyPartToDissect(Corpse corpse, Pawn dissector)
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

        
        private List<ThoughtDef> ThoughtsFromDissection(Pawn dissector, Corpse corpse, ThingDef thingDef)
        {
            // TODO: Thoughts depending on wether the corpse was a colonist, prisoner, enemy or stranger
            List<ThoughtDef> thoughts = new List<ThoughtDef>();
            if(dissector.skills.GetSkill(SkillDefOf.Medicine).passion == Passion.None && !dissector.story.traits.HasTrait(TraitDefOf.Cannibal))
            {
                thoughts.Add(DissectionDefOf.DissectionNoPassion);
            }
            else if(dissector.skills.GetSkill(SkillDefOf.Medicine).passion == Passion.Minor)
            {
                thoughts.Add(DissectionDefOf.DissectionMinorPassion);
            }
            else if(dissector.skills.GetSkill(SkillDefOf.Medicine).passion == Passion.Major)
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
            return Mathf.Max(multiplier, 0.1f);
        }
    }
}
