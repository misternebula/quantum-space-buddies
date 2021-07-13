using QSB.Events;
using QSB.ShipSync.WorldObjects;
using QSB.WorldSync;

namespace QSB.ShipSync.Events.Hull
{
	internal class HullRepairTickEvent : QSBEvent<RepairTickMessage>
	{
		public override EventType Type => EventType.HullRepairTick;

		public override void SetupListener() => GlobalMessenger<ShipHull, float>.AddListener(EventNames.QSBHullRepairTick, Handler);
		public override void CloseListener() => GlobalMessenger<ShipHull, float>.RemoveListener(EventNames.QSBHullRepairTick, Handler);

		private void Handler(ShipHull hull, float repairFraction) => SendEvent(CreateMessage(hull, repairFraction));

		private RepairTickMessage CreateMessage(ShipHull hull, float repairFraction)
		{
			var worldObject = QSBWorldSync.GetWorldFromUnity<QSBShipHull, ShipHull>(hull);
			return new RepairTickMessage
			{
				AboutId = LocalPlayerId,
				ObjectId = worldObject.ObjectId,
				RepairNumber = repairFraction
			};
		}

		public override void OnReceiveRemote(bool server, RepairTickMessage message)
		{
			var worldObject = QSBWorldSync.GetWorldFromId<QSBShipHull>(message.ObjectId);
			worldObject.RepairTick(message.RepairNumber);
		}
	}
}
