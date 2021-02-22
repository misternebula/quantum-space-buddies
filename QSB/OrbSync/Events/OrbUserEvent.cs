﻿using OWML.Common;
using QSB.Events;
using QSB.Utility;
using QSB.WorldSync;
using QSB.WorldSync.Events;
using QuantumUNET;
using QuantumUNET.Components;
using System.Linq;

namespace QSB.OrbSync.Events
{
	public class OrbUserEvent : QSBEvent<WorldObjectMessage>
	{
		public override EventType Type => EventType.OrbUser;

		public override void SetupListener() => GlobalMessenger<int>.AddListener(EventNames.QSBOrbUser, Handler);
		public override void CloseListener() => GlobalMessenger<int>.RemoveListener(EventNames.QSBOrbUser, Handler);

		private void Handler(int id) => SendEvent(CreateMessage(id));

		private WorldObjectMessage CreateMessage(int id) => new WorldObjectMessage
		{
			AboutId = LocalPlayerId,
			ObjectId = id
		};

		public override void OnReceiveLocal(bool server, WorldObjectMessage message)
		{
			if (server)
			{
				HandleServer(message);
			}
			else
			{
				HandleClient(message);
			}
		}

		public override void OnReceiveRemote(bool server, WorldObjectMessage message)
		{
			if (server)
			{
				HandleServer(message);
			}
			else
			{
				HandleClient(message);
			}
		}

		private static void HandleServer(WorldObjectMessage message)
		{
			var fromPlayer = QNetworkServer.connections.First(x => x.GetPlayer().PlayerId == message.FromId);
			if (WorldObjectManager.OrbSyncList.Count == 0)
			{
				DebugLog.ToConsole($"Error - OrbSyncList is empty. (ID {message.ObjectId})", MessageType.Error);
				return;
			}
			if (fromPlayer == null)
			{
				DebugLog.ToConsole("Error - FromPlayer is null!", MessageType.Error);
			}
			var orbSync = WorldObjectManager.OrbSyncList
				.First(x => x.AttachedOrb == WorldObjectManager.OldOrbList[message.ObjectId]);
			if (orbSync == null)
			{
				DebugLog.ToConsole($"Error - No orb found for user event. (ID {message.ObjectId})", MessageType.Error);
				return;
			}
			var orbIdentity = orbSync.GetComponent<QNetworkIdentity>();
			if (orbIdentity == null)
			{
				DebugLog.ToConsole($"Error - Orb identity is null. (ID {message.ObjectId})", MessageType.Error);
				return;
			}
			if (orbIdentity.ClientAuthorityOwner != null && orbIdentity.ClientAuthorityOwner != fromPlayer)
			{
				orbIdentity.RemoveClientAuthority(orbIdentity.ClientAuthorityOwner);
			}
			orbIdentity.AssignClientAuthority(fromPlayer);
			orbSync.enabled = true;
		}

		private static void HandleClient(WorldObjectMessage message)
		{
			if (WorldObjectManager.OrbSyncList.Count < message.ObjectId)
			{
				DebugLog.ToConsole(
					$"Error - Orb id {message.ObjectId} out of range of orb sync list {WorldObjectManager.OrbSyncList.Count}.",
					MessageType.Error);
				return;
			}
			if (!WorldObjectManager.OrbSyncList.Any(x => x.AttachedOrb == WorldObjectManager.OldOrbList[message.ObjectId]))
			{
				DebugLog.ToConsole($"Error - No NomaiOrbTransformSync has AttachedOrb with objectId {message.ObjectId}!");
				return;
			}
			var orb = WorldObjectManager.OrbSyncList
				.First(x => x.AttachedOrb == WorldObjectManager.OldOrbList[message.ObjectId]);
			orb.enabled = true;
		}
	}
}