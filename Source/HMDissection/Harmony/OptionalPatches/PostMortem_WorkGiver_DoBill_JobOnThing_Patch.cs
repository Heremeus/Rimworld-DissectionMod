using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using Verse.AI;

namespace HMDissection
{
    public class PostMortem_WorkGiver_DoBill_JobOnThing_Patch
    {
        private static Dictionary<Thing, List<Bill>> temporarilySuspendedBills = new Dictionary<Thing, List<Bill>>();
        public static void Enabled(Harmony harmony)
        {
            if (ModsConfig.ActiveModsInLoadOrder.Any(m => m.Name.Contains("Harvest Organs Post Mortem")))
            {
                harmony.Patch(AccessTools.Method(typeof(WorkGiver_DoBill), "JobOnThing"), typeof(WorkGiver_DoDissectionBill).GetMethod("JobOnThing_Prefix"), typeof(WorkGiver_DoDissectionBill).GetMethod("JobOnThing_Postfix"));
            }
        }
        public void JobOnThing_Prefix(WorkGiver_DoBill __instance, ref Pawn pawn, ref Thing thing, ref bool forced)
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

        public void JobOnThing_Postfix(WorkGiver_DoBill __instance, Pawn pawn, Thing thing, bool forced, ref Job __result)
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