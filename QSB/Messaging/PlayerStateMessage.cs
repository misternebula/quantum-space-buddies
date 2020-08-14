﻿using UnityEngine.Networking;

namespace QSB.Messaging
{
    public class PlayerStateMessage : PlayerMessage
    {
        public string PlayerName { get; set; }
        public bool PlayerReady { get; set; }
        public bool PlayerAwake { get; set; }
        public State PlayerState { get; set; }
        public Sector.Name SectorID { get; set; }
        public string SectorName { get; set; }

        public override void Deserialize(NetworkReader reader)
        {
            base.Deserialize(reader);
            PlayerName = reader.ReadString();
            PlayerReady = reader.ReadBoolean();
            PlayerAwake = reader.ReadBoolean();
            PlayerState = (State)reader.ReadInt32();
            SectorID = (Sector.Name)reader.ReadInt32();
            SectorName = reader.ReadString();
        }

        public override void Serialize(NetworkWriter writer)
        {
            base.Serialize(writer);
            writer.Write(PlayerName);
            writer.Write(PlayerReady);
            writer.Write(PlayerAwake);
            writer.Write((int)PlayerState);
            writer.Write((int)SectorID);
            writer.Write(SectorName);
        }
    }
}
