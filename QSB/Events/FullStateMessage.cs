﻿using System;
using System.Collections.Generic;
using System.Linq;
using QSB.Messaging;
using UnityEngine.Networking;

namespace QSB.Events
{
    public class FullStateMessage : PlayerMessage
    {
        public override MessageType MessageType => MessageType.FullState;

        public string PlayerName { get; set; }

        public override void Deserialize(NetworkReader reader)
        {
            base.Deserialize(reader);
            PlayerName = reader.ReadString();
        }

        public override void Serialize(NetworkWriter writer)
        {
            base.Serialize(writer);
            writer.Write(PlayerName);
        }
    }
}
