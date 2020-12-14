﻿using QSB.Events;
using QSB.Player;
using QSB.Tools;
using QSB.Utility;
using System.Linq;
using UnityEngine;

namespace QSB.TransformSync
{
	public class PlayerCameraSync : TransformSync
	{
		protected void Start()
		{
			var lowestBound = QSBPlayerManager.GetSyncObjects<PlayerTransformSync>()
                .Where(x => x.NetId.Value < NetId.Value).OrderBy(x => x.NetId.Value).Last();
			NetIdentity.SetRootIdentity(lowestBound.NetIdentity);
		}

		protected override Transform InitLocalTransform()
		{
			var body = Locator.GetPlayerCamera().gameObject.transform;

			Player.Camera = body.gameObject;

			Player.IsReady = true;
			GlobalMessenger<bool>.FireEvent(EventNames.QSBPlayerReady, true);
			DebugLog.DebugWrite("PlayerCameraSync init done - Request state!");
			GlobalMessenger.FireEvent(EventNames.QSBPlayerStatesRequest);

			return body;
		}

		protected override Transform InitRemoteTransform()
		{
			var body = new GameObject("RemotePlayerCamera");

			PlayerToolsManager.Init(body.transform);

			Player.Camera = body;

			return body.transform;
		}

		public override bool IsReady => Locator.GetPlayerTransform() != null
			&& Player != null
			&& QSBPlayerManager.PlayerExists(Player.PlayerId)
			&& NetId.Value != uint.MaxValue
			&& NetId.Value != 0U;
	}
}