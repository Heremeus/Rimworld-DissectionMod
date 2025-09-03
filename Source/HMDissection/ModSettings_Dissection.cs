using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace HMDissection
{
    public class ModSettings_Dissection : ModSettings
    {
        public override void ExposeData()
        {
            Scribe_Values.Look<float>(ref DestroyBodyChance, "DestroyBodyChance", 0.7f);
            Scribe_Values.Look<int>(ref ExpPerCorpse, "ExpPerCorpse", 3000);
            Scribe_Values.Look<bool>(ref IgnoreDailyLimit, "IgnoreDailyLimit", false);
            Scribe_Values.Look<bool>(ref AlwaysDetroyBodies, "AlwaysDetroyBodies", false);
            base.ExposeData();
        }
        private Vector2 scrollPosition;
        public void DoSettingsWindowContents(Rect inRect)
        {
            Rect rect = new Rect(inRect.x, inRect.y, inRect.width - 20f, inRect.height);
            rect.height = 300f;
            Widgets.BeginScrollView(inRect, ref this.scrollPosition, rect, true);
            Listing_Dissection options = new Listing_Dissection();
            options.Begin(rect);
            options.GapLine();
            Text.Font = GameFont.Medium;
            Text.Font = GameFont.Small;
            options.Gap();
            DestroyBodyChance = options.CustomSliderLabel("Dissection_DestroyBodyChanceSetting_title".Translate(), DestroyBodyChance, 0f, 1f, 0.5f, "Dissection_DestroyBodyChanceSetting_desc".Translate(), (DestroyBodyChance * 100).ToString("F2") + "%", 1f.ToString(), 0f.ToString(), 0.01f);
            options.Gap();
            ExpPerCorpse = options.CustomSliderLabelInt("Dissection_ExpSetting_title".Translate(), ExpPerCorpse, 0, 100000, 0.5f, "Dissection_ExpSetting_desc".Translate(), ExpPerCorpse.ToString(), 100000.ToString(), 0.ToString(), 1000);
            options.Gap();
            options.CustomCheckboxLabeled("Dissection_DailyLimitSetting_title".Translate(), ref IgnoreDailyLimit, "Dissection_DailyLimitSetting_desc".Translate());
            options.CustomCheckboxLabeled("Dissection_AlwaysDestroyBodiesSetting_title".Translate(), ref AlwaysDetroyBodies, "Dissection_AlwaysDestroyBodiesSetting_desc".Translate());
            options.GapLine();
            options.Gap();
            if (options.ButtonText("Reset to Defaults"))
            {
                ResetSettingsToDefault();
            }
            options.End();
            Widgets.EndScrollView();
        }
        private void ResetSettingsToDefault()
        {
            DestroyBodyChance = 0.7f;
            ExpPerCorpse = 3000;
            IgnoreDailyLimit = false;
            AlwaysDetroyBodies = false;
        }

        public static float
            DestroyBodyChance = 0.7f;

        public static int
           ExpPerCorpse = 3000;

        public static bool
            IgnoreDailyLimit = false,
            AlwaysDetroyBodies = false;
    }
}