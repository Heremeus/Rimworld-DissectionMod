using Verse;

namespace HMDissection
{
    [StaticConstructorOnStartup]
    public static class CompatibilityUtility
    {
        public static readonly ThingDef AutopsyTableDef = DefDatabase<ThingDef>.GetNamed("TableAutopsy", false);
    }
}
