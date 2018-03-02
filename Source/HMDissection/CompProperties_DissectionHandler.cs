using Verse;

namespace HMDissection
{
    public class CompProperties_DissectionHandler : CompProperties
    {
        public float baseExpPerSecond;
        public float nutritionDissectedPerSecond;

        public CompProperties_DissectionHandler()
        {
            compClass = typeof(CompProperties_DissectionHandler);
        }
    }
}
