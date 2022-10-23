using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace HMDissection
{
    [DefOf]
    public static class DissectionDefOf
    {
        public static ThoughtDef DissectionNoPassion;
        public static ThoughtDef DissectionPassion;
        public static TaleDef DissectedColonistWithPassionTale;
        public static TaleDef DissectedColonistWithoutPassionTale;
        public static TaleDef DissectedStrangerWithPassionTale;
        public static TaleDef DissectedStrangerWithoutPassionTale;
        public static TaleDef DissectedEnemyWithPassionTale;
        public static TaleDef DissectedEnemyWithoutPassionTale;
        public static RecipeDef DissectHumanRecipe;
        public static HediffDef DissectedHediff;
        public static BodyPartDef Lung;
        public static JobDef DoDissectionBill;
        public static WorkTypeDef MedicalTraining;
    }
}
