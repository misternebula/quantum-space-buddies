﻿using OWML.Common;
using QSB.Player.Tools;
using QSB.TransformSync;
using QSB.Utility;
using UnityEngine;

namespace QSB.Player.TransformSyncs
{
	public class PlayerProbeSync : SyncedTransform
	{
		private Transform _disabledSocket;

		private Transform GetProbe() =>
			Locator.GetProbe().transform.Find("CameraPivot").Find("Geometry");

		protected override Transform InitLocalTransform()
		{
			var body = GetProbe();

			SetSocket(Player.CameraBody.transform);
			Player.ProbeBody = body.gameObject;

			return body;
		}

		protected override Transform InitRemoteTransform()
		{
			var probe = GetProbe();

			if (probe == null)
			{
				DebugLog.ToConsole("Error - Probe is null!", MessageType.Error);
				return default;
			}

			var body = probe.InstantiateInactive();
			body.name = "RemoteProbeTransform";

			Destroy(body.GetComponentInChildren<ProbeAnimatorController>());

			PlayerToolsManager.CreateProbe(body, Player);

			QSBCore.Helper.Events.Unity.RunWhen(
				() => Player.ProbeLauncher != null,
				() => SetSocket(Player.ProbeLauncher.ToolGameObject.transform));
			Player.ProbeBody = body.gameObject;

			return body;
		}

		private void SetSocket(Transform socket) => _disabledSocket = socket;

		protected override void UpdateTransform()
		{
			base.UpdateTransform();
			if (Player == null)
			{
				DebugLog.ToConsole($"Player is null for {AttachedNetId}!", MessageType.Error);
				return;
			}
			if (_disabledSocket == null)
			{
				DebugLog.ToConsole($"DisabledSocket is null for {AttachedNetId}! (ProbeLauncher null? : {Player.ProbeLauncher == null})", MessageType.Error);
				return;
			}
			if (Player.GetState(State.ProbeActive) || ReferenceSector?.AttachedObject == null)
			{
				return;
			}
			if (HasAuthority)
			{
				transform.position = ReferenceSector.Transform.InverseTransformPoint(_disabledSocket.position);
				return;
			}
			if (TransformToSync.position == Vector3.zero)
			{
				return;
			}
			TransformToSync.localPosition = ReferenceSector.Transform.InverseTransformPoint(_disabledSocket.position);
		}

		public override bool IsReady => Locator.GetProbe() != null
			&& Player != null
			&& PlayerManager.PlayerExists(Player.PlayerId)
			&& Player.IsReady
			&& NetId.Value != uint.MaxValue
			&& NetId.Value != 0U;
	}
}