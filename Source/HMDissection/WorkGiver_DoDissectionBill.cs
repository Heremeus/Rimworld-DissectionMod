using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;

namespace HMDissection
{
    public class WorkGiver_DoDissectionBill : WorkGiver_DoBill
    {
        private static Dictionary<Thing, List<Bill>> temporarilySuspendedBills = new Dictionary<Thing, List<Bill>>();

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            // Don't train medicine after exceeding full rate learning threshold or when maxed.
            if (!Dissection.Singleton.IgnoreDailyLimit && !forced)
            {
                SkillRecord medicineSkill = pawn.skills.GetSkill(SkillDefOf.Medicine);
                float xpToday = medicineSkill.xpSinceMidnight;
                float xpLimit = SkillRecord.MaxFullRateXpPerDay;
                if (xpToday >= xpLimit)
                {
                    return true;
                }

                float xpExpected = medicineSkill.LearnRateFactor() * Dissection.Singleton.ExpPerCorpse;
                if (medicineSkill.Level == SkillRecord.MaxLevel && medicineSkill.XpTotalEarned + xpExpected >= medicineSkill.XpRequiredForLevelUp)
                {
                    return true;
                }
            }

            return base.ShouldSkip(pawn, forced);
        }
        
        public static void TryStartNewDoBillJob_Postfix(Pawn pawn, Bill bill, IBillGiver giver, List<ThingCount> chosenIngThings, Job haulOffJob, bool dontCreateJobIfHaulOffRequired, ref Job __result)
        {
            if (__result.def == JobDefOf.DoBill && bill.recipe == DissectionDefOf.DissectHumanRecipe)
            {
                __result.def = DissectionDefOf.DoDissectionBill;
            }
        }

        public static void JobOnThing_Prefix(WorkGiver_DoBill __instance, ref Pawn pawn, ref Thing thing, ref bool forced)
        {
            // Autopsy table has two WorkGiver (WorkGiver_DoBill and WorkGiver_DoDissectionBill).
            // We need to temporarily suspend bills for this function or otherwise the wrong WorkGiver tries to execute them

            // Suspend all non-dissection bills when checking for jobs as WorkGiver_DoDissectionBill
            if (__instance is WorkGiver_DoDissectionBill && thing.def == CompatibilityUtility.AutopsyTableDef)
            {
                if (thing is IBillGiver billGiver)
                {
                    foreach (Bill bill in billGiver.BillStack.Bills)
                    {
                        if (!bill.suspended && bill.recipe != DissectionDefOf.DissectHumanRecipe)
                        {
                            if (!temporarilySuspendedBills.ContainsKey(thing))
                            {
                                temporarilySuspendedBills.Add(thing, new List<Bill>());
                            }
                            temporarilySuspendedBills[thing].Add(bill);
                            bill.suspended = true;
                        }
                    }
                }
            }
            // Suspend all dissection bills when checking for jobs as WorkGiver_DoBill
            if (!(__instance is WorkGiver_DoDissectionBill) && thing.def == CompatibilityUtility.AutopsyTableDef)
            {
                if (thing is IBillGiver billGiver)
                {
                    foreach (Bill bill in billGiver.BillStack.Bills)
                    {
                        if (!bill.suspended && bill.recipe == DissectionDefOf.DissectHumanRecipe)
                        {
                            if (!temporarilySuspendedBills.ContainsKey(thing))
                            {
                                temporarilySuspendedBills.Add(thing, new List<Bill>());
                            }
                            temporarilySuspendedBills[thing].Add(bill);
                            bill.suspended = true;
                        }
                    }
                }
            }
        }

        public static void JobOnThing_Postfix(WorkGiver_DoBill __instance, Pawn pawn, Thing thing, bool forced, ref Job __result)
        {
            // Re-enable all non-dissection bills when checking for jobs as WorkGiver_DoDissectionBill
            if (__instance is WorkGiver_DoDissectionBill && thing.def == CompatibilityUtility.AutopsyTableDef && temporarilySuspendedBills.ContainsKey(thing))
            {
                if (thing is IBillGiver billGiver)
                {
                    foreach (Bill bill in billGiver.BillStack.Bills)
                    {
                        if (bill.recipe != DissectionDefOf.DissectHumanRecipe && temporarilySuspendedBills[thing].Contains(bill))
                        {
                            bill.suspended = false;
                        }
                    }
                }
                temporarilySuspendedBills.Remove(thing);
            }
            // Re-enable all dissection bills when checking for jobs as WorkGiver_DoBill
            if (!(__instance is WorkGiver_DoDissectionBill) && thing.def == CompatibilityUtility.AutopsyTableDef && temporarilySuspendedBills.ContainsKey(thing))
            {
                if (thing is IBillGiver billGiver)
                {
                    foreach (Bill bill in billGiver.BillStack.Bills)
                    {
                        if (bill.recipe == DissectionDefOf.DissectHumanRecipe && temporarilySuspendedBills[thing].Contains(bill))
                        {
                            bill.suspended = false;
                        }
                    }
                }
                temporarilySuspendedBills.Remove(thing);
            }
        }
    }
}
