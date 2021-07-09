using QSB.Events;
using QSB.Messaging;
using QSB.Player;

namespace QSB.ProbeSync.Events
{
	class PlayerProbeEvent : QSBEvent<EnumMessage<ProbeEvent>>
	{
		public override EventType Type => EventType.ProbeEvent;

		public override void SetupListener()
			=> GlobalMessenger<ProbeEvent>.AddListener(EventNames.QSBProbeEvent, Handler);

		public override void CloseListener()
			=> GlobalMessenger<ProbeEvent>.RemoveListener(EventNames.QSBProbeEvent, Handler);

		private void Handler(ProbeEvent probeEvent) => SendEvent(CreateMessage(probeEvent));

		private EnumMessage<ProbeEvent> CreateMessage(ProbeEvent probeEvent) => new EnumMessage<ProbeEvent>
		{
			AboutId = LocalPlayerId,
			EnumValue = probeEvent
		};

		public override void OnReceiveRemote(bool server, EnumMessage<ProbeEvent> message)
		{
			var player = QSBPlayerManager.GetPlayer(message.AboutId);
			var probe = player.Probe;

			probe.HandleEvent(message.EnumValue);
		}
	}
}
