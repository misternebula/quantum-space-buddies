﻿using System;
using UnityEngine.Networking;

namespace QSB.Messaging
{
    // Extend this to create new message handlers.
    public class MessageHandler<T> where T : MessageBase, new()
    {
        public event Action<T> OnClientReceiveMessage;
        public event Action<T> OnServerReceiveMessage;

        private readonly MessageType _messageType;

        public MessageHandler(MessageType messageType)
        {
            _messageType = messageType + MsgType.Highest + 1;
            if (QSBNetworkManager.Instance.IsReady)
            {
                Init();
            }
            else
            {
                QSBNetworkManager.Instance.OnNetworkManagerReady += Init;
            }
        }

        private void Init()
        {
            NetworkServer.RegisterHandler((short)_messageType, OnServerReceiveMessageHandler);
            NetworkManager.singleton.client.RegisterHandler((short)_messageType, OnClientReceiveMessageHandler);
        }

        public void SendToAll(T message)
        {
            if (!QSBNetworkManager.Instance.IsReady)
            {
                return;
            }
            NetworkServer.SendToAll((short)_messageType, message);
        }

        public void SendToServer(T message)
        {
            if (!QSBNetworkManager.Instance.IsReady)
            {
                return;
            }
            NetworkManager.singleton.client.Send((short)_messageType, message);
        }

        private void OnClientReceiveMessageHandler(NetworkMessage netMsg)
        {
            var message = netMsg.ReadMessage<T>();
            OnClientReceiveMessage?.Invoke(message);
        }

        private void OnServerReceiveMessageHandler(NetworkMessage netMsg)
        {
            var message = netMsg.ReadMessage<T>();
            OnServerReceiveMessage?.Invoke(message);
        }

    }
}
