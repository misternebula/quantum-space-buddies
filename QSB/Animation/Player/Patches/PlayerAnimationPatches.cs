using QSB.Events;
using QSB.Patches;
using QSB.Player;
using QSB.Utility;
using UnityEngine;

namespace QSB.Animation.Patches
{
	internal class PlayerAnimationPatches : QSBPatch
	{
		public override QSBPatchTypes Type => QSBPatchTypes.OnClientConnect;

		public override void DoPatches()
			=> Prefix(nameof(PlayerAnimController_LateUpdate));

		public static bool PlayerAnimController_LateUpdate(
			PlayerAnimController __instance,
			PlayerCharacterController ____playerController,
			ThrusterModel ____playerJetpack,
			ref float ____ungroundedTime,
			Animator ____animator,
			ref bool ____justBecameGrounded,
			ref bool ____justTookFallDamage,
			ref bool ____leftFootGrounded,
			ref bool ____rightFootGrounded,
			ref bool ____rightArmHidden,
			GameObject[] ____rightArmObjects,
			int ____defaultLayer,
			int ____probeOnlyLayer)
		{
			var isGrounded = ____playerController.IsGrounded();
			var isAttached = PlayerState.IsAttached();
			var isInZeroG = PlayerState.InZeroG();
			var isFlying = ____playerJetpack.GetLocalAcceleration().y > 0f;
			var movementVector = Vector3.zero;
			if (!isAttached)
			{
				movementVector = ____playerController.GetRelativeGroundVelocity();
			}

			if (Mathf.Abs(movementVector.x) < 0.05f)
			{
				movementVector.x = 0f;
			}

			if (Mathf.Abs(movementVector.z) < 0.05f)
			{
				movementVector.z = 0f;
			}

			if (isFlying)
			{
				____ungroundedTime = Time.time;
			}

			var freefallMagnitude = 0f;
			var timeInFreefall = 0f;
			var lastGroundBody = ____playerController.GetLastGroundBody();
			if (!isGrounded && !isAttached && !isInZeroG && lastGroundBody != null)
			{
				freefallMagnitude = (____playerController.GetAttachedOWRigidbody(false).GetVelocity() - lastGroundBody.GetPointVelocity(____playerController.transform.position)).magnitude;
				timeInFreefall = Time.time - ____ungroundedTime;
			}

			____animator.SetFloat("RunSpeedX", movementVector.x / 3f);
			____animator.SetFloat("RunSpeedY", movementVector.z / 3f);
			____animator.SetFloat("TurnSpeed", ____playerController.GetTurning());
			____animator.SetBool("Grounded", isGrounded || isAttached || PlayerState.IsRecentlyDetached());
			____animator.SetLayerWeight(1, ____playerController.GetJumpChargeFraction());
			____animator.SetFloat("FreefallSpeed", freefallMagnitude / 15f * (timeInFreefall / 3f));
			____animator.SetBool("InZeroG", isInZeroG || isFlying);
			____animator.SetBool("UsingJetpack", isInZeroG && PlayerState.IsWearingSuit());
			if (____justBecameGrounded)
			{
				var playerAnimationSync = QSBPlayerManager.LocalPlayer.AnimationSync;
				if (____justTookFallDamage)
				{
					____animator.SetTrigger("LandHard");
					QSBEventManager.FireEvent(EventNames.QSBAnimTrigger, playerAnimationSync.AttachedNetId, "LandHard");
				}
				else
				{
					____animator.SetTrigger("Land");
					QSBEventManager.FireEvent(EventNames.QSBAnimTrigger, playerAnimationSync.AttachedNetId, "Land");
				}
			}

			if (isGrounded)
			{
				var leftFootLift = ____animator.GetFloat("LeftFootLift");
				if (!____leftFootGrounded && leftFootLift < 0.333f)
				{
					____leftFootGrounded = true;
					__instance.RaiseEvent("OnLeftFootGrounded");
				}
				else if (____leftFootGrounded && leftFootLift > 0.666f)
				{
					____leftFootGrounded = false;
					__instance.RaiseEvent("OnLeftFootLift");
				}

				var rightFootLift = ____animator.GetFloat("RightFootLift");
				if (!____rightFootGrounded && rightFootLift < 0.333f)
				{
					____rightFootGrounded = true;
					__instance.RaiseEvent("OnRightFootGrounded");
				}
				else if (____rightFootGrounded && rightFootLift > 0.666f)
				{
					____rightFootGrounded = false;
					__instance.RaiseEvent("OnRightFootLift");
				}
			}

			____justBecameGrounded = false;
			____justTookFallDamage = false;
			var usingTool = Locator.GetToolModeSwapper().GetToolMode() != ToolMode.None;
			if ((usingTool && !____rightArmHidden) || (!usingTool && ____rightArmHidden))
			{
				____rightArmHidden = usingTool;
				for (var i = 0; i < ____rightArmObjects.Length; i++)
				{
					____rightArmObjects[i].layer = (!____rightArmHidden) ? ____defaultLayer : ____probeOnlyLayer;
				}
			}

			return false;
		}
	}
}
