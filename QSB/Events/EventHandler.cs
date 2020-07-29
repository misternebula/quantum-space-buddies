﻿using System.Collections;
using QSB.Messaging;
using QSB.TransformSync;
using UnityEngine;
using UnityEngine.Networking;

namespace QSB.Events
{
    public class EventHandler : NetworkBehaviour
    {
        public static EventHandler LocalInstance;

        private MessageHandler<EventMessage> _eventHandler;

        private void Awake()
        {
            LocalInstance = this;

            _eventHandler = new MessageHandler<EventMessage>();
            _eventHandler.OnClientReceiveMessage += OnClientReceiveMessage;
            _eventHandler.OnServerReceiveMessage += OnServerReceiveMessage;
        }

        public void Send(EventType eventType)
        {
            StartCoroutine(SendEvent(eventType));
        }

        private IEnumerator SendEvent(EventType eventType)
        {
            yield return new WaitUntil(() => PlayerTransformSync.LocalInstance != null);
            var message = new EventMessage
            {
                EventType = (int)eventType,
                SenderId = PlayerTransformSync.LocalInstance.netId.Value
            };
            _eventHandler.SendToServer(message);
        }

        private void OnServerReceiveMessage(EventMessage message)
        {
            _eventHandler.SendToAll(message);
        }

        private void OnClientReceiveMessage(EventMessage message)
        {
            if (message.SenderId == PlayerTransformSync.LocalInstance.netId.Value)
            {
                return;
            }
            switch ((EventType)message.EventType)
            {
                case EventType.TurnOnFlashlight:
                    PlayerRegistry.GetPlayerFlashlight(message.SenderId).TurnOn();
                    PlayerRegistry.UpdateState(message.SenderId, State.Flashlight, true);
                    break;
                case EventType.TurnOffFlashlight:
                    PlayerRegistry.GetPlayerFlashlight(message.SenderId).TurnOff();
                    PlayerRegistry.UpdateState(message.SenderId, State.Flashlight, false);
                    break;
                case EventType.SuitUp:
                    PlayerRegistry.UpdateState(message.SenderId, State.Suit, true);
                    break;
                case EventType.RemoveSuit:
                    PlayerRegistry.UpdateState(message.SenderId, State.Suit, false);
                    break;
                case EventType.EquipSignalscope:
                    PlayerRegistry.UpdateState(message.SenderId, State.SignalScope, true);
                    PlayerRegistry.GetPlayerSignalscope(message.SenderId).EquipTool();
                    break;
                case EventType.UnequipSignalscope:
                    PlayerRegistry.UpdateState(message.SenderId, State.SignalScope, false);
                    PlayerRegistry.GetPlayerSignalscope(message.SenderId).UnequipTool();
                    break;
            }
        }
    }
}
