﻿using OWML.Utils;
using QSB.ElevatorSync.WorldObjects;
using QSB.Events;
using QSB.Patches;
using QSB.WorldSync;

namespace QSB.ElevatorSync.Patches
{
	public class ElevatorPatches : QSBPatch
	{
		public override QSBPatchTypes Type => QSBPatchTypes.OnClientConnect;

		public static void StartLift(Elevator __instance)
		{
			var isGoingUp = __instance.GetValue<bool>("_goingToTheEnd");
			var id = QSBWorldSync.GetIdFromUnity<QSBElevator, Elevator>(__instance);
			QSBEventManager.FireEvent(EventNames.QSBStartLift, id, isGoingUp);
		}

		public override void DoPatches() => QSBCore.HarmonyHelper.AddPostfix<Elevator>("StartLift", typeof(ElevatorPatches), nameof(StartLift));

		public override void DoUnpatches() => QSBCore.HarmonyHelper.Unpatch<Elevator>("StartLift");
	}
}