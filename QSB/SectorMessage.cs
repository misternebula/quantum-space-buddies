﻿using QSB.Messaging;
using UnityEngine.Networking;

namespace QSB
{
    public class SectorMessage : QSBMessage
    {
        public override MessageType MessageType => MessageType.SectorSync;

        public int SectorId;
        public uint SenderId;

        public override void Deserialize(NetworkReader reader)
        {
            SectorId = reader.ReadInt32();
            SenderId = reader.ReadPackedUInt32();
        }

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(SectorId);
            writer.Write(SenderId);
        }

    }
}