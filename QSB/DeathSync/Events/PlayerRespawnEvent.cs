using QSB.Events;
using QSB.Messaging;
using QSB.Player;

namespace QSB.DeathSync.Events
{
	internal class PlayerRespawnEvent : QSBEvent<PlayerMessage>
	{
		public override EventType Type => EventType.PlayerRespawn;

		public override void SetupListener()
			=> GlobalMessenger<uint>.AddListener(EventNames.QSBPlayerRespawn, Handler);

		public override void CloseListener()
			=> GlobalMessenger<uint>.RemoveListener(EventNames.QSBPlayerRespawn, Handler);

		private void Handler(uint playerId) => SendEvent(CreateMessage(playerId));

		private PlayerMessage CreateMessage(uint playerId) => new PlayerMessage
		{
			AboutId = playerId
		};

		public override void OnReceiveLocal(bool server, PlayerMessage message)
			=> OnReceiveRemote(server, message);

		public override void OnReceiveRemote(bool server, PlayerMessage message)
		{
			if (message.AboutId == LocalPlayerId)
			{
				RespawnManager.Instance.Respawn();
			}

			RespawnManager.Instance.OnPlayerRespawn(QSBPlayerManager.GetPlayer(message.AboutId));
		}
	}
}
