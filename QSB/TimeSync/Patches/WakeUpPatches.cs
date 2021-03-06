﻿using OWML.Utils;
using QSB.Patches;

namespace QSB.TimeSync.Patches
{
	public class WakeUpPatches : QSBPatch
	{
		public override QSBPatchTypes Type => QSBPatchTypes.OnNonServerClientConnect;

		public static bool OnStartOfTimeLoopPrefix(ref PlayerCameraEffectController __instance)
		{
			if (__instance.gameObject.CompareTag("MainCamera") && QSBSceneManager.CurrentScene != OWScene.EyeOfTheUniverse)
			{
				__instance.Invoke("WakeUp");
			}
			return false;
		}

		public override void DoPatches() => QSBCore.HarmonyHelper.AddPrefix<PlayerCameraEffectController>("OnStartOfTimeLoop", typeof(WakeUpPatches), nameof(OnStartOfTimeLoopPrefix));

		public override void DoUnpatches() => QSBCore.HarmonyHelper.Unpatch<PlayerCameraEffectController>("OnStartOfTimeLoop");
	}
}