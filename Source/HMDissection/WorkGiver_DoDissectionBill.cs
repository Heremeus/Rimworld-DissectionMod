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
        private const int XP_LIMIT_TOLERANCE = 500;

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            
            // Don't train medicine when doing so would exceed full rate learning threshold.
			// Only applies after gaining a minimum of 1000xp (otherwise pawns that have a high learning multiplier would never dissect).
            if(!Dissection.Singleton.IgnoreDailyLimit && !forced)
            {
                SkillRecord medicineSkill = pawn.skills.GetSkill(SkillDefOf.Medicine);
                float xpToday = medicineSkill.xpSinceMidnight;
                float xpLimit = SkillRecord.MaxFullRateXpPerDay;
                float xpExpected = medicineSkill.LearnRateFactor() * Dissection.Singleton.ExpPerCorpse;
                if (xpToday > 1000 && xpToday + xpExpected > xpLimit + XP_LIMIT_TOLERANCE)
                {
                    return true;
                }
                
                // Do not train medicine when maxed out
                if (medicineSkill.Level == SkillRecord.MaxLevel && medicineSkill.XpTotalEarned + xpExpected >= medicineSkill.XpRequiredForLevelUp)
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
