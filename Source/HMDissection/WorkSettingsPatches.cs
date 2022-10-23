using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HMDissection
{
    public static class WorkSettingsPatches
    {

        public static void EnableAndInitialize_Postfix(Pawn ___pawn)
        {
            SetDissectionPriorityForPassionatedPawns(___pawn);
        }

        public static void PrepForMapGen_Postfix(List<Pawn> ___startingAndOptionalPawns)
        {
            // Set priority for medical training if the pawn has a passion for medicine
            // This way the pawn will practice medicine by default, even if it has 0 medicine skill
            foreach (Pawn pawn in ___startingAndOptionalPawns)
            {
                SetDissectionPriorityForPassionatedPawns(pawn);
            }
        }

        private static void SetDissectionPriorityForPassionatedPawns(Pawn pawn)
        {
            // Set priority for medical training if the pawn has a passion for medicine
            // This way the pawn will practice medicine by default, even if it has 0 medicine skill
            Passion? passion = pawn?.skills?.GetSkill(SkillDefOf.Medicine)?.passion;
            if (passion.HasValue && passion.Value != Passion.None)
            {
                pawn.workSettings?.SetPriority(DissectionDefOf.MedicalTraining, 3);
            }
        }
    }
}
