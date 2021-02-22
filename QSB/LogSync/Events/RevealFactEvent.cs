﻿using QSB.Events;
using QSB.WorldSync;

namespace QSB.LogSync.Events
{
	public class RevealFactEvent : QSBEvent<RevealFactMessage>
	{
		public override EventType Type => EventType.RevealFact;

		public override void SetupListener() => GlobalMessenger<string, bool, bool>.AddListener(EventNames.QSBRevealFact, Handler);
		public override void CloseListener() => GlobalMessenger<string, bool, bool>.RemoveListener(EventNames.QSBRevealFact, Handler);

		private void Handler(string id, bool saveGame, bool showNotification) => SendEvent(CreateMessage(id, saveGame, showNotification));

		private RevealFactMessage CreateMessage(string id, bool saveGame, bool showNotification) => new RevealFactMessage
		{
			AboutId = LocalPlayerId,
			FactId = id,
			SaveGame = saveGame,
			ShowNotification = showNotification
		};

		public override void OnReceiveLocal(bool server, RevealFactMessage message)
		{
			if (server)
			{
				WorldObjectManager.AddFactReveal(message.FactId, message.SaveGame, message.ShowNotification);
			}
		}

		public override void OnReceiveRemote(bool server, RevealFactMessage message)
		{
			if (server)
			{
				WorldObjectManager.AddFactReveal(message.FactId, message.SaveGame, message.ShowNotification);
			}
			if (!QSBCore.HasWokenUp)
			{
				return;
			}
			Locator.GetShipLogManager().RevealFact(message.FactId, message.SaveGame, message.ShowNotification);
		}
	}
}
