﻿using OWML.Common;
using QSB.Events;
using QSB.Patches;
using QSB.Player;
using QSB.QuantumSync.WorldObjects;
using QSB.Utility;
using QSB.WorldSync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace QSB.QuantumSync.Patches
{
	public class QuantumPatches : QSBPatch
	{
		public override QSB.Patches.PatchType Type => QSB.Patches.PatchType.OnClientConnect;

		public override void DoPatches()
		{
			QSBCore.Helper.HarmonyHelper.AddPrefix<SocketedQuantumObject>("ChangeQuantumState", typeof(QuantumPatches), nameof(Socketed_ChangeQuantumState));
			QSBCore.Helper.HarmonyHelper.AddPostfix<SocketedQuantumObject>("MoveToSocket", typeof(QuantumPatches), nameof(Socketed_MoveToSocket));
			QSBCore.Helper.HarmonyHelper.AddPrefix<QuantumShuffleObject>("ChangeQuantumState", typeof(QuantumPatches), nameof(Shuffle_ChangeQuantumState));
			QSBCore.Helper.HarmonyHelper.AddPrefix<MultiStateQuantumObject>("ChangeQuantumState", typeof(QuantumPatches), nameof(MultiState_ChangeQuantumState));
			QSBCore.Helper.HarmonyHelper.AddPostfix<QuantumState>("SetVisible", typeof(QuantumPatches), nameof(QuantumState_SetVisible));
			QSBCore.Helper.HarmonyHelper.AddPrefix<QuantumShrine>("IsPlayerInDarkness", typeof(QuantumPatches), nameof(Shrine_IsPlayerInDarkness));
			QSBCore.Helper.HarmonyHelper.AddPrefix<QuantumShrine>("ChangeQuantumState", typeof(QuantumPatches), nameof(Shrine_ChangeQuantumState));
			QSBCore.Helper.HarmonyHelper.AddPrefix<QuantumShrine>("OnEntry", typeof(QuantumPatches), nameof(Shrine_OnEntry));
			QSBCore.Helper.HarmonyHelper.AddPrefix<QuantumShrine>("OnExit", typeof(QuantumPatches), nameof(Shrine_OnExit));
			QSBCore.Helper.HarmonyHelper.AddPrefix<QuantumMoon>("CheckPlayerFogProximity", typeof(QuantumPatches), nameof(Moon_CheckPlayerFogProximity));
		}

		public override void DoUnpatches()
		{
			QSBCore.Helper.HarmonyHelper.Unpatch<SocketedQuantumObject>("ChangeQuantumState");
			QSBCore.Helper.HarmonyHelper.Unpatch<SocketedQuantumObject>("MoveToSocket");
			QSBCore.Helper.HarmonyHelper.Unpatch<QuantumShuffleObject>("ChangeQuantumState");
			QSBCore.Helper.HarmonyHelper.Unpatch<MultiStateQuantumObject>("ChangeQuantumState");
			QSBCore.Helper.HarmonyHelper.Unpatch<QuantumState>("SetVisible");
			QSBCore.Helper.HarmonyHelper.Unpatch<QuantumShrine>("IsPlayerInDarkness");
			QSBCore.Helper.HarmonyHelper.Unpatch<QuantumShrine>("ChangeQuantumState");
			QSBCore.Helper.HarmonyHelper.Unpatch<QuantumShrine>("OnEntry");
			QSBCore.Helper.HarmonyHelper.Unpatch<QuantumShrine>("OnExit");
			QSBCore.Helper.HarmonyHelper.Unpatch<QuantumMoon>("CheckPlayerFogProximity");
		}

		public static bool Socketed_ChangeQuantumState(
			SocketedQuantumObject __instance,
			ref bool __result,
			bool skipInstantVisibilityCheck,
			List<QuantumSocket> ____childSockets,
			List<QuantumSocket> ____socketList,
			ref QuantumSocket ____recentlyObscuredSocket,
			QuantumSocket ____occupiedSocket)
		{
			var socketedWorldObject = WorldObjectManager.GetWorldObject<QSBSocketedQuantumObject, SocketedQuantumObject>(__instance);
			if (socketedWorldObject.ControllingPlayer != PlayerManager.LocalPlayerId)
			{
				return false;
			}

			foreach (var socket in ____childSockets)
			{
				if (socket.IsOccupied())
				{
					__result = false;
					return false;
				}
			}

			if (____socketList.Count <= 1)
			{
				DebugLog.ToConsole($"Error - Not enough quantum sockets in list for {__instance.name}!", MessageType.Error);
				__result = false;
				return false;
			}

			var list = new List<QuantumSocket>();
			foreach (var socket in ____socketList)
			{
				if (!socket.IsOccupied() && socket.IsActive())
				{
					list.Add(socket);
				}
			}

			if (list.Count == 0)
			{
				__result = false;
				return false;
			}

			if (____recentlyObscuredSocket != null)
			{
				__instance.GetType().GetMethod("MoveToSocket", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { ____recentlyObscuredSocket });
				____recentlyObscuredSocket = null;
				__result = true;
				return false;
			}

			var occupiedSocket = ____occupiedSocket;
			for (var i = 0; i < 20; i++)
			{
				var index = UnityEngine.Random.Range(0, list.Count);
				__instance.GetType().GetMethod("MoveToSocket", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { list[index] });
				if (skipInstantVisibilityCheck)
				{
					__result = true;
					return false;
				}
				bool socketNotSuitable;
				var isSocketIlluminated = (bool)__instance.GetType().GetMethod("CheckIllumination", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, null);

				var playersEntangled = QuantumManager.GetEntangledPlayers(__instance);
				if (playersEntangled.Count() != 0)
				{
					// socket not suitable if illuminated
					socketNotSuitable = isSocketIlluminated;
				}
				else
				{
					var checkVisInstant = (bool)__instance.GetType().GetMethod("CheckVisibilityInstantly", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, null);
					if (isSocketIlluminated)
					{
						// socket not suitable if object is visible
						socketNotSuitable = checkVisInstant;
					}
					else
					{
						// socket not suitable if player is inside object
						socketNotSuitable = playersEntangled.Any(x => __instance.CheckPointInside(x.CameraBody.transform.position));
					}
				}

				if (!socketNotSuitable)
				{
					__result = true;
					return false;
				}
				list.RemoveAt(index);
				if (list.Count == 0)
				{
					break;
				}
			}
			__instance.GetType().GetMethod("MoveToSocket", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { occupiedSocket });
			__result = false;
			return false;
		}

		public static void Socketed_MoveToSocket(SocketedQuantumObject __instance, QuantumSocket socket)
		{
			var objectWorldObject = WorldObjectManager.GetWorldObject<QSBSocketedQuantumObject, SocketedQuantumObject>(__instance);
			var socketWorldObject = WorldObjectManager.GetWorldObject<QSBQuantumSocket, QuantumSocket>(socket);
			if (objectWorldObject == null)
			{
				DebugLog.ToConsole($"Worldobject is null for {__instance.name}!");
				return;
			}

			if (objectWorldObject.ControllingPlayer != PlayerManager.LocalPlayerId)
			{
				return;
			}

			EventManager.FireEvent(
					EventNames.QSBSocketStateChange,
					objectWorldObject.ObjectId,
					socketWorldObject.ObjectId,
					__instance.transform.localRotation);
		}

		public static bool Shuffle_ChangeQuantumState(
			QuantumShuffleObject __instance,
			ref List<int> ____indexList,
			ref Vector3[] ____localPositions,
			ref Transform[] ____shuffledObjects,
			ref bool __result)
		{
			var shuffleWorldObject = WorldObjectManager.GetWorldObject<QSBQuantumShuffleObject, QuantumShuffleObject>(__instance);
			if (shuffleWorldObject.ControllingPlayer != PlayerManager.LocalPlayerId)
			{
				return false;
			}

			____indexList.Clear();
			____indexList = Enumerable.Range(0, ____localPositions.Length).ToList();
			for (var i = 0; i < ____indexList.Count; ++i)
			{
				var random = UnityEngine.Random.Range(i, ____indexList.Count);
				var temp = ____indexList[i];
				____indexList[i] = ____indexList[random];
				____indexList[random] = temp;
			}
			for (var j = 0; j < ____shuffledObjects.Length; j++)
			{
				____shuffledObjects[j].localPosition = ____localPositions[____indexList[j]];
			}
			//DebugLog.DebugWrite($"{__instance.name} shuffled.");
			EventManager.FireEvent(
					EventNames.QSBQuantumShuffle,
					shuffleWorldObject.ObjectId,
					____indexList.ToArray());
			__result = true;
			return false;
		}

		public static bool MultiState_ChangeQuantumState(MultiStateQuantumObject __instance)
		{
			var qsbObj = WorldObjectManager.GetWorldObject<QSBMultiStateQuantumObject, MultiStateQuantumObject>(__instance);
			var isInControl = qsbObj.ControllingPlayer == PlayerManager.LocalPlayerId;
			return isInControl;
		}

		public static void QuantumState_SetVisible(QuantumState __instance, bool visible)
		{
			if (!visible)
			{
				return;
			}
			var allMultiStates = WorldObjectManager.GetWorldObjects<QSBMultiStateQuantumObject>();
			var owner = allMultiStates.First(x => x.QuantumStates.Contains(__instance));
			//DebugLog.DebugWrite($"{owner.AttachedObject.name} controller is {owner.ControllingPlayer}");
			if (owner.ControllingPlayer != PlayerManager.LocalPlayerId)
			{
				return;
			}
			//DebugLog.DebugWrite($"{owner.AttachedObject.name} to quantum state {Array.IndexOf(owner.QuantumStates, __instance)}");
			EventManager.FireEvent(
					EventNames.QSBMultiStateChange,
					owner.ObjectId,
					Array.IndexOf(owner.QuantumStates, __instance));
		}

		public static bool Shrine_IsPlayerInDarkness(ref bool __result, Light[] ____lamps, float ____fadeFraction, bool ____isProbeInside, NomaiGateway ____gate)
		{
			foreach (var lamp in ____lamps)
			{
				if (lamp.intensity > 0f)
				{
					__result = false;
					return false;
				}
			}

			var playersInMoon = PlayerManager.PlayerList.Where(x => x.IsInMoon);
			if (playersInMoon.Any(x => !x.IsInShrine)
				|| playersInMoon.Any(x => x.FlashLight != null && x.FlashLight.FlashlightOn)
				|| (PlayerManager.LocalPlayer.IsInShrine && PlayerState.IsFlashlightOn())
				|| playersInMoon.Count() == 0)
			{
				__result = false;
				return false;
			}
			// TODO : make this *really* check for all players - check other probes and other jetpacks!
			__result = ____gate.GetOpenFraction() == 0f
				&& !____isProbeInside
				&& Locator.GetThrusterLightTracker().GetLightRange() <= 0f;
			return false;
		}

		public static bool Shrine_ChangeQuantumState(QuantumShrine __instance)
		{
			var shrineWorldObject = WorldObjectManager.GetWorldObject<QSBSocketedQuantumObject, SocketedQuantumObject>(__instance);
			var isInControl = shrineWorldObject.ControllingPlayer == PlayerManager.LocalPlayerId;
			return isInControl;
		}

		public static bool Shrine_OnEntry(
			GameObject hitObj,
			ref bool ____isPlayerInside,
			ref bool ____fading,
			OWLightController ____exteriorLightController,
			ref bool ____isProbeInside)
		{
			if (hitObj.CompareTag("PlayerDetector"))
			{
				____isPlayerInside = true;
				____fading = true;
				____exteriorLightController.FadeTo(0f, 1f);
				EventManager.FireEvent(EventNames.QSBEnterShrine);
			}
			else if (hitObj.CompareTag("ProbeDetector"))
			{
				____isProbeInside = true;
			}
			return false;
		}

		public static bool Shrine_OnExit(
			GameObject hitObj,
			ref bool ____isPlayerInside,
			ref bool ____fading,
			OWLightController ____exteriorLightController,
			ref bool ____isProbeInside)
		{
			if (hitObj.CompareTag("PlayerDetector"))
			{
				____isPlayerInside = false;
				____fading = true;
				____exteriorLightController.FadeTo(1f, 1f);
				EventManager.FireEvent(EventNames.QSBExitShrine);
			}
			else if (hitObj.CompareTag("ProbeDetector"))
			{
				____isProbeInside = false;
			}
			return false;
		}

		public static bool Moon_CheckPlayerFogProximity(
			QuantumMoon __instance,
			int ____stateIndex,
			float ____eyeStateFogOffset,
			ref bool ____isPlayerInside,
			float ____fogRadius,
			float ____fogThickness,
			float ____fogRolloffDistance,
			string ____revealFactID,
			OWRigidbody ____moonBody,
			bool ____hasSunCollapsed,
			Transform ____vortexReturnPivot,
			OWAudioSource ____vortexAudio,
			ref int ____collapseToIndex,
			VisibilityTracker ____visibilityTracker,
			QuantumFogEffectBubbleController ____playerFogBubble,
			QuantumFogEffectBubbleController ____shipLandingCamFogBubble)
		{
			var playerDistance = Vector3.Distance(__instance.transform.position, Locator.GetPlayerCamera().transform.position);
			var fogOffset = (____stateIndex != 5) ? 0f : ____eyeStateFogOffset;
			var distanceFromFog = playerDistance - (____fogRadius + fogOffset);
			var fogAlpha = 0f;
			if (!____isPlayerInside)
			{
				fogAlpha = Mathf.InverseLerp(____fogThickness + ____fogRolloffDistance, ____fogThickness, distanceFromFog);
				if (distanceFromFog < 0f)
				{
					if ((bool)__instance.GetType().GetMethod("IsLockedByProbeSnapshot", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, null) || QuantumManager.IsVisibleUsingCameraFrustum((ShapeVisibilityTracker)____visibilityTracker, true))
					{
						____isPlayerInside = true;
						__instance.GetType().GetMethod("SetSurfaceState", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { ____stateIndex });
						Locator.GetShipLogManager().RevealFact(____revealFactID, true, true);
						EventManager.FireEvent("PlayerEnterQuantumMoon");
					}
					else
					{
						__instance.GetType().GetMethod("Collapse", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { true });
					}
				}
			}
			else if (____isPlayerInside)
			{
				fogAlpha = Mathf.InverseLerp(-____fogThickness - ____fogRolloffDistance, -____fogThickness, distanceFromFog);
				if (distanceFromFog >= 0f)
				{
					if (____stateIndex != 5)
					{
						____isPlayerInside = false;
						if (!(bool)__instance.GetType().GetMethod("IsLockedByProbeSnapshot", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, null) && !QuantumManager.IsVisibleUsingCameraFrustum((ShapeVisibilityTracker)____visibilityTracker, true))
						{
							__instance.GetType().GetMethod("Collapse", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { true });
						}
						__instance.GetType().GetMethod("SetSurfaceState", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { -1 });
						EventManager.FireEvent("PlayerExitQuantumMoon");
					}
					else
					{
						var vector = Locator.GetPlayerTransform().position - __instance.transform.position;
						Locator.GetPlayerBody().SetVelocity(____moonBody.GetPointVelocity(Locator.GetPlayerTransform().position) - (vector.normalized * 5f));
						var d = 80f;
						Locator.GetPlayerBody().SetPosition(__instance.transform.position + (____vortexReturnPivot.up * d));
						if (!Physics.autoSyncTransforms)
						{
							Physics.SyncTransforms();
						}
						var component = Locator.GetPlayerCamera().GetComponent<PlayerCameraController>();
						component.SetDegreesY(component.GetMinDegreesY());
						____vortexAudio.SetLocalVolume(0f);
						____collapseToIndex = 1;
						__instance.GetType().GetMethod("Collapse", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { true });
					}
				}
			}
			____playerFogBubble.SetFogAlpha(fogAlpha);
			____shipLandingCamFogBubble.SetFogAlpha(fogAlpha);
			return false;
		}
	}
}
