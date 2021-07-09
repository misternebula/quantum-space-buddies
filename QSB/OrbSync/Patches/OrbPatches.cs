using QSB.Events;
using QSB.Patches;
using QSB.Utility;
using QSB.WorldSync;
using UnityEngine;

namespace QSB.OrbSync.Patches
{
	public class OrbPatches : QSBPatch
	{
		public override QSBPatchTypes Type => QSBPatchTypes.OnClientConnect;

		public static void NomaiInterfaceOrb_StartDragFromPosition(bool __result, NomaiInterfaceOrb __instance)
		{
			if (__result)
			{
				QSBEventManager.FireEvent(EventNames.QSBOrbUser, QSBWorldSync.OldOrbList.FindIndex(x => x == __instance));
			}
		}

		public static bool NomaiInterfaceSlot_CheckOrbCollision(ref bool __result, NomaiInterfaceSlot __instance, NomaiInterfaceOrb orb,
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
					QSBWorldSync.HandleSlotStateChange(__instance, orb, true);
					__instance.RaiseEvent("OnSlotActivated", __instance);
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
				QSBWorldSync.HandleSlotStateChange(__instance, orb, false);
				____occupyingOrb = null;
				__instance.RaiseEvent("OnSlotDeactivated", __instance);
				__result = false;
				return false;
			}

			__result = true;
			return false;
		}

		public override void DoPatches()
		{
			Postfix(nameof(NomaiInterfaceOrb_StartDragFromPosition));
			Prefix(nameof(NomaiInterfaceSlot_CheckOrbCollision));
		}
	}
}