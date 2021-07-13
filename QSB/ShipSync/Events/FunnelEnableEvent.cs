using QSB.Events;
using QSB.Messaging;

namespace QSB.ShipSync.Events
{
	internal class FunnelEnableEvent : QSBEvent<PlayerMessage>
	{
		public override EventType Type => EventType.EnableFunnel;

		public override void SetupListener()
			=> GlobalMessenger.AddListener(EventNames.QSBEnableFunnel, Handler);

		public override void CloseListener()
			=> GlobalMessenger.RemoveListener(EventNames.QSBEnableFunnel, Handler);

		private void Handler() => SendEvent(CreateMessage());

		private PlayerMessage CreateMessage() => new PlayerMessage
		{
			AboutId = LocalPlayerId
		};

		public override void OnReceiveRemote(bool server, PlayerMessage message)
			=> ShipManager.Instance.ShipTractorBeam.ActivateTractorBeam();
	}
}
