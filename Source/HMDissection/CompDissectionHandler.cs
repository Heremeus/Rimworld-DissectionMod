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
                if (leftoverNutritionToDissect <= 0f)
                {
                    // Destroy the part that was dissected
                    if (currentDissectedPart != null)
                    {
                        if(currentDissectedPart.def.defName.ToLower().Contains("torso"))
                        {
                            // Destroyed last part, spawn products
                            //SpawnDissectionProducts(dissector, corpse);

                            // Make DoRecipeWork not fail
                            JobDriver_DoBill jobDriver_DoBill = (JobDriver_DoBill)dissector.jobs.curDriver;
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
                leftoverNutritionToDissect -= Props.nutritionDissectedPerSecond * O_TICKS_PER_SECOND * speed;


                // Determine the amount of exp
                float exp = Props.baseExpPerSecond * O_TICKS_PER_SECOND * GetExpMultiplierForCorpse(corpse, currentDissectedPart) * speed;
                dissector.skills.GetSkill(dissector.CurJob.RecipeDef.workSkill).Learn(exp, false);
            }
#if DEBUG
            if(!string.IsNullOrEmpty(tickLog))
            {
                Log.Message(tickLog);
                tickLog = "";
            }
#endif
        }

        // Todo: Instead of spawning meat and leather, spawn custom item that can be butchered for meat and leather
        private void SpawnDissectionProducts(Pawn dissector, Corpse corpse)
        {
            float efficiency = dissector.GetStatValue(StatDefOf.MedicalSurgerySuccessChance, false);
            var products = DissectionProducts(dissector, corpse.InnerPawn, efficiency, efficiency * 0.333f).ToList();
#if DEBUG
            Log.Message("Got following products: ");
            for(int i = 0; i < products.Count; ++i)
            {
                Log.Message(i + " - " + products[i] + "("+products[i].def.defName+")");
            }
#endif
            if (products.Count > 1)
            {
                for (int i = 0; i < products.Count; i++)
                {
                    if (!GenPlace.TryPlaceThing(products[i], dissector.Position, dissector.Map, ThingPlaceMode.Near, null))
                    {
                        Log.Error(string.Concat(new object[]
                        {
                        dissector,
                        " could not drop recipe product ",
                        products[i],
                        " near ",
                        dissector.Position
                        }));
                    }
                }
            }
            if (products.Count > 0)
            {
                products[0].SetPositionDirect(dissector.Position);
                IntVec3 c;
                if (StoreUtility.TryFindBestBetterStoreCellFor(products[0], dissector, dissector.Map, StoragePriority.Unstored, dissector.Faction, out c, true))
                {
                    dissector.carryTracker.TryStartCarry(products[0]);
                    dissector.CurJob.targetB = c;
                    dissector.CurJob.targetA = products[0];
                    dissector.CurJob.count = 99999;
                    //dissector.Reserve(products[0], dissector.CurJob, 1, products[0].stackCount, null);
                    //Log.Message("Reserved product");
                    //dissector.Reserve(c, dissector.CurJob, 1, -1, null);
                    //Log.Message("Reserved cell");
                    //dissector.CurJob.SetTarget(TargetIndex.B, c);
                    //dissector.CurJob.SetTarget(TargetIndex.A, products[0]);
                    //dissector.CurJob.count = 99999;
                    //dissector.CurJob.haulMode = HaulMode.ToCellStorage;
                }
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
            if (dissector.needs.mood != null)
            {
                List<ThoughtDef> list = ThoughtsFromDissection(dissector, corpse, corpse.def);
                for (int j = 0; j < list.Count; j++)
                {
                    dissector.needs.mood.thoughts.memories.TryGainMemory(list[j], null);
                }
            }
            if (dissector.IsColonist)
            {
                // TODO: custom tale
                //TaleRecorder.RecordTale(TaleDefOf.AteRawHumanlikeMeat, new object[]
                //{
                //    dissector
                //});
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

        public IEnumerable<Thing> DissectionProducts(Pawn dissector, Pawn corpse, float meatEfficiency, float leatherEfficiency)
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
