﻿using OWML.Common;
using QSB.Animation.NPC.WorldObjects;
using QSB.ConversationSync;
using QSB.Events;
using QSB.Patches;
using QSB.Player;
using QSB.Utility;
using QSB.WorldSync;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace QSB.Animation.NPC.Patches
{
	public class CharacterAnimationPatches : QSBPatch
	{
		public override QSBPatchTypes Type => QSBPatchTypes.OnClientConnect;

		public override void DoPatches()
		{
			QSBCore.HarmonyHelper.AddPrefix<CharacterAnimController>("OnAnimatorIK", typeof(CharacterAnimationPatches), nameof(AnimController_OnAnimatorIK));
			QSBCore.HarmonyHelper.AddPrefix<CharacterAnimController>("OnZoneEntry", typeof(CharacterAnimationPatches), nameof(AnimController_OnZoneEntry));
			QSBCore.HarmonyHelper.AddPrefix<CharacterAnimController>("OnZoneExit", typeof(CharacterAnimationPatches), nameof(AnimController_OnZoneExit));
			QSBCore.HarmonyHelper.AddPrefix<FacePlayerWhenTalking>("OnStartConversation", typeof(CharacterAnimationPatches), nameof(FacePlayerWhenTalking_OnStartConversation));
			QSBCore.HarmonyHelper.AddPrefix<CharacterDialogueTree>("StartConversation", typeof(CharacterAnimationPatches), nameof(CharacterDialogueTree_StartConversation));
			QSBCore.HarmonyHelper.AddPrefix<CharacterDialogueTree>("EndConversation", typeof(CharacterAnimationPatches), nameof(CharacterDialogueTree_EndConversation));
			QSBCore.HarmonyHelper.AddPrefix<KidRockController>("Update", typeof(CharacterAnimationPatches), nameof(KidRockController_Update));
		}

		public override void DoUnpatches()
		{
			QSBCore.HarmonyHelper.Unpatch<CharacterAnimController>("OnAnimatorIK");
			QSBCore.HarmonyHelper.Unpatch<CharacterAnimController>("OnZoneEntry");
			QSBCore.HarmonyHelper.Unpatch<CharacterAnimController>("OnZoneExit");
			QSBCore.HarmonyHelper.Unpatch<FacePlayerWhenTalking>("OnStartConversation");
			QSBCore.HarmonyHelper.Unpatch<CharacterDialogueTree>("StartConversation");
			QSBCore.HarmonyHelper.Unpatch<CharacterDialogueTree>("EndConversation");
			QSBCore.HarmonyHelper.Unpatch<KidRockController>("Update");
		}

		public static bool AnimController_OnAnimatorIK(
			CharacterAnimController __instance,
			float ___headTrackingWeight,
			bool ___lookOnlyWhenTalking,
			bool ____playerInHeadZone,
			bool ____inConversation,
			ref float ____currentLookWeight,
			ref Vector3 ____currentLookTarget,
			DampedSpring3D ___lookSpring,
			Animator ____animator,
			CharacterDialogueTree ____dialogueTree)
		{
			if (!WorldObjectManager.AllReady)
			{
				return false;
			}

			var playerId = ConversationManager.Instance.GetPlayerTalkingToTree(____dialogueTree);
			var player = QSBPlayerManager.GetPlayer(playerId);
			var qsbObj = QSBWorldSync.GetWorldFromUnity<QSBCharacterAnimController, CharacterAnimController>(__instance); // TODO : maybe cache this somewhere... or assess how slow this is

			PlayerInfo playerToUse;
			if (____inConversation)
			{
				if (playerId == uint.MaxValue)
				{
					DebugLog.DebugWrite($"Error - {__instance.name} is in conversation with a null player! Defaulting to active camera.", MessageType.Error);
					playerToUse = QSBPlayerManager.LocalPlayer;
				}
				else
				{
					playerToUse = player.CameraBody == null
						? QSBPlayerManager.LocalPlayer
						: player;
				}
			}
			else if (!___lookOnlyWhenTalking && qsbObj.GetPlayersInHeadZone().Count != 0) // TODO : maybe this would be more fun if characters looked between players at random times? :P
			{
				playerToUse = QSBPlayerManager.GetClosestPlayerToWorldPoint(qsbObj.GetPlayersInHeadZone(), __instance.transform.position);
			}
			else
			{
				playerToUse = QSBPlayerManager.GetClosestPlayerToWorldPoint(__instance.transform.position, true);
			}

			var localPosition = ____animator.transform.InverseTransformPoint(playerToUse.CameraBody.transform.position);

			var targetWeight = ___headTrackingWeight;
			if (___lookOnlyWhenTalking)
			{
				if (!____inConversation 
					|| qsbObj.GetPlayersInHeadZone().Count == 0 
					|| !qsbObj.GetPlayersInHeadZone().Contains(playerToUse))
				{
					targetWeight *= 0;
				}
			}
			else
			{
				if (qsbObj.GetPlayersInHeadZone().Count == 0 
					|| !qsbObj.GetPlayersInHeadZone().Contains(playerToUse))
				{
					targetWeight *= 0;
				}
			}

			____currentLookWeight = Mathf.Lerp(____currentLookWeight, targetWeight, Time.deltaTime * 2f);
			____currentLookTarget = ___lookSpring.Update(____currentLookTarget, localPosition, Time.deltaTime);
			____animator.SetLookAtPosition(____animator.transform.TransformPoint(____currentLookTarget));
			____animator.SetLookAtWeight(____currentLookWeight);
			return false;

		}

		public static bool AnimController_OnZoneExit(CharacterAnimController __instance)
		{
			var qsbObj = QSBWorldSync.GetWorldFromUnity<QSBCharacterAnimController, CharacterAnimController>(__instance);
			QSBEventManager.FireEvent(EventNames.QSBExitHeadZone, qsbObj.ObjectId);
			return false;
		}

		public static bool AnimController_OnZoneEntry(CharacterAnimController __instance)
		{
			var qsbObj = QSBWorldSync.GetWorldFromUnity<QSBCharacterAnimController, CharacterAnimController>(__instance);
			QSBEventManager.FireEvent(EventNames.QSBEnterHeadZone, qsbObj.ObjectId);
			return false;
		}

		public static bool FacePlayerWhenTalking_OnStartConversation(
			FacePlayerWhenTalking __instance,
			CharacterDialogueTree ____dialogueTree)
		{
			var playerId = ConversationManager.Instance.GetPlayerTalkingToTree(____dialogueTree);
			if (playerId == uint.MaxValue)
			{
				DebugLog.ToConsole($"Error - No player talking to {____dialogueTree.name}!", MessageType.Error);
				return false;
			}
			var player = QSBPlayerManager.GetPlayer(playerId);

			var distance = player.Body.transform.position - __instance.transform.position;
			var vector2 = distance - Vector3.Project(distance, __instance.transform.up);
			var angle = Vector3.Angle(__instance.transform.forward, vector2) * Mathf.Sign(Vector3.Dot(vector2, __instance.transform.right));
			var axis = __instance.transform.parent.InverseTransformDirection(__instance.transform.up);
			var lhs = Quaternion.AngleAxis(angle, axis);
			__instance.GetType().GetMethod("FaceLocalRotation", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { lhs * __instance.transform.localRotation });

			return false;
		}

		public static bool CharacterDialogueTree_StartConversation(CharacterDialogueTree __instance)
		{
			var allNpcAnimControllers = QSBWorldSync.GetWorldObjects<INpcAnimController>();
			var ownerOfThis = allNpcAnimControllers.FirstOrDefault(x => x.GetDialogueTree() == __instance);
			if (ownerOfThis == default)
			{
				return true;
			}
			var id = QSBWorldSync.GetIdFromTypeSubset(ownerOfThis);
			QSBEventManager.FireEvent(EventNames.QSBNpcAnimEvent, AnimationEvent.StartConversation, id);
			return true;
		}

		public static bool CharacterDialogueTree_EndConversation(CharacterDialogueTree __instance)
		{
			var allNpcAnimControllers = QSBWorldSync.GetWorldObjects<INpcAnimController>();
			var ownerOfThis = allNpcAnimControllers.FirstOrDefault(x => x.GetDialogueTree() == __instance);
			if (ownerOfThis == default)
			{
				return true;
			}
			var id = QSBWorldSync.GetIdFromTypeSubset(ownerOfThis);
			QSBEventManager.FireEvent(EventNames.QSBNpcAnimEvent, AnimationEvent.EndConversation, id);
			return true;
		}

		public static bool KidRockController_Update(
			KidRockController __instance,
			bool ____throwingRock,
			CharacterDialogueTree ____dialogueTree,
			float ____nextThrowTime)
		{
			if (!WorldObjectManager.AllReady)
			{
				return true;
			}
			var qsbObj = QSBWorldSync.GetWorldObjects<QSBCharacterAnimController>().First(x => x.GetDialogueTree() == ____dialogueTree);

			if (!____throwingRock && !qsbObj.InConversation() && Time.time > ____nextThrowTime)
			{
				__instance.GetType().GetMethod("StartRockThrow", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, null);
			}
			return false;
		}
	}
}