using System.Collections.Generic;
using UnityEngine;
using Verse;
using HarmonyLib;

namespace HMDissection
{
    public class Mod_Dissection : Mod
    {

        public static ModSettings_Dissection settings;
        public Mod_Dissection(ModContentPack content) : base(content)
        {
            settings = GetSettings<ModSettings_Dissection>();
            Harmony harmony = new (this.Content.PackageIdPlayerFacing);
            //PostMortem_WorkGiver_DoBill_JobOnThing_Patch.Enabled(harmony);
            harmony.PatchAll();

        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            settings.DoSettingsWindowContents(inRect);
        }
        public override string SettingsCategory()
        {
            return "Medical Dissection";
        }
        public override void WriteSettings()
        {
            base.WriteSettings();
        }
        
    }
}