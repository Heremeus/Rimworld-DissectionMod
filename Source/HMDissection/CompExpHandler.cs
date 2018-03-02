using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace HMDissection
{
    public class CompExpHandler : ThingComp
    {
        private const float O_TICKS_PER_SECOND = 1f/60f;

        private float timeSinceLastDamage = 0f;

        public CompProperties_ExpHandler Props
        {
            get
            {
                return (CompProperties_ExpHandler)props;
            }
        }

        //Class="HMDissection.CompProperties_ExpHandler"><compClass>HMDissection.CompExpHandler</compClass><baseExpPerSecond>0.1</baseExpPerSecond></li> doesn't correspond to any field in type ThingDef.

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
            
            Pawn interactingPawn = parent.InteractionCell.GetThingList(parent.Map)
                .Select(thing => thing as Pawn)
                .FirstOrDefault(pawn => pawn != null && pawn.CurJob?.targetA == parent);
            if(interactingPawn != null)
            {
                // Determine the amount of exp
                float exp =  Props.baseExpPerSecond * O_TICKS_PER_SECOND;
                interactingPawn.skills.GetSkill(interactingPawn.CurJob.RecipeDef.workSkill).Learn(exp, false);
            }


            // TODO: Split this into another comp
            if (timeSinceLastDamage > 1.5f)
            {
                // Damage corpse
                Building_WorkTable table = parent as Building_WorkTable;
                Corpse corpse = parent.Position.GetThingList(parent.Map).FirstOrDefault(thing => thing is Corpse) as Corpse;
                if (corpse != null)
                {
                    DamageDef dDef = new DamageDef();
                    DamageInfo dInfo = new DamageInfo(dDef, 1);
                    //corpse.TakeDamage(dInfo);
                    corpse.Ingested(interactingPawn, 0.1f);
                }
                timeSinceLastDamage = 0f;
            }
            timeSinceLastDamage += O_TICKS_PER_SECOND;
        }
    }
}
