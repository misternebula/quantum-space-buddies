﻿using UnityEngine.Networking;

namespace QSB.Messaging
{
    public class PlayerStateMessage : PlayerMessage
    {
        public string PlayerName { get; set; }
        public bool PlayerReady { get; set; }
        public State PlayerState { get; set; }

        public override void Deserialize(NetworkReader reader)
        {
            base.Deserialize(reader);
            PlayerName = reader.ReadString();
            PlayerReady = reader.ReadBoolean();
            PlayerState = (State)reader.ReadInt32();
        }

        public override void Serialize(NetworkWriter writer)
        {
            base.Serialize(writer);
            writer.Write(PlayerName);
            writer.Write(PlayerReady);
            writer.Write((int)PlayerState);
        }
    }
}