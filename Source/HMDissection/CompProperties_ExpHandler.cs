using Verse;

namespace HMDissection
{
    public class CompProperties_ExpHandler : CompProperties
    {
        public float baseExpPerSecond;

        public CompProperties_ExpHandler()
        {
            compClass = typeof(CompExpHandler);
        }
    }
}
