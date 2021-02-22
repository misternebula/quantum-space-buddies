﻿using QSB.Events;
using QSB.Messaging;
using QSB.Player;

namespace QSB.Player.Tools.Events
{
	public class PlayerFlashlightEvent : QSBEvent<ToggleMessage>
	{
		public override EventType Type => EventType.FlashlightActiveChange;

		public override void SetupListener()
		{
			GlobalMessenger.AddListener(EventNames.TurnOnFlashlight, HandleTurnOn);
			GlobalMessenger.AddListener(EventNames.TurnOffFlashlight, HandleTurnOff);
		}

		public override void CloseListener()
		{
			GlobalMessenger.RemoveListener(EventNames.TurnOnFlashlight, HandleTurnOn);
			GlobalMessenger.RemoveListener(EventNames.TurnOffFlashlight, HandleTurnOff);
		}

		private void HandleTurnOn() => SendEvent(CreateMessage(true));
		private void HandleTurnOff() => SendEvent(CreateMessage(false));

		private ToggleMessage CreateMessage(bool value) => new ToggleMessage
		{
			AboutId = LocalPlayerId,
			ToggleValue = value
		};

		public override void OnReceiveRemote(bool server, ToggleMessage message)
		{
			var player = PlayerManager.GetPlayer(message.AboutId);
			player.UpdateState(State.Flashlight, message.ToggleValue);
			player.FlashLight?.UpdateState(message.ToggleValue);
		}

		public override void OnReceiveLocal(bool server, ToggleMessage message) =>
			PlayerManager.LocalPlayer.UpdateState(State.Flashlight, message.ToggleValue);
	}
}