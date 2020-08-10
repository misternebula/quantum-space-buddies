﻿using OWML.Common;
using QSB.Events;

namespace QSB.Utility
{
    public static class Patches
    {
        private static void ProbeAnchor()
        {
            GlobalMessenger.FireEvent(EventNames.QSBOnProbeAnchor);
        }

        private static bool ProbeWarp(ref bool ____isRetrieving)
        {
            if (!____isRetrieving)
            {
                GlobalMessenger.FireEvent(EventNames.QSBOnProbeWarp);
            }  
            return true;
        }

        public static void DoPatches(IHarmonyHelper helper)
        {
            helper.AddPostfix<SurveyorProbe>("OnAnchor", typeof(Patches), nameof(ProbeAnchor));
            helper.AddPrefix<SurveyorProbe>("Retrieve", typeof(Patches), nameof(ProbeWarp));
        }
    }
}
