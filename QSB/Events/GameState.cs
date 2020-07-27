﻿using System.Collections;
using System.Collections.Generic;
using QSB.Messaging;
using QSB.TransformSync;
using UnityEngine;
using UnityEngine.Networking;

namespace QSB.Events
{
    class GameState : NetworkBehaviour
    {
        public static GameState LocalInstance { get; private set; }

        private MessageHandler<FullStateMessage> _messageHandler;

        private void Awake()
        {
            _messageHandler = new MessageHandler<FullStateMessage>();
            _messageHandler.OnClientReceiveMessage += OnClientReceiveMessage;

            LocalInstance = this;
        }

        private void OnClientReceiveMessage(FullStateMessage message)
        {
            Finder.HandleFullStateMessage(message);
        }

        public void Send()
        {

            foreach (var player in Finder.GetPlayers())
            {
                var message = new FullStateMessage()
                {
                    PlayerName = player.Name,
                    SenderId = player.NetId
                };

                _messageHandler.SendToAll(message);
            }
        }
    }
}
