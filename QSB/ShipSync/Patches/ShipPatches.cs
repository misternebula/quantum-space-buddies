using OWML.Utils;
using QSB.Events;
using QSB.Patches;
using QSB.Utility;
using UnityEngine;

namespace QSB.ShipSync.Patches
{
	internal class ShipPatches : QSBPatch
	{
		public override QSBPatchTypes Type => QSBPatchTypes.OnClientConnect;

		public override void DoPatches()
		{
			Prefix(nameof(HatchController_OnPressInteract));
			Prefix(nameof(HatchController_OnEntry));
			Prefix(nameof(ShipTractorBeamSwitch_OnTriggerExit));
			Prefix(nameof(InteractZone_UpdateInteractVolume));
			Prefix(nameof(ShipElectricalComponent_OnEnterShip));
			Prefix(nameof(ShipElectricalComponent_OnExitShip));
			Prefix(nameof(ShipComponent_SetDamaged));
			Prefix(nameof(ShipHull_FixedUpdate));
			Prefix(nameof(ShipDamageController_OnImpact));
			Postfix(nameof(ShipComponent_RepairTick));
			Prefix(nameof(ShipHull_RepairTick));
		}

		public static bool HatchController_OnPressInteract()
		{
			if (!PlayerState.IsInsideShip())
			{
				ShipManager.Instance.ShipTractorBeam.ActivateTractorBeam();
				QSBEventManager.FireEvent(EventNames.QSBEnableFunnel);
			}

			QSBEventManager.FireEvent(EventNames.QSBHatchState, true);
			return true;
		}

		public static bool HatchController_OnEntry(GameObject hitObj)
		{
			if (hitObj.CompareTag("PlayerDetector"))
			{
				QSBEventManager.FireEvent(EventNames.QSBHatchState, false);
			}

			return true;
		}

		public static bool ShipTractorBeamSwitch_OnTriggerExit(Collider hitCollider, bool ____isPlayerInShip, bool ____functional)
		{
			if (!____isPlayerInShip && ____functional && hitCollider.CompareTag("PlayerDetector") && !ShipManager.Instance.HatchController.GetValue<GameObject>("_hatchObject").activeSelf)
			{
				ShipManager.Instance.HatchController.Invoke("CloseHatch");
				ShipManager.Instance.ShipTractorBeam.DeactivateTractorBeam();
				QSBEventManager.FireEvent(EventNames.QSBHatchState, false);
			}

			return false;
		}

		public static bool InteractZone_UpdateInteractVolume(InteractZone __instance, OWCamera ____playerCam, ref bool ____focused)
		{
			/* Angle for interaction with the ship hatch
			 *  
			 *  \  80°  / - If in ship
			 *   \     /
			 *    \   /
			 *   [=====]  - Hatch
			 *    /   \
			 *   /     \
			 *  / 280°  \ - If not in ship
			 *  
			 */

			if (!QSBCore.WorldObjectsReady || __instance != ShipManager.Instance.HatchInteractZone)
			{
				return true;
			}

			var angle = 2f * Vector3.Angle(____playerCam.transform.forward, __instance.transform.forward);

			____focused = PlayerState.IsInsideShip()
				? angle <= 80
				: angle >= 280;

			__instance.CallBase<InteractZone, SingleInteractionVolume>("UpdateInteractVolume");

			return false;
		}

		public static bool ShipElectricalComponent_OnEnterShip(ShipElectricalComponent __instance, bool ____damaged, ElectricalSystem ____electricalSystem)
		{
			__instance.CallBase<ShipElectricalComponent, ShipComponent>("OnEnterShip");

			return false;
		}

		public static bool ShipElectricalComponent_OnExitShip(ShipElectricalComponent __instance, bool ____damaged, ElectricalSystem ____electricalSystem)
		{
			__instance.CallBase<ShipElectricalComponent, ShipComponent>("OnExitShip");

			return false;
		}

		public static bool ShipComponent_SetDamaged(ShipComponent __instance, bool damaged, ref bool ____damaged, ref float ____repairFraction, DamageEffect ____damageEffect)
		{
			if (____damaged == damaged)
			{
				return false;
			}

			if (damaged)
			{
				____damaged = true;
				____repairFraction = 0f;
				__instance.GetType().GetAnyMethod("OnComponentDamaged").Invoke(__instance, null);
				__instance.RaiseEvent("OnDamaged", __instance);
				QSBEventManager.FireEvent(EventNames.QSBComponentDamaged, __instance);
			}
			else
			{
				____damaged = false;
				____repairFraction = 1f;
				__instance.GetType().GetAnyMethod("OnComponentRepaired").Invoke(__instance, null);
				__instance.RaiseEvent("OnRepaired", __instance);
				QSBEventManager.FireEvent(EventNames.QSBComponentRepaired, __instance);
			}

			__instance.GetType().GetAnyMethod("UpdateColliderState").Invoke(__instance, null);
			if (____damageEffect)
			{
				____damageEffect.SetEffectBlend(1f - ____repairFraction);
			}

			return false;
		}

		public static bool ShipHull_FixedUpdate(ShipHull __instance, ref ImpactData ____dominantImpact, ref float ____integrity, ref bool ____damaged, DamageEffect ____damageEffect, ShipComponent[] ____components)
		{
			if (____dominantImpact != null)
			{
				var damage = Mathf.InverseLerp(30f, 200f, ____dominantImpact.speed);
				if (damage > 0f)
				{
					var num2 = 0.15f;
					if (damage < num2 && ____integrity > 1f - num2)
					{
						damage = num2;
					}

					____integrity = Mathf.Max(____integrity - damage, 0f);
					if (!____damaged)
					{
						____damaged = true;
						__instance.RaiseEvent("OnDamaged", __instance);
						QSBEventManager.FireEvent(EventNames.QSBHullDamaged, __instance);
					}

					if (____damageEffect != null)
					{
						____damageEffect.SetEffectBlend(1f - ____integrity);
					}

					QSBEventManager.FireEvent(EventNames.QSBHullChangeIntegrity, __instance, ____integrity);
				}

				foreach (var component in ____components)
				{
					if (!(component == null) && !component.isDamaged)
					{
						if (component.ApplyImpact(____dominantImpact))
						{
							break;
						}
					}
				}

				__instance.RaiseEvent("OnImpact", ____dominantImpact, damage);
				QSBEventManager.FireEvent(EventNames.QSBHullImpact, __instance, ____dominantImpact, damage);

				____dominantImpact = null;
			}

			__instance.enabled = false;
			return false;
		}

		public static bool ShipDamageController_OnImpact()
			=> ShipManager.Instance.HasAuthority;

		public static void ShipComponent_RepairTick(ShipComponent __instance, float ____repairFraction)
		{
			QSBEventManager.FireEvent(EventNames.QSBComponentRepairTick, __instance, ____repairFraction);
			return;
		}

		public static bool ShipHull_RepairTick(ShipHull __instance, ref float ____integrity, ref bool ____damaged, DamageEffect ____damageEffect, float ____repairTime)
		{
			if (!____damaged)
			{
				return false;
			}

			____integrity = Mathf.Min(____integrity + Time.deltaTime / ____repairTime, 1f);
			QSBEventManager.FireEvent(EventNames.QSBHullRepairTick, __instance, ____integrity);

			if (____integrity >= 1f)
			{
				____damaged = false;
				__instance.RaiseEvent("OnRepaired", __instance);
				QSBEventManager.FireEvent(EventNames.QSBHullRepaired, __instance);
			}

			if (____damageEffect != null)
			{
				____damageEffect.SetEffectBlend(1f - ____integrity);
			}

			return false;
		}
	}
}
