using RimWorld;
using System.Collections.Generic;
using Verse;

namespace HMDissection
{
    public class CompExpHandler : ThingComp
    {
        public CompProperties_ExpHandler Props
        {
            get => (CompProperties_ExpHandler)props;
        }

        public override void CompTick()
        {
            base.CompTick();
            
            if (!parent.Spawned)
            {
                return;
            }
            if (parent.TryGetComp<CompPowerTrader>() != null && !parent.TryGetComp<CompPowerTrader>().PowerOn)
            {
                return;
            }
            
            List<Thing> affectedThings = (parent.InteractionCell.GetThingList(parent.Map));
            
            foreach (Thing t in affectedThings)
            {
                Pawn p = t as Pawn;
                if(p != null)
                {
                    
                }
            }
        }
    }
}
