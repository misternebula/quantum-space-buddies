using OWML.Common;
using QSB.Events;
using QSB.Utility;

namespace QSB.Player.Events
{
	public class PlayerJoinEvent : QSBEvent<PlayerJoinMessage>
	{
		public override EventType Type => EventType.PlayerJoin;

		public override void SetupListener() => GlobalMessenger<string>.AddListener(EventNames.QSBPlayerJoin, Handler);
		public override void CloseListener() => GlobalMessenger<string>.RemoveListener(EventNames.QSBPlayerJoin, Handler);

		private void Handler(string name) => SendEvent(CreateMessage(name));

		private PlayerJoinMessage CreateMessage(string name) => new PlayerJoinMessage
		{
			AboutId = LocalPlayerId,
			PlayerName = name,
			QSBVersion = QSBCore.QSBVersion
		};

		public override void OnReceiveRemote(bool server, PlayerJoinMessage message)
		{
			if (server && (message.QSBVersion != QSBCore.QSBVersion))
			{
				DebugLog.ToConsole($"Error - Client {message.PlayerName} connecting with wrong version. (Client:{message.QSBVersion}, Server:{QSBCore.QSBVersion})", MessageType.Error);
				QSBEventManager.FireEvent(EventNames.QSBPlayerKick, message.AboutId, KickReason.VersionNotMatching);
				return;
			}

			var player = QSBPlayerManager.GetPlayer(message.AboutId);
			player.Name = message.PlayerName;
			DebugLog.ToAll($"{player.Name} joined!", MessageType.Info);
			DebugLog.DebugWrite($"{player.Name} joined as id {player.PlayerId}", MessageType.Info);
		}

		public override void OnReceiveLocal(bool server, PlayerJoinMessage message)
		{
			var player = QSBPlayerManager.GetPlayer(QSBPlayerManager.LocalPlayerId);
			player.Name = message.PlayerName;
			var text = $"Connected to server as {player.Name}.";
			DebugLog.ToAll(text, MessageType.Info);

			if (QSBSceneManager.IsInUniverse)
			{
				QSBPlayerManager.LocalPlayer.PlayerStates.IsReady = true;
				QSBEventManager.FireEvent(EventNames.QSBPlayerReady, true);
			}
		}
	}
}