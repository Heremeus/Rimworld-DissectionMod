using Verse;

namespace HMDissection
{
    public class CompProperties_DissectionHandler : CompProperties
    {
        //public float nutritionDissectedPerSecond = 0.01f;

        //public float baseExpPerSecond = 0.0f;

        public float baseDiseasesBonusPercent = 0.25f;
        public int diseaseStackLimit = 3;
        public float stackedDiseaseMultiplier = 0.75f;

        public float baseDamageMalusPercent = 0.05f;
        public int damageStackLimit = 10;
        public float stackedDamageMultiplier = 1.0f;

        public CompProperties_DissectionHandler()
        {
            compClass = typeof(CompProperties_DissectionHandler);
        }
    }
}
