﻿using OWML.Common;
using QSB.Animation.Events;
using QSB.ConversationSync.Events;
using QSB.DeathSync.Events;
using QSB.ElevatorSync.Events;
using QSB.FrequencySync.Events;
using QSB.GeyserSync.Events;
using QSB.LogSync.Events;
using QSB.OrbSync.Events;
using QSB.Player.Events;
using QSB.QuantumSync.Events;
using QSB.TimeSync.Events;
using QSB.Tools.Events;
using QSB.TranslationSync.Events;
using QSB.Utility;
using System.Collections.Generic;

namespace QSB.Events
{
	public static class EventManager
	{
		public static bool Ready { get; private set; }

		private static List<IQSBEvent> _eventList = new List<IQSBEvent>();

		public static void Init()
		{
			_eventList = new List<IQSBEvent>
			{
				// Player
				new PlayerReadyEvent(),
				new PlayerJoinEvent(),
				new PlayerSuitEvent(),
				new PlayerFlashlightEvent(),
				new PlayerSignalscopeEvent(),
				new PlayerTranslatorEvent(),
				new PlayerProbeLauncherEvent(),
				new PlayerProbeEvent(),
				new PlayerSectorEvent(),
				new PlayerDeathEvent(),
				new PlayerStatesRequestEvent(),
				new ServerSendPlayerStatesEvent(),
				new ChangeAnimTypeEvent(),
				new CrouchEvent(),
				new ServerTimeEvent(),
				new PlayerEntangledEvent(),
				// World Objects
				new ElevatorEvent(),
				new GeyserEvent(),
				new OrbSlotEvent(),
				new OrbUserEvent(),
				new SocketStateChangeEvent(),
				new MultiStateChangeEvent(),
				new SetAsTranslatedEvent(),
				new QuantumShuffleEvent(),
				new MoonStateChangeEvent(),
				new EnterLeaveEvent(),
				new QuantumAuthorityEvent(),
				// Conversation/dialogue/exploration
				new ConversationEvent(),
				new ConversationStartEndEvent(),
				new DialogueConditionEvent(),
				new RevealFactEvent(),
				new IdentifyFrequencyEvent(),
				new IdentifySignalEvent()
			};

			if (UnitTestDetector.IsInUnitTest)
			{
				return;
			}

			_eventList.ForEach(ev => ev.SetupListener());

			Ready = true;

			DebugLog.DebugWrite("Event Manager ready.", MessageType.Success);
		}

		public static void Reset()
		{
			Ready = false;
			_eventList.ForEach(ev => ev.CloseListener());
			_eventList = new List<IQSBEvent>();
		}

		public static void FireEvent(string eventName)
		{
			if (!QSBCore.IsInMultiplayer)
			{
				return;
			}
			GlobalMessenger.FireEvent(eventName);
		}

		public static void FireEvent<T>(string eventName, T arg)
		{
			if (!QSBCore.IsInMultiplayer)
			{
				DebugLog.ToConsole($"Warning - Tried to send event {eventName} while not connected to/hosting server.", MessageType.Warning);
				return;
			}
			GlobalMessenger<T>.FireEvent(eventName, arg);
		}

		public static void FireEvent<T, U>(string eventName, T arg1, U arg2)
		{
			if (!QSBCore.IsInMultiplayer)
			{
				DebugLog.ToConsole($"Warning - Tried to send event {eventName} while not connected to/hosting server.", MessageType.Warning);
				return;
			}
			GlobalMessenger<T, U>.FireEvent(eventName, arg1, arg2);
		}

		public static void FireEvent<T, U, V>(string eventName, T arg1, U arg2, V arg3)
		{
			if (!QSBCore.IsInMultiplayer)
			{
				DebugLog.ToConsole($"Warning - Tried to send event {eventName} while not connected to/hosting server.", MessageType.Warning);
				return;
			}
			GlobalMessenger<T, U, V>.FireEvent(eventName, arg1, arg2, arg3);
		}
	}
}