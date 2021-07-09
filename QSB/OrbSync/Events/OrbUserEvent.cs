using OWML.Common;
using QSB.Events;
using QSB.OrbSync.TransformSync;
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
			var fromPlayer = QNetworkServer.connections.First(x => x.GetPlayerId() == message.FromId);
			if (NomaiOrbTransformSync.OrbTransformSyncs == null || NomaiOrbTransformSync.OrbTransformSyncs.Count == 0)
			{
				DebugLog.ToConsole($"Error - OrbTransformSyncs is empty or null. (ID {message.ObjectId})", MessageType.Error);
				return;
			}

			if (QSBWorldSync.OldOrbList == null || QSBWorldSync.OldOrbList.Count == 0)
			{
				DebugLog.ToConsole($"Error - OldOrbList is empty or null. (ID {message.ObjectId})", MessageType.Error);
				return;
			}

			if (fromPlayer == null)
			{
				DebugLog.ToConsole("Error - FromPlayer is null!", MessageType.Error);
			}

			var orbSync = NomaiOrbTransformSync.OrbTransformSyncs
				.FirstOrDefault(x => x.AttachedObject == QSBWorldSync.OldOrbList[message.ObjectId].transform);
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
			if (NomaiOrbTransformSync.OrbTransformSyncs == null || NomaiOrbTransformSync.OrbTransformSyncs.Count == 0)
			{
				DebugLog.ToConsole($"Error - OrbTransformSyncs is empty or null. (ID {message.ObjectId})", MessageType.Error);
				return;
			}

			if (QSBWorldSync.OldOrbList == null || QSBWorldSync.OldOrbList.Count == 0)
			{
				DebugLog.ToConsole($"Error - OldOrbList is empty or null. (ID {message.ObjectId})", MessageType.Error);
				return;
			}

			if (!NomaiOrbTransformSync.OrbTransformSyncs.Any(x => x.AttachedObject == QSBWorldSync.OldOrbList[message.ObjectId].transform))
			{
				DebugLog.ToConsole($"Error - No NomaiOrbTransformSync has AttachedOrb with objectId {message.ObjectId}!");
				return;
			}

			var orb = NomaiOrbTransformSync.OrbTransformSyncs
				.First(x => x.AttachedObject == QSBWorldSync.OldOrbList[message.ObjectId].transform);
			orb.enabled = true;
		}
	}
}