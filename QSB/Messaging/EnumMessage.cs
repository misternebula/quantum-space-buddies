﻿using QuantumUNET.Transport;

namespace QSB.Messaging
{
	public class EnumMessage<T> : PlayerMessage
	{
		public T EnumValue;

		public override void Deserialize(QNetworkReader reader)
		{
			base.Deserialize(reader);
			EnumValue = (T)(object)reader.ReadInt32();
		}

		public override void Serialize(QNetworkWriter writer)
		{
			base.Serialize(writer);
			writer.Write((int)(object)EnumValue);
		}
	}
}