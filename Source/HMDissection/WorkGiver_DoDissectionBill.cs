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
        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            // Don't train medicine when doing so would exceed full rate learning threshold.
            if(!Dissection.Singleton.IgnoreDailyLimit && !forced)
            {
                float xpToday = pawn.skills.GetSkill(SkillDefOf.Medicine).xpSinceMidnight;
                float xpLimit = SkillRecord.MaxFullRateXpPerDay;
                float xpExpected = pawn.skills.GetSkill(SkillDefOf.Medicine).LearnRateFactor() * Dissection.Singleton.ExpPerCorpse;
                if (xpToday + xpExpected > xpLimit)
                {
                    return true;
                }
            }

            return base.ShouldSkip(pawn, forced);
        }

        public static void TryStartNewDoBillJob_Postfix(WorkGiver_DoBill __instance, Pawn pawn, Bill bill, IBillGiver giver, ref Job __result)
        {
            if (__instance is WorkGiver_DoDissectionBill  && __result.def == JobDefOf.DoBill)
            {
                __result.def = DissectionDefOf.DoDissectionBill;
            }
        }
    }
}
