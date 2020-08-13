﻿using QSB.Messaging;
using UnityEngine.Networking;

namespace QSB.DeathSync
{
    public class PlayerDeathMessage : PlayerMessage
    {
        public DeathType DeathType { get; set; }

        public override void Deserialize(NetworkReader reader)
        {
            base.Deserialize(reader);
            DeathType = (DeathType)reader.ReadInt16();
        }

        public override void Serialize(NetworkWriter writer)
        {
            base.Serialize(writer);
            writer.Write((short)DeathType);
        }
    }
}
