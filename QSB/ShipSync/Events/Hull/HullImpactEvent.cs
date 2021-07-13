using QSB.Events;
using QSB.ShipSync.WorldObjects;
using QSB.WorldSync;

namespace QSB.ShipSync.Events.Hull
{
	internal class HullImpactEvent : QSBEvent<HullImpactMessage>
	{
		public override EventType Type => EventType.HullImpact;

		public override void SetupListener() => GlobalMessenger<ShipHull, ImpactData, float>.AddListener(EventNames.QSBHullImpact, Handler);
		public override void CloseListener() => GlobalMessenger<ShipHull, ImpactData, float>.RemoveListener(EventNames.QSBHullImpact, Handler);

		private void Handler(ShipHull hull, ImpactData data, float damage) => SendEvent(CreateMessage(hull, data, damage));

		private HullImpactMessage CreateMessage(ShipHull hull, ImpactData data, float damage)
		{
			var worldObject = QSBWorldSync.GetWorldFromUnity<QSBShipHull, ShipHull>(hull);
			return new HullImpactMessage
			{
				AboutId = LocalPlayerId,
				ObjectId = worldObject.ObjectId,
				Point = data.point,
				Normal = data.normal,
				Velocity = data.velocity,
				Speed = data.speed,
				Damage = damage
			};
		}

		public override void OnReceiveRemote(bool server, HullImpactMessage message)
		{
			var worldObject = QSBWorldSync.GetWorldFromId<QSBShipHull>(message.ObjectId);
		}
	}
}
