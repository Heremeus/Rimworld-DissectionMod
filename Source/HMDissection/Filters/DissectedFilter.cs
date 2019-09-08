using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace HMDissection.Filters
{
    public abstract class BaseDissectedFilter : SpecialThingFilterWorker
    {
        public override bool Matches(Thing t)
        {
            return DoesMatch(t as Corpse);
        }

        public override bool CanEverMatch(ThingDef def)
        {
            return def.IsWithinCategory(ThingCategoryDefOf.Corpses);
        }

        protected virtual bool DoesMatch(Corpse corpse)
        {
            if (corpse == null)
                return false;

            RaceProperties race = corpse.InnerPawn.RaceProps;
            if (race.Humanlike)
            {
                IEnumerable<BodyPartRecord> source = from x in corpse.InnerPawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined)
                                                     where DissectionUtility.DissectableBodyParts.Contains(x.def)
                                                     select x;
                bool hasDissectableParts = source.Any();
                return (corpse.InnerPawn.health.hediffSet.HasHediff(DissectionDefOf.DissectedHediff) || !hasDissectableParts) == ShouldBeDissected();
            }
            return false;
        }

        public abstract bool ShouldBeDissected();
    }

    public class DissectedFilter : BaseDissectedFilter
    {
        public override bool ShouldBeDissected()
        {
            return true;
        }
    }

    public class NotDissectedFilter : BaseDissectedFilter
    {
        public override bool ShouldBeDissected()
        {
            return false;
        }
    }
}
