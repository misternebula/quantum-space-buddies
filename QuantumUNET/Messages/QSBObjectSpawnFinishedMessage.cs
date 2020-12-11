﻿namespace QuantumUNET.Messages
{
	internal class QSBObjectSpawnFinishedMessage : QSBMessageBase
	{
		public uint State;

		public override void Deserialize(QSBNetworkReader reader)
		{
			State = reader.ReadPackedUInt32();
		}

		public override void Serialize(QSBNetworkWriter writer)
		{
			writer.WritePackedUInt32(State);
		}
	}
}