﻿using QSB.Messaging;
using QuantumUNET.Transport;

namespace QSB.WorldSync.Events
{
	public class WorldObjectMessage : PlayerMessage
	{
		public int ObjectId { get; set; }

		public override void Deserialize(QNetworkReader reader)
		{
			base.Deserialize(reader);
			ObjectId = reader.ReadInt32();
		}

		public override void Serialize(QNetworkWriter writer)
		{
			base.Serialize(writer);
			writer.Write(ObjectId);
		}
	}
}