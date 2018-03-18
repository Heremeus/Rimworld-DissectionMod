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
        private const float IG_HOURS_TO_REAL_SEONDS = 41f; // Converts one ingame hour to 41 real time seconds

        public static Dissection Singleton { get; private set; }
        public override string ModIdentifier => "MedicalDissection";

        private SettingHandle<int> expHandle;
        public int ExpPerCorpse => expHandle.Value;

        private SettingHandle<float> timeHandle;
        public float SecondsPerCorpse => timeHandle.Value * IG_HOURS_TO_REAL_SEONDS;

        private SettingHandle<bool> ignoreDailyLimitHandle;
        public bool IgnoreDailyLimit => ignoreDailyLimitHandle.Value;

        public Dissection() : base()
        {
            if(Singleton != null)
            {
                Log.Warning("Singleton was not null!");
            }
            Singleton = this;
        }

        public override void DefsLoaded()
        {
            Log.Message("Medical Dissection loaded");
            expHandle = Settings.GetHandle<int>("expPerCorpse", "Dissection_ExpSetting_title".Translate(), "Dissection_ExpSetting_desc".Translate(), 1500, Validators.IntRangeValidator(0, 1000000));
            expHandle.SpinnerIncrement = 100;
            timeHandle = Settings.GetHandle<float>("timePerCorpse", "Dissection_DurationSetting_title".Translate(), "Dissection_DurationSetting_desc".Translate(), 2f, Validators.FloatRangeValidator(0f, 1000000f));
            ignoreDailyLimitHandle = Settings.GetHandle<bool>("ignoreDailyLimit", "Dissection_DailyLimitSetting_title".Translate(), "Dissection_DailyLimitSetting_desc".Translate(), false);
        }
    }
}
