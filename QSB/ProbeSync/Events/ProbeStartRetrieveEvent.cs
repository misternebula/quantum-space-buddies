using QSB.Events;
using QSB.Messaging;
using QSB.Player;

namespace QSB.ProbeSync.Events
{
	class ProbeStartRetrieveEvent : QSBEvent<FloatMessage>
	{
		public override EventType Type => EventType.ProbeStartRetrieve;

		public override void SetupListener()
			=> GlobalMessenger<float>.AddListener(EventNames.QSBProbeStartRetrieve, Handler);

		public override void CloseListener()
			=> GlobalMessenger<float>.RemoveListener(EventNames.QSBProbeStartRetrieve, Handler);

		private void Handler(float duration) => SendEvent(CreateMessage(duration));

		private FloatMessage CreateMessage(float duration) => new FloatMessage
		{
			AboutId = LocalPlayerId,
			Value = duration
		};

		public override void OnReceiveRemote(bool server, FloatMessage message)
		{
			var player = QSBPlayerManager.GetPlayer(message.AboutId);
			var probe = player.Probe;
			probe.OnStartRetrieve(message.Value);
		}
	}
}
