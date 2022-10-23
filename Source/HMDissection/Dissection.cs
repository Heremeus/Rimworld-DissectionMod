using HugsLib;
using HugsLib.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace HMDissection
{
    public class Dissection : ModBase
    {
        public static Dissection Singleton { get; private set; }
        public override string ModIdentifier => "MedicalDissection";

        private SettingHandle<int> expHandle;
        public int ExpPerCorpse => expHandle.Value;

        private SettingHandle<bool> ignoreDailyLimitHandle;
        public bool IgnoreDailyLimit => ignoreDailyLimitHandle.Value;

        private SettingHandle<float> destroyBodyChanceHandle;
        public float DestroyBodyChance => destroyBodyChanceHandle.Value;

        private SettingHandle<bool> alwaysDetroyBodiesHandle;
        public bool AlwaysDetroyBodies => alwaysDetroyBodiesHandle.Value;

        public Dissection() : base()
        {
            if(Singleton != null)
            {
                Log.Warning("Singleton was not null!");
            }
            Singleton = this;
        }

        public override void Initialize()
        {
            base.Initialize();
            HarmonyPatches.Patch();
        }

        public override void DefsLoaded()
        {
            Log.Message("Medical Dissection v2.8 loaded");
            expHandle = Settings.GetHandle("expPerCorpse", "Dissection_ExpSetting_title".Translate(), "Dissection_ExpSetting_desc".Translate(), 3000, Validators.IntRangeValidator(0, 1000000));
            expHandle.SpinnerIncrement = 100;
            ignoreDailyLimitHandle = Settings.GetHandle("ignoreDailyLimit", "Dissection_DailyLimitSetting_title".Translate(), "Dissection_DailyLimitSetting_desc".Translate(), false);
            destroyBodyChanceHandle = Settings.GetHandle("destroyBodyChance", "Dissection_DestroyBodyChanceSetting_title".Translate(), "Dissection_DestroyBodyChanceSetting_desc".Translate(), 0.70f, PercentSettingIsValid);
            alwaysDetroyBodiesHandle = Settings.GetHandle("alwaysDestroyBodies", "Dissection_AlwaysDestroyBodiesSetting_title".Translate(), "Dissection_AlwaysDestroyBodiesSetting_desc".Translate(), false);
        }

        private static bool PercentSettingIsValid(string value)
        {
            if(float.TryParse(value, out float fValue))
            {
                return fValue >= 0.0f && fValue <= 1.0f;
            }
            return false;
        }
    }
}
