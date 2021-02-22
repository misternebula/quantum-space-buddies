﻿using QSB.Events;
using QSB.Patches;
using QSB.WorldSync;
using UnityEngine;

namespace QSB.OrbSync.Patches
{
	public class OrbPatches : QSBPatch
	{
		public override PatchType Type => PatchType.OnClientConnect;

		public static void StartDragCallEvent(bool __result, NomaiInterfaceOrb __instance)
		{
			if (__result)
			{
				EventManager.FireEvent(EventNames.QSBOrbUser, WorldObjectManager.OldOrbList.FindIndex(x => x == __instance));
			}
		}

		public static bool CheckOrbCollision(ref bool __result, NomaiInterfaceSlot __instance, NomaiInterfaceOrb orb,
			bool ____ignoreDraggedOrbs, float ____radius, float ____exitRadius, ref NomaiInterfaceOrb ____occupyingOrb)
		{
			if (____ignoreDraggedOrbs && orb.IsBeingDragged())
			{
				__result = false;
				return false;
			}
			var orbDistance = Vector3.Distance(orb.transform.position, __instance.transform.position);
			var triggerRadius = orb.IsBeingDragged() ? ____exitRadius : ____radius;
			if (____occupyingOrb == null && orbDistance < ____radius)
			{
				____occupyingOrb = orb;
				if (Time.timeSinceLevelLoad > 1f)
				{
					WorldObjectManager.HandleSlotStateChange(__instance, orb, true);
					WorldObjectManager.RaiseEvent(__instance, "OnSlotActivated");
				}
				__result = true;
				return false;
			}
			if (____occupyingOrb == null || ____occupyingOrb != orb)
			{
				__result = false;
				return false;
			}
			if (orbDistance > triggerRadius)
			{
				WorldObjectManager.HandleSlotStateChange(__instance, orb, false);
				____occupyingOrb = null;
				WorldObjectManager.RaiseEvent(__instance, "OnSlotDeactivated");
				__result = false;
				return false;
			}
			__result = true;
			return false;
		}

		public override void DoPatches()
		{
			QSBCore.Helper.HarmonyHelper.AddPostfix<NomaiInterfaceOrb>("StartDragFromPosition", typeof(OrbPatches), nameof(StartDragCallEvent));
			QSBCore.Helper.HarmonyHelper.AddPrefix<NomaiInterfaceSlot>("CheckOrbCollision", typeof(OrbPatches), nameof(CheckOrbCollision));
		}

		public override void DoUnpatches()
		{
			QSBCore.Helper.HarmonyHelper.Unpatch<NomaiInterfaceOrb>("StartDragFromPosition");
			QSBCore.Helper.HarmonyHelper.Unpatch<NomaiInterfaceSlot>("CheckOrbCollision");
		}
	}
}