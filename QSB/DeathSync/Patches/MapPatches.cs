using QSB.Patches;
using UnityEngine;

namespace QSB.DeathSync.Patches
{
	internal class MapPatches : QSBPatch
	{
		public override QSBPatchTypes Type => QSBPatchTypes.RespawnTime;

		public override void DoPatches()
		{
			Prefix(nameof(MapController_LateUpdate));
			Prefix(nameof(MapController_EnterMapView));
		}

		public static bool MapController_EnterMapView(
			MapController __instance,
			ref bool ____isMapMode,
			OWAudioSource ____audioSource,
			MapMarkerManager ____mapMarkerManager,
			OWCamera ____mapCamera,
			OWCamera ____activeCam,
			MeshRenderer ____gridRenderer,
			ref Transform ____targetTransform,
			ref bool ____lockedToTargetTransform,
			ref Vector3 ____position,
			ref float ____yaw,
			ref float ____pitch,
			ref float ____zoom,
			ref float ____targetZoom,
			ref bool ____interpPosition,
			ref bool ____interpPitch,
			ref bool ____interpZoom,
			ref bool ____framingPlayer,
			ref float ____lockTimer,
			float ____defaultYawAngle,
			float ____initialPitchAngle,
			float ____initialZoomDist,
			float ____defaultZoomDist,
			float ____lockOnMoveLength,
			ref float ____gridOverrideSize,
			ref bool ____gridOverride,
			ref float ____gridTimer,
			ref float ____revealLength,
			ReferenceFrame ____currentRFrame,
			float ____gridLockOnLength,
			ref float ____revealTimer
			)
		{
			if (____isMapMode)
			{
				return false;
			}

			____mapMarkerManager.SetVisible(true);
			GlobalMessenger.FireEvent("EnterMapView");
			GlobalMessenger<OWCamera>.FireEvent("SwitchActiveCamera", ____mapCamera);
			if (____audioSource.isPlaying)
			{
				____audioSource.Stop();
				____audioSource.SetLocalVolume(1f);
				____audioSource.Play();
			}
			else
			{
				____audioSource.SetLocalVolume(1f);
				____audioSource.Play();
			}

			Locator.GetAudioMixer().MixMap();
			____activeCam.enabled = false;
			____mapCamera.enabled = true;
			____gridRenderer.enabled = false;
			____targetTransform = null;
			____lockedToTargetTransform = false;
			____position = RespawnOnDeath.Instance.DeathPositionWorld - Locator.GetCenterOfTheUniverse().GetStaticReferenceFrame().GetPosition();
			____position.y = 0f;
			____yaw = ____defaultYawAngle;
			____pitch = ____initialPitchAngle;
			____zoom = ____initialZoomDist;
			____targetZoom = ____defaultZoomDist;
			__instance.transform.rotation = Quaternion.LookRotation(-RespawnOnDeath.Instance.DeathPlayerUpVector, RespawnOnDeath.Instance.DeathPlayerForwardVector);
			__instance.transform.position = RespawnOnDeath.Instance.DeathPositionWorld;
			____interpPosition = true;
			____interpPitch = true;
			____interpZoom = true;
			____framingPlayer = ____lockedToTargetTransform;
			____lockTimer = ____lockOnMoveLength;
			____gridOverrideSize = (____currentRFrame == null) ? 0f : ____currentRFrame.GetAutopilotArrivalDistance();
			____gridOverride = ____gridOverrideSize > 0f;
			____gridTimer = (!____gridOverride) ? 0f : ____gridLockOnLength;
			____revealLength = 20f;
			____revealTimer = 0f;
			____isMapMode = true;
			return false;
		}

		public static bool MapController_LateUpdate(
			MapController __instance,
			ref float ____observatoryRevealTwist,
			ref float ____defaultPitchAngle,
			ref float ____initialPitchAngle,
			OWCamera ____mapCamera,
			ref float ____lockTimer,
			ref float ____revealTimer,
			float ____lockOnMoveLength,
			float ____revealLength,
			ref bool ____screenPromptsVisible,
			bool ____isPaused,
			ScreenPrompt ____closePrompt,
			ScreenPrompt ____panPrompt,
			ScreenPrompt ____rotatePrompt,
			ScreenPrompt ____zoomPrompt,
			ref bool ____lockedToTargetTransform,
			ref bool ____interpPosition,
			ref bool ____interpPitch,
			ref bool ____interpZoom,
			OWCamera ____activeCam,
			ref Vector3 ____position,
			float ____panSpeed,
			ref float ____zoom,
			float ____maxPanDistance,
			float ____yawSpeed,
			ref float ____yaw,
			float ____pitchSpeed,
			ref float ____pitch,
			float ____minPitchAngle,
			float ____maxPitchAngle,
			ref float ____targetZoom,
			float ____minZoomDistance,
			float ____maxZoomDistance,
			float ____initialZoomDist,
			float ____zoomSpeed,
			float ____observatoryRevealDist
			)
		{
			____lockTimer = Mathf.Min(____lockTimer + Time.deltaTime, ____lockOnMoveLength);
			____revealTimer = Mathf.Min(____revealTimer + Time.deltaTime, ____revealLength);

			var revealFraction = Mathf.Clamp01(____revealTimer / ____revealLength);
			var smoothedRevealFraction = Mathf.SmoothStep(0f, 1f, revealFraction);

			var canInteractWith = ____revealTimer > 18f;

			if (____screenPromptsVisible && ____isPaused)
			{
				____closePrompt.SetVisibility(false);
				____panPrompt.SetVisibility(false);
				____rotatePrompt.SetVisibility(false);
				____zoomPrompt.SetVisibility(false);
				____screenPromptsVisible = false;
			}
			else if (!____screenPromptsVisible && canInteractWith && !____isPaused)
			{
				____closePrompt.SetVisibility(false);
				____panPrompt.SetVisibility(true);
				____rotatePrompt.SetVisibility(true);
				____zoomPrompt.SetVisibility(true);
				____screenPromptsVisible = true;
			}

			var XZinput = Vector2.zero;
			var lookInput = Vector2.zero;
			var zoomInput = 0f;
			if (canInteractWith)
			{
				XZinput = OWInput.GetValue(InputLibrary.moveXZ, InputMode.All);
				lookInput = InputLibrary.look.GetValue(false);
				zoomInput = OWInput.GetValue(InputLibrary.mapZoom, InputMode.All);
				lookInput.y *= -1f;
				zoomInput *= -1f;
			}

			____lockedToTargetTransform &= XZinput.sqrMagnitude < 0.01f;
			____interpPosition &= XZinput.sqrMagnitude < 0.01f;
			____interpPitch &= Mathf.Abs(lookInput.y) < 0.1f;
			____interpZoom &= Mathf.Abs(zoomInput) < 0.1f;

			if (____interpPosition)
			{
				var a = ____activeCam.transform.position - Locator.GetCenterOfTheUniverse().GetOffsetPosition();
				var b = Vector3.zero;
				____position = Vector3.Lerp(a, b, smoothedRevealFraction);
			}
			else
			{
				var normalized = Vector3.Scale(__instance.transform.forward + __instance.transform.up, new Vector3(1f, 0f, 1f)).normalized;
				var a2 = (__instance.transform.right * XZinput.x) + (normalized * XZinput.y);
				____position += a2 * ____panSpeed * ____zoom * Time.deltaTime;
				____position.y = 0f;
				if (____position.sqrMagnitude > ____maxPanDistance * ____maxPanDistance)
				{
					____position = ____position.normalized * ____maxPanDistance;
				}
			}

			____yaw += lookInput.x * ____yawSpeed * Time.deltaTime;
			____yaw = OWMath.WrapAngle(____yaw);
			if (____interpPitch)
			{
				____pitch = Mathf.Lerp(____initialPitchAngle, ____defaultPitchAngle, smoothedRevealFraction);
			}
			else
			{
				____pitch += lookInput.y * ____pitchSpeed * Time.deltaTime;
				____pitch = Mathf.Clamp(____pitch, ____minPitchAngle, ____maxPitchAngle);
			}

			if (____interpZoom)
			{
				____zoom = Mathf.Lerp(____initialZoomDist, ____targetZoom, smoothedRevealFraction);
			}
			else
			{
				____zoom += zoomInput * ____zoomSpeed * Time.deltaTime;
				____zoom = Mathf.Clamp(____zoom, ____minZoomDistance, ____maxZoomDistance);
			}

			____mapCamera.nearClipPlane = Mathf.Lerp(0.1f, 1f, smoothedRevealFraction);

			var finalRotation = Quaternion.Euler(____pitch, ____yaw, 0f);

			var num4 = revealFraction * (2f - revealFraction);

			var num5 = Mathf.SmoothStep(0f, 1f, num4);

			// Create rotation that's looking down at the player from above
			var lookingDownAtPlayer = Quaternion.LookRotation(-RespawnOnDeath.Instance.DeathPlayerUpVector, Vector3.up);

			// Get starting position - distance above player
			var startingPosition = RespawnOnDeath.Instance.DeathPositionWorld;
			startingPosition += RespawnOnDeath.Instance.DeathPlayerUpVector * num5 * ____observatoryRevealDist;

			// Lerp to final rotation
			__instance.transform.rotation = Quaternion.Lerp(lookingDownAtPlayer, finalRotation, num5);

			// Lerp reveal twist
			__instance.transform.rotation *= Quaternion.AngleAxis(Mathf.Lerp(____observatoryRevealTwist, 0f, num4), Vector3.forward);

			var endPosition = ____position + (-__instance.transform.forward * ____zoom) + Locator.GetCenterOfTheUniverse().GetStaticReferenceFrame().GetPosition();

			// Lerp to final position
			__instance.transform.position = Vector3.Lerp(startingPosition, endPosition, num5);

			return false;
		}
	}
}
