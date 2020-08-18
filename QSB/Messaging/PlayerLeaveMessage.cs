﻿using System;
using System.Linq;
using UnityEngine.Networking;

namespace QSB.Messaging
{
    public class PlayerLeaveMessage : PlayerMessage
    {
        public uint[] NetIds { get; set; }

        public override void Deserialize(NetworkReader reader)
        {
            base.Deserialize(reader);
            NetIds = reader.ReadString().Split(',').Select(x => Convert.ToUInt32(x)).ToArray();
        }

        public override void Serialize(NetworkWriter writer)
        {
            base.Serialize(writer);
            writer.Write(string.Join(",", NetIds.Select(x => x.ToString()).ToArray()));
        }
    }
}