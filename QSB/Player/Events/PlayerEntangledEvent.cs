﻿using QSB.Events;
using QSB.QuantumSync;
using QSB.WorldSync.Events;

namespace QSB.Player.Events
{
	internal class PlayerEntangledEvent : QSBEvent<WorldObjectMessage>
	{
		public override EventType Type => EventType.PlayerEntangle;

		public override void SetupListener() => GlobalMessenger<int>.AddListener(EventNames.QSBPlayerEntangle, Handler);
		public override void CloseListener() => GlobalMessenger<int>.RemoveListener(EventNames.QSBPlayerEntangle, Handler);

		private void Handler(int id) => SendEvent(CreateMessage(id));

		private WorldObjectMessage CreateMessage(int id) => new WorldObjectMessage
		{
			AboutId = LocalPlayerId,
			ObjectId = id
		};

		public override void OnReceiveLocal(bool server, WorldObjectMessage message)
		{
			var player = PlayerManager.LocalPlayer;
			if (message.ObjectId == -1)
			{
				player.EntangledObject = null;
				return;
			}
			var quantumObject = QuantumManager.GetObject(message.ObjectId);
			player.EntangledObject = quantumObject;
		}

		public override void OnReceiveRemote(bool server, WorldObjectMessage message)
		{
			var player = PlayerManager.GetPlayer(message.AboutId);
			if (message.ObjectId == -1)
			{
				player.EntangledObject = null;
				return;
			}
			var quantumObject = QuantumManager.GetObject(message.ObjectId);
			player.EntangledObject = quantumObject;
		}
	}
}
