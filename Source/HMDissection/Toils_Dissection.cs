using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;

namespace HMDissection
{
    public static class Toils_Dissection
    {
        public static Toil StripCorpse(TargetIndex corpseIndex)
        {
            Toil toil = new Toil();
            toil.initAction = delegate
            {
                Pawn actor = toil.actor;
                Job curJob = actor.jobs.curJob;
                Thing thing = curJob.GetTarget(corpseIndex).Thing;
                Designation designation = actor.Map.designationManager.DesignationOn(thing, DesignationDefOf.Strip);
                if (designation != null)
                {
                    designation.Delete();
                }
                if (thing is IStrippable strippable)
                {
                    strippable.Strip();
                }
                actor.records.Increment(RecordDefOf.BodiesStripped);
            };
            
            return toil;
        }

        public static Toil DoDissectionRecipeWork()
        {
            Toil toil = new Toil();
            toil.initAction = delegate
            {
                Pawn actor = toil.actor;
                Job curJob = actor.jobs.curJob;
                JobDriver_DoBill jobDriver = (JobDriver_DoBill)actor.jobs.curDriver;

                jobDriver.workLeft = curJob.bill.recipe.WorkAmountTotal(null);

                jobDriver.billStartTick = Find.TickManager.TicksGame;
                jobDriver.ticksSpentDoingRecipeWork = 0;
                curJob.bill.Notify_DoBillStarted(actor);
            };

            toil.tickAction = delegate
            {
                Pawn actor = toil.actor;
                Job curJob = actor.jobs.curJob;
                JobDriver_DoBill jobDriver_DoBill = (JobDriver_DoBill)actor.jobs.curDriver;

                jobDriver_DoBill.ticksSpentDoingRecipeWork++;
                curJob.bill.Notify_PawnDidWork(actor);
                if (toil.actor.CurJob.GetTarget(TargetIndex.A).Thing is IBillGiverWithTickAction billGiverWithTickAction)
                {
                    billGiverWithTickAction.UsedThisTick();
                }

                if (!curJob.playerForced)
                {

                }

                var passion = actor.skills.GetSkill(SkillDefOf.Medicine).passion;

                float workDone = 1f * actor.GetStatValue(StatDefOf.WorkSpeedGlobal, true);

                if (DebugSettings.fastCrafting)
                {
                    workDone *= 30f;
                }

                jobDriver_DoBill.workLeft -= workDone;

                actor.GainComfortFromCellIfPossible();

                if (jobDriver_DoBill.workLeft <= 0f)
                {
                    jobDriver_DoBill.ReadyForNextToil();
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            toil.WithEffect(() => toil.actor.CurJob.bill.recipe.effectWorking, TargetIndex.A);
            toil.PlaySustainerOrSound(() => toil.actor.CurJob.bill.recipe.soundWorking);
            toil.WithProgressBar(TargetIndex.A, delegate
            {
                Pawn actor = toil.actor;
                Job curJob = actor.CurJob;
                return 1f - ((JobDriver_DoBill)actor.jobs.curDriver).workLeft / curJob.bill.recipe.WorkAmountTotal(null);
            }, false, -0.5f);
            toil.FailOn(() => toil.actor.CurJob.bill.suspended);
            toil.activeSkill = (() => toil.actor.CurJob.bill.recipe.workSkill);
            return toil;
        }

        public static Toil FinishRecipeAndStartStoringCorpse(TargetIndex corpseIndex)
        {
            Toil toil = new Toil();
            toil.initAction = delegate
            {
                Pawn actor = toil.actor;
                Job curJob = actor.jobs.curJob;
                Corpse corpse = actor.jobs.curJob.GetTarget(TargetIndex.B).Thing as Corpse;

                actor.skills.GetSkill(SkillDefOf.Medicine).Learn(Dissection.Singleton.ExpPerCorpse, Dissection.Singleton.IgnoreDailyLimit);

                ApplyThoughts(actor, corpse);
                RecordTale(actor, corpse);

                bool destroyedBody = RemoveDissectedBodyParts(actor, corpse);

                curJob.bill.Notify_IterationCompleted(actor, new List<Thing>() { corpse });
                RecordsUtility.Notify_BillDone(actor, new List<Thing>() { corpse });

                if(destroyedBody)
                {
                    actor.jobs.EndCurrentJob(JobCondition.Succeeded);
                    return;
                }

                IntVec3 bestStoreCell = IntVec3.Invalid;
                if (curJob.bill.GetStoreMode() == BillStoreModeDefOf.BestStockpile)
                {
                    StoreUtility.TryFindBestBetterStoreCellFor(corpse, actor, actor.Map, StoragePriority.Unstored, actor.Faction, out bestStoreCell, true);
                }
                else if (curJob.bill.GetStoreMode() == BillStoreModeDefOf.SpecificStockpile)
                {
                    StoreUtility.TryFindBestBetterStoreCellForIn(corpse, actor, actor.Map, StoragePriority.Unstored, actor.Faction, curJob.bill.GetStoreZone().slotGroup, out bestStoreCell, true);
                }
                else
                {
                    Log.ErrorOnce("Unknown store mode", 9158246);
                }

                if (bestStoreCell.IsValid)
                {
                    corpse.DeSpawn();
                    actor.carryTracker.TryStartCarry(corpse);
                    curJob.targetC = bestStoreCell;
                    curJob.targetB = corpse;
                    curJob.count = 99999;
                    
                    curJob.placedThings?.Clear();
                }
                else
                {
                    actor.jobs.EndCurrentJob(JobCondition.Succeeded);
                }
            };
            return toil;
        }

        public static Toil PlaceHauledThingInCell(TargetIndex cellInd, Toil nextToilOnPlaceFailOrIncomplete, bool storageMode)
        {
            Toil toil = new Toil();
            toil.initAction = delegate ()
            {
                Pawn actor = toil.actor;
                Job curJob = actor.jobs.curJob;
                IntVec3 cell = curJob.GetTarget(cellInd).Cell;
                if (actor.carryTracker.CarriedThing == null)
                {
                    Log.Error(actor + " tried to place hauled thing in cell but is not hauling anything.");
                    return;
                }
                SlotGroup slotGroup = actor.Map.haulDestinationManager.SlotGroupAt(cell);
                if (slotGroup != null && slotGroup.Settings.AllowedToAccept(actor.carryTracker.CarriedThing))
                {
                    actor.Map.designationManager.TryRemoveDesignationOn(actor.carryTracker.CarriedThing, DesignationDefOf.Haul);
                }
                Action<Thing, int> placedAction = null;
                if (curJob.def == DissectionDefOf.DoDissectionBill)
                {
                    placedAction = delegate (Thing th, int added)
                    {
                        if (curJob.placedThings == null)
                        {
                            curJob.placedThings = new List<ThingCountClass>();
                        }
                        ThingCountClass thingCountClass = curJob.placedThings.Find((ThingCountClass x) => x.thing == th);
                        if (thingCountClass != null)
                        {
                            thingCountClass.Count += added;
                        }
                        else
                        {
                            curJob.placedThings.Add(new ThingCountClass(th, added));
                        }
                    };
                }
                if (!actor.carryTracker.TryDropCarriedThing(cell, ThingPlaceMode.Direct, out Thing thing, placedAction))
                {
                    if (storageMode)
                    {
                        if (nextToilOnPlaceFailOrIncomplete != null && StoreUtility.TryFindBestBetterStoreCellFor(actor.carryTracker.CarriedThing, actor, actor.Map, StoragePriority.Unstored, actor.Faction, out IntVec3 c, true))
                        {
                            if (actor.CanReserve(c, 1, -1, null, false))
                            {
                                actor.Reserve(c, actor.CurJob, 1, -1, null, true);
                            }
                            actor.CurJob.SetTarget(cellInd, c);
                            actor.jobs.curDriver.JumpToToil(nextToilOnPlaceFailOrIncomplete);
                            return;
                        }
                        Job job = HaulAIUtility.HaulAsideJobFor(actor, actor.carryTracker.CarriedThing);
                        if (job != null)
                        {
                            curJob.targetA = job.targetA;
                            curJob.targetB = job.targetB;
                            curJob.targetC = job.targetC;
                            curJob.count = job.count;
                            curJob.haulOpportunisticDuplicates = job.haulOpportunisticDuplicates;
                            curJob.haulMode = job.haulMode;
                            actor.jobs.curDriver.JumpToToil(nextToilOnPlaceFailOrIncomplete);
                        }
                        else
                        {
                            Log.Error(string.Concat(new object[]
                            {
                                "Incomplete haul for ",
                                actor,
                                ": Could not find anywhere to put ",
                                actor.carryTracker.CarriedThing,
                                " near ",
                                actor.Position,
                                ". Destroying. This should never happen!"
                            }));
                            actor.carryTracker.CarriedThing.Destroy(DestroyMode.Vanish);
                        }
                    }
                    else if (nextToilOnPlaceFailOrIncomplete != null)
                    {
                        actor.jobs.curDriver.JumpToToil(nextToilOnPlaceFailOrIncomplete);
                        return;
                    }
                }
            };
            return toil;
        }

        private static void ApplyThoughts(Pawn worker, Corpse corpse)
        {
            if (worker.needs.mood != null)
            {
                List<ThoughtDef> thoughts = new List<ThoughtDef>();
                if (worker.skills.GetSkill(SkillDefOf.Medicine).passion == Passion.None && !worker.story.traits.HasTrait(TraitDefOf.Cannibal) && !worker.story.traits.HasTrait(TraitDefOf.Psychopath))
                {
                    thoughts.Add(DissectionDefOf.DissectionNoPassion);
                }
                else
                {
                    thoughts.Add(DissectionDefOf.DissectionPassion);
                }

                for (int i = 0; i < thoughts.Count; i++)
                {
                    worker.needs.mood.thoughts.memories.TryGainMemory(thoughts[i], null);
                }
            }
        }

        private static void RecordTale(Pawn worker, Corpse corpse)
        {
            if (worker.IsColonist)
            {
                TaleDef tale;
                if(corpse.InnerPawn.IsColonist)
                {
                    if(worker.skills.GetSkill(SkillDefOf.Medicine).passion == Passion.None)
                    {
                        tale = DissectionDefOf.DissectedColonistWithoutPassionTale;
                    }
                    else
                    {
                        tale = DissectionDefOf.DissectedColonistWithPassionTale;
                    }
                }
                else if((corpse.InnerPawn.Faction != null && corpse.InnerPawn.Faction.PlayerRelationKind == FactionRelationKind.Hostile) ||
                        (corpse.InnerPawn.guilt != null && corpse.InnerPawn.guilt.IsGuilty))
                {
                    if (worker.skills.GetSkill(SkillDefOf.Medicine).passion == Passion.None)
                    {
                        tale = DissectionDefOf.DissectedEnemyWithoutPassionTale;
                    }
                    else
                    {
                        tale = DissectionDefOf.DissectedEnemyWithPassionTale;
                    }
                }
                else
                {
                    if (worker.skills.GetSkill(SkillDefOf.Medicine).passion == Passion.None)
                    {
                        tale = DissectionDefOf.DissectedStrangerWithoutPassionTale;
                    }
                    else
                    {
                        tale = DissectionDefOf.DissectedStrangerWithPassionTale;
                    }
                }
                TaleRecorder.RecordTale(tale, new object[]
                {
                    worker,
                    corpse.InnerPawn
                });
            }
        }

        // Copied from JobDriver_DoBill
        public static Toil JumpToCollectNextIntoHandsForBill(Toil gotoGetTargetToil, TargetIndex ind)
        {
            Toil toil = new Toil();
            toil.initAction = delegate ()
            {
                Pawn actor = toil.actor;
                if (actor.carryTracker.CarriedThing == null)
                {
                    Log.Error("JumpToAlsoCollectTargetInQueue run on " + actor + " who is not carrying something.");
                    return;
                }
                if (actor.carryTracker.Full)
                {
                    return;
                }
                Job curJob = actor.jobs.curJob;
                List<LocalTargetInfo> targetQueue = curJob.GetTargetQueue(ind);
                if (targetQueue.NullOrEmpty())
                {
                    return;
                }
                for (int i = 0; i < targetQueue.Count; i++)
                {
                    if (GenAI.CanUseItemForWork(actor, targetQueue[i].Thing))
                    {
                        if (targetQueue[i].Thing.CanStackWith(actor.carryTracker.CarriedThing))
                        {
                            if ((actor.Position - targetQueue[i].Thing.Position).LengthHorizontalSquared <= 64f)
                            {
                                int num = (actor.carryTracker.CarriedThing != null) ? actor.carryTracker.CarriedThing.stackCount : 0;
                                int num2 = curJob.countQueue[i];
                                num2 = Mathf.Min(num2, targetQueue[i].Thing.def.stackLimit - num);
                                num2 = Mathf.Min(num2, actor.carryTracker.AvailableStackSpace(targetQueue[i].Thing.def));
                                if (num2 > 0)
                                {
                                    curJob.count = num2;
                                    curJob.SetTarget(ind, targetQueue[i].Thing);
                                    List<int> countQueue;
                                    int index;
                                    (countQueue = curJob.countQueue)[index = i] = countQueue[index] - num2;
                                    if (curJob.countQueue[i] <= 0)
                                    {
                                        curJob.countQueue.RemoveAt(i);
                                        targetQueue.RemoveAt(i);
                                    }
                                    actor.jobs.curDriver.JumpToToil(gotoGetTargetToil);
                                    return;
                                }
                            }
                        }
                    }
                }
            };
            return toil;
        }

        /// <summary>
        /// Removes random body parts from the corpse. Has a chance of destroying the body.
        /// </summary>
        /// <param name="actor">The pawn performing the dissection. His skill determines the chance of destroying the body.</param>
        /// <param name="corpse">The corpse being dissected.</param>
        /// <returns>True if the body was destroyed, False otherwise.</returns>
        private static bool RemoveDissectedBodyParts(Pawn actor, Corpse corpse)
        {
            // There is a chance the whole body is destroyed in the process
            bool destroyBody = Dissection.Singleton.AlwaysDetroyBodies;
            if(!destroyBody)
            {
                float skillLevelMultiplier = 1.0f - (float)actor.skills.GetSkill(SkillDefOf.Medicine).Level / SkillRecord.MaxLevel;
                float chance = Dissection.Singleton.DestroyBodyChance * skillLevelMultiplier * skillLevelMultiplier;
                float rand = Rand.Range(0.0f, 1.0f);
                destroyBody = rand <= chance;
            }
            if (destroyBody)
            {
                if (PawnUtility.ShouldSendNotificationAbout(corpse.InnerPawn) && corpse.InnerPawn.RaceProps.Humanlike)
                {
                    Messages.Message(
                        "Dissection_MessageDestroyedByDoctor"
                            .Translate(corpse.InnerPawn.LabelShort, actor.LabelIndefinite())
                            .CapitalizeFirst(),
                        actor, MessageTypeDefOf.NegativeEvent);
                }
                Log.Message($"Destroyed {corpse.InnerPawn.Name}'s body while dissecting.");
                corpse.Destroy();
                return true;
            }


            IEnumerable<BodyPartRecord> source = from x in corpse.InnerPawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined)
                                                 where DissectionUtility.DissectableBodyParts.Contains(x.def)
                                                 select x;
            int numBodyPartsToDissect =
                    (Rand.RangeInclusive(0, DissectionUtility.DissectableBodyParts.Count)
                    + Rand.RangeInclusive(0, DissectionUtility.DissectableBodyParts.Count)
                    + Rand.RangeInclusive(0, DissectionUtility.DissectableBodyParts.Count));
            numBodyPartsToDissect = Mathf.RoundToInt(numBodyPartsToDissect / 3.0f);
            numBodyPartsToDissect = Mathf.Min(source.Count(), numBodyPartsToDissect);

            var bodyParts = source.InRandomOrder().ToArray();

            for (int i = 0; i < numBodyPartsToDissect; ++i)
            {
                Hediff_MissingPart hediff_MissingPart = (Hediff_MissingPart)HediffMaker.MakeHediff(HediffDefOf.MissingBodyPart, corpse.InnerPawn, bodyParts[i]);
                hediff_MissingPart.lastInjury = HediffDefOf.SurgicalCut;
                hediff_MissingPart.IsFresh = true;
                corpse.InnerPawn.health.AddHediff(hediff_MissingPart);
            }

            Hediff dissectedHediff = HediffMaker.MakeHediff(DissectionDefOf.DissectedHediff, corpse.InnerPawn);
            corpse.InnerPawn.health.AddHediff(dissectedHediff);
            return false;
        }
    }
}
