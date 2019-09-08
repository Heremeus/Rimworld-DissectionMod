using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;

namespace HMDissection
{
    class JobDriver_DoDissectionBill : JobDriver_DoBill
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            var result = base.TryMakePreToilReservations(errorOnFailed);
            return result;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            AddEndCondition(delegate
            {
                Thing thing = GetActor().jobs.curJob.GetTarget(TargetIndex.A).Thing;
                if (thing is Building && !thing.Spawned)
                {
                    return JobCondition.Incompletable;
                }
                return JobCondition.Ongoing;
            });

            this.FailOnBurningImmobile(TargetIndex.A);

            this.FailOn(IsDeletedOrNotUsableForBills);

            // Copied from JobDriver_DoBill (removed a few things that were not needed for dissecton)
            Toil gotoBillGiver = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            yield return Toils_Jump.JumpIf(gotoBillGiver, () => job.GetTargetQueue(TargetIndex.B).NullOrEmpty());
            Toil extract = Toils_JobTransforms.ExtractNextTargetFromQueue(TargetIndex.B, true);
            yield return extract;
            Toil getToHaulTarget = Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.B).FailOnSomeonePhysicallyInteracting(TargetIndex.B);
            yield return getToHaulTarget;
            yield return Toils_Haul.StartCarryThing(TargetIndex.B, true, false, true);
            yield return Toils_Dissection.JumpToCollectNextIntoHandsForBill(getToHaulTarget, TargetIndex.B);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell).FailOnDestroyedOrNull(TargetIndex.B);
            Toil findPlaceTarget = Toils_JobTransforms.SetTargetToIngredientPlaceCell(TargetIndex.A, TargetIndex.B, TargetIndex.C);
            yield return findPlaceTarget;
            yield return Toils_Dissection.PlaceHauledThingInCell(TargetIndex.C, findPlaceTarget, false);
            yield return Toils_Jump.JumpIfHaveTargetInQueue(TargetIndex.B, extract);
            yield return gotoBillGiver;

            // Strip body
            Toil doDissectionRecipeWork = Toils_Dissection.DoDissectionRecipeWork().FailOnDespawnedNullOrForbiddenPlacedThings().FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
            yield return Toils_Jump.JumpIf(doDissectionRecipeWork, () =>
                {
                    LocalTargetInfo targetInfo = job.GetTarget(TargetIndex.B);
                    if (targetInfo.HasThing)
                    {
                        return !(targetInfo.Thing is Corpse corpse) || !corpse.AnythingToStrip();
                    }
                    return false;
                });
            yield return Toils_General.Wait(60).WithProgressBarToilDelay(TargetIndex.B, false, -0.5f);
            yield return Toils_Dissection.StripCorpse(TargetIndex.B);

            // Copied from JobDriver_DoBill
            yield return doDissectionRecipeWork;
            yield return Toils_Dissection.FinishRecipeAndStartStoringCorpse(TargetIndex.B);

            yield return Toils_Reserve.Reserve(TargetIndex.C);

            // Go to storage cell
            yield return Toils_Goto.GotoCell(TargetIndex.C, PathEndMode.ClosestTouch);
            // Drop corpse
            yield return Toils_Dissection.PlaceHauledThingInCell(TargetIndex.C, null, false);
            
            Toil recount = new Toil();
            recount.initAction = delegate ()
            {
                if (recount.actor.jobs.curJob.bill is Bill_Production bill_Production && bill_Production.repeatMode == BillRepeatModeDefOf.TargetCount)
                {
                    Map.resourceCounter.UpdateResourceCounts();
                }
            };
            yield return recount;

            yield break;
        }

        private bool IsDeletedOrNotUsableForBills()
        {
            if (job.GetTarget(TargetIndex.A).Thing is IBillGiver billGiver)
            {
                if (job.bill.DeletedOrDereferenced)
                {
                    return true;
                }
                if (!billGiver.CurrentlyUsableForBills())
                {
                    return true;
                }
            }
            return false;
        }
    }
}
