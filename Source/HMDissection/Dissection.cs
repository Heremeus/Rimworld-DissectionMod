using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace HMDissection
{
    public class DissectionSettings : ModSettings
    {
        public int ExpPerCorpse = 3000;
        public bool IgnoreDailyLimit = false;
        public float DestroyBodyChance = 0.7f;
        public bool AlwaysDestroyBodies = false;
        public int MaxLevel = SkillRecord.MaxLevel;

        public void ResetToDefaults()
        {
            ExpPerCorpse = 3000;
            IgnoreDailyLimit = false;
            DestroyBodyChance = 0.7f;
            AlwaysDestroyBodies = false;
            MaxLevel = SkillRecord.MaxLevel;
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref ExpPerCorpse, "Dissection_ExpSetting", 3000);
            Scribe_Values.Look(ref IgnoreDailyLimit, "Dissection_DailyLimitSetting", false);
            Scribe_Values.Look(ref DestroyBodyChance, "Dissection_DestroyBodyChanceSetting", 0.7f);
            Scribe_Values.Look(ref AlwaysDestroyBodies, "Dissection_AlwaysDestroyBodiesSetting", false);
            Scribe_Values.Look(ref MaxLevel, "Dissection_MaxLevel", SkillRecord.MaxLevel);
            base.ExposeData();
        }

        public override string ToString()
        {
            return $"DissectionSettings(ExpPerCorpse={ExpPerCorpse}, IgnoreDailyLimit={IgnoreDailyLimit}, DestroyBodyChance={DestroyBodyChance}, AlwaysDestroyBodies={AlwaysDestroyBodies}, MaxLevel={MaxLevel})";
        }
    }

    public class Dissection : Mod
    {
        public static Dissection Singleton { get; private set; }

        private DissectionSettings settings;
        public DissectionSettings Settings => settings;

        /// <summary>
        /// A mandatory constructor which resolves the reference to the settings.
        /// </summary>
        /// <param name="content"></param>
        public Dissection(ModContentPack content) : base(content)
        {
            if(Singleton != null)
            {
                Log.Warning("Singleton was not null!");
            }
            Singleton = this;

            settings = GetSettings<DissectionSettings>();
        }

        /// <summary>
        /// The (optional) GUI part to set the settings.
        /// </summary>
        /// <param name="inRect">A Unity Rect with the size of the settings window.</param>
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);

            // Experience per corpse setting
            string label = "Dissection_ExpSetting_title".Translate() + ": " + settings.ExpPerCorpse;
            settings.ExpPerCorpse = (int)listingStandard.SliderLabeled(label, settings.ExpPerCorpse, 0, 10000, 0.3f, "Dissection_ExpSetting_desc".Translate());
            listingStandard.Gap();
            
            // Ignore daily limit setting
            listingStandard.CheckboxLabeled("Dissection_DailyLimitSetting_title".Translate(), ref settings.IgnoreDailyLimit, "Dissection_DailyLimitSetting_desc".Translate());
            listingStandard.Gap();
            
            // Destroy body chance setting
            label = "Dissection_DestroyBodyChanceSetting_title".Translate() + ": " + (settings.DestroyBodyChance * 100f).ToString("F0") + "%";
            settings.DestroyBodyChance = listingStandard.SliderLabeled(label, settings.DestroyBodyChance, 0f, 1f, 0.3f, "Dissection_DestroyBodyChanceSetting_desc".Translate());
            listingStandard.Gap();
            
            // Always destroy bodies setting
            listingStandard.CheckboxLabeled("Dissection_AlwaysDestroyBodiesSetting_title".Translate(), ref settings.AlwaysDestroyBodies, "Dissection_AlwaysDestroyBodiesSetting_desc".Translate());
            listingStandard.Gap();

            // Max level setting
            label = "Dissection_MaxLevel_title".Translate() + ": " + settings.MaxLevel;
            settings.MaxLevel = (int)listingStandard.SliderLabeled(label, settings.MaxLevel, 0, 99, 0.3f, "Dissection_MaxLevel_desc".Translate());
            listingStandard.Gap();

            // Add separation before reset button
            listingStandard.Gap();
            listingStandard.Gap();
            
            // Reset button
            if (listingStandard.ButtonText("Dissection_Settings_Reset".Translate()))
            {
                settings.ResetToDefaults();
            }
            
            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "Medical Dissection";
        }
    }
}
