using QSB.WorldSync.Events;
using QuantumUNET.Transport;

namespace QSB.ShipSync.Events
{
	internal class RepairTickMessage : WorldObjectMessage
	{
		public float RepairNumber { get; set; }

		public override void Deserialize(QNetworkReader reader)
		{
			base.Deserialize(reader);
			RepairNumber = reader.ReadSingle();
		}

		public override void Serialize(QNetworkWriter writer)
		{
			base.Serialize(writer);
			writer.Write(RepairNumber);
		}
	}
}
