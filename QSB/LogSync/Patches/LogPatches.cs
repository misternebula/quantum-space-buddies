﻿using QSB.Events;
using QSB.Patches;

namespace QSB.LogSync.Patches
{
	public class LogPatches : QSBPatch
	{
		public override PatchType Type => PatchType.OnClientConnect;

		public static void RevealFact(string id, bool saveGame, bool showNotification, bool __result)
		{
			if (!__result)
			{
				return;
			}
			EventManager.FireEvent(EventNames.QSBRevealFact, id, saveGame, showNotification);
		}

		public override void DoPatches() => QSBCore.Helper.HarmonyHelper.AddPostfix<ShipLogManager>("RevealFact", typeof(LogPatches), nameof(RevealFact));

		public override void DoUnpatches() => QSBCore.Helper.HarmonyHelper.Unpatch<ShipLogManager>("RevealFact");
	}
}