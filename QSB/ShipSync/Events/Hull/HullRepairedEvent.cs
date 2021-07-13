using QSB.Events;
using QSB.ShipSync.WorldObjects;
using QSB.WorldSync;
using QSB.WorldSync.Events;

namespace QSB.ShipSync.Events.Hull
{
	internal class HullRepairedEvent : QSBEvent<WorldObjectMessage>
	{
		public override EventType Type => EventType.HullRepaired;

		public override void SetupListener() => GlobalMessenger<ShipHull>.AddListener(EventNames.QSBHullRepaired, Handler);
		public override void CloseListener() => GlobalMessenger<ShipHull>.RemoveListener(EventNames.QSBHullRepaired, Handler);

		private void Handler(ShipHull hull) => SendEvent(CreateMessage(hull));

		private WorldObjectMessage CreateMessage(ShipHull hull)
		{
			var worldObject = QSBWorldSync.GetWorldFromUnity<QSBShipHull, ShipHull>(hull);
			return new WorldObjectMessage
			{
				AboutId = LocalPlayerId,
				ObjectId = worldObject.ObjectId
			};
		}

		public override void OnReceiveRemote(bool server, WorldObjectMessage message)
		{
			var worldObject = QSBWorldSync.GetWorldFromId<QSBShipHull>(message.ObjectId);
			worldObject.SetRepaired();
		}
	}
}
