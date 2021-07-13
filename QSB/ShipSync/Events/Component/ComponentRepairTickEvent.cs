using QSB.Events;
using QSB.ShipSync.WorldObjects;
using QSB.WorldSync;

namespace QSB.ShipSync.Events.Component
{
	internal class ComponentRepairTickEvent : QSBEvent<RepairTickMessage>
	{
		public override EventType Type => EventType.ComponentRepairTick;

		public override void SetupListener() => GlobalMessenger<ShipComponent, float>.AddListener(EventNames.QSBComponentRepairTick, Handler);
		public override void CloseListener() => GlobalMessenger<ShipComponent, float>.RemoveListener(EventNames.QSBComponentRepairTick, Handler);

		private void Handler(ShipComponent hull, float repairFraction) => SendEvent(CreateMessage(hull, repairFraction));

		private RepairTickMessage CreateMessage(ShipComponent hull, float repairFraction)
		{
			var worldObject = QSBWorldSync.GetWorldFromUnity<QSBShipComponent, ShipComponent>(hull);
			return new RepairTickMessage
			{
				AboutId = LocalPlayerId,
				ObjectId = worldObject.ObjectId,
				RepairNumber = repairFraction
			};
		}

		public override void OnReceiveRemote(bool server, RepairTickMessage message)
		{
			var worldObject = QSBWorldSync.GetWorldFromId<QSBShipComponent>(message.ObjectId);
			worldObject.RepairTick(message.RepairNumber);
		}
	}
}
