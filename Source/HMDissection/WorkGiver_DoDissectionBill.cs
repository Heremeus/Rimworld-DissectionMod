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
            // Don't train medicine after exceeding full rate learning threshold or when maxed.
            if (!ModSettings_Dissection.IgnoreDailyLimit && !forced)
            {
                SkillRecord medicineSkill = pawn.skills.GetSkill(SkillDefOf.Medicine);
                float xpToday = medicineSkill.xpSinceMidnight;
                float xpLimit = SkillRecord.MaxFullRateXpPerDay;
                if (xpToday >= xpLimit)
                {
                    return true;
                }

                float xpExpected = medicineSkill.LearnRateFactor() * ModSettings_Dissection.ExpPerCorpse;
                if (medicineSkill.Level == SkillRecord.MaxLevel && medicineSkill.XpTotalEarned + xpExpected >= medicineSkill.XpRequiredForLevelUp)
                {
                    return true;
                }
            }

            return base.ShouldSkip(pawn, forced);
        }
    }
}
