using Verse;

namespace HMDissection
{
    public class CompProperties_ExpHandler : CompProperties
    {
        public int expAmount;

        public CompProperties_ExpHandler()
        {
            compClass = typeof(CompExpHandler);
        }
    }
}
