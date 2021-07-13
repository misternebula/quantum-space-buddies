using QSB.Events;
using QSB.ShipSync.WorldObjects;
using QSB.WorldSync;

namespace QSB.ShipSync.Events.Hull
{
	internal class HullChangeIntegrityEvent : QSBEvent<HullChangeIntegrityMessage>
	{
		public override EventType Type => EventType.HullChangeIntegrity;

		public override void SetupListener() => GlobalMessenger<ShipHull, float>.AddListener(EventNames.QSBHullChangeIntegrity, Handler);
		public override void CloseListener() => GlobalMessenger<ShipHull, float>.RemoveListener(EventNames.QSBHullChangeIntegrity, Handler);

		private void Handler(ShipHull hull, float integrity) => SendEvent(CreateMessage(hull, integrity));

		private HullChangeIntegrityMessage CreateMessage(ShipHull hull, float integrity)
		{
			var worldObject = QSBWorldSync.GetWorldFromUnity<QSBShipHull, ShipHull>(hull);
			return new HullChangeIntegrityMessage
			{
				AboutId = LocalPlayerId,
				ObjectId = worldObject.ObjectId,
				Integrity = integrity
			};
		}

		public override void OnReceiveRemote(bool server, HullChangeIntegrityMessage message)
		{
			var worldObject = QSBWorldSync.GetWorldFromId<QSBShipHull>(message.ObjectId);
			worldObject.ChangeIntegrity(message.Integrity);
		}
	}
}
