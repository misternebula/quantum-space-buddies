using OWML.Utils;
using QSB.Patches;

namespace QSB.DeathSync.Patches
{
	internal class RespawnPatches : QSBPatch
	{
		public override QSBPatchTypes Type => QSBPatchTypes.OnClientConnect;

		public override void DoPatches()
		{
			Prefix(nameof(PlayerRecoveryPoint_OnGainFocus));
			Prefix(nameof(PlayerRecoveryPoint_OnPressInteract));
		}

		public static bool PlayerRecoveryPoint_OnGainFocus(
			PlayerResources ____playerResources,
			bool ____refuelsPlayer,
			bool ____healsPlayer,
			SingleInteractionVolume ____interactVolume
			)
		{
			if (____playerResources == null)
			{
				____playerResources = Locator.GetPlayerTransform().GetComponent<PlayerResources>();
			}

			var isAtFullHealth = ____playerResources.GetHealthFraction() == 1f;
			var isAtFullFuel = ____playerResources.GetFuelFraction() == 1f;
			var canBeRefueled = false;
			var canBeHealed = false;

			if (!isAtFullFuel && ____refuelsPlayer)
			{
				canBeRefueled = true;
			}

			if (!isAtFullHealth && ____healsPlayer)
			{
				canBeHealed = true;
			}

			var showRespawnPrompt = false;

			var uitextType = UITextType.None;
			if (canBeHealed && canBeRefueled)
			{
				// Heal and refuel
				uitextType = UITextType.RefillPrompt_0;

				____interactVolume.SetKeyCommandVisible(true);
			}
			else if (canBeHealed)
			{
				// Heal
				uitextType = UITextType.RefillPrompt_2;

				____interactVolume.SetKeyCommandVisible(true);
			}
			else if (canBeRefueled)
			{
				// Refuel
				uitextType = UITextType.RefillPrompt_4;
				____interactVolume.SetKeyCommandVisible(true);
			}
			else if (RespawnManager.Instance.RespawnNeeded)
			{
				showRespawnPrompt = true;
			}
			else if (____refuelsPlayer && ____healsPlayer)
			{
				// Fuel and health full
				uitextType = UITextType.RefillPrompt_7;
				____interactVolume.SetKeyCommandVisible(false);
			}
			else if (____refuelsPlayer)
			{
				// Fuel full
				uitextType = UITextType.RefillPrompt_8;
				____interactVolume.SetKeyCommandVisible(false);
			}
			else if (____healsPlayer)
			{
				// Health full
				uitextType = UITextType.RefillPrompt_9;
				____interactVolume.SetKeyCommandVisible(false);
			}

			if (showRespawnPrompt)
			{
				____interactVolume.GetValue<ScreenPrompt>("_screenPrompt").SetText($"<CMD> Respawn Player");
				____interactVolume.GetValue<ScreenPrompt>("_noCommandIconPrompt").SetText("Respawn Player");
			}

			if (uitextType != UITextType.None)
			{
				____interactVolume.ChangePrompt(uitextType);
			}

			return false;
		}

		public static bool PlayerRecoveryPoint_OnPressInteract(
			PlayerRecoveryPoint __instance,
			PlayerResources ____playerResources,
			ref bool ____recovering,
			bool ____refuelsPlayer,
			bool ____healsPlayer,
			PlayerAudioController ____playerAudioController,
			SingleInteractionVolume ____interactVolume
			)
		{
			var playerNeedsRefueling = ____playerResources.GetFuelFraction() != 1f;
			var playerNeedsHealing = ____playerResources.GetHealthFraction() != 1f;
			var canBeInteractedWith = false;

			if (playerNeedsRefueling && ____refuelsPlayer)
			{
				canBeInteractedWith = true;
			}

			if (playerNeedsHealing && ____healsPlayer)
			{
				canBeInteractedWith = true;
			}

			if (RespawnManager.Instance.RespawnNeeded)
			{
				canBeInteractedWith = true;
			}

			if (canBeInteractedWith)
			{
				if (RespawnManager.Instance.RespawnNeeded && !playerNeedsRefueling && !playerNeedsHealing)
				{
					RespawnManager.Instance.RespawnSomePlayer();
					return false;
				}

				____playerResources.StartRefillResources(____refuelsPlayer, ____healsPlayer);

				if (____playerAudioController != null)
				{
					if (playerNeedsRefueling && ____refuelsPlayer)
					{
						____playerAudioController.PlayRefuel();
					}

					if (playerNeedsHealing && ____healsPlayer)
					{
						____playerAudioController.PlayMedkit();
					}
				}

				____recovering = true;
				__instance.enabled = true;
			}
			else
			{
				____interactVolume.ResetInteraction();
			}

			return false;
		}
	}
}
