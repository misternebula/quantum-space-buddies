﻿using UnityEngine.Networking;

namespace QSB.Messaging
{
    public class SectorMessage : PlayerMessage
    {
        public int SectorId;
        public string SectorName;

        public override void Deserialize(NetworkReader reader)
        {
            base.Deserialize(reader);
            SectorId = reader.ReadInt32();
            SectorName = reader.ReadString();
        }

        public override void Serialize(NetworkWriter writer)
        {
            base.Serialize(writer);
            writer.Write(SectorId);
            writer.Write(SectorName);
        }
    }
}