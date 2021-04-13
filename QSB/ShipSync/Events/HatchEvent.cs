﻿using OWML.Utils;
using QSB.Events;
using QSB.Messaging;
using System.Linq;
using UnityEngine;

namespace QSB.ShipSync.Events
{
	class HatchEvent : QSBEvent<BoolMessage>
	{
		public override QSB.Events.EventType Type => QSB.Events.EventType.OpenHatch;

		public override void SetupListener() 
			=> GlobalMessenger<bool>.AddListener(EventNames.QSBHatchState, Handler);

		public override void CloseListener() 
			=> GlobalMessenger<bool>.RemoveListener(EventNames.QSBHatchState, Handler);

		private void Handler(bool open) => SendEvent(CreateMessage(open));

		private BoolMessage CreateMessage(bool open) => new BoolMessage
		{
			AboutId = LocalPlayerId,
			Value = open
		};

		public override void OnReceiveRemote(bool server, BoolMessage message)
		{
			var shipTransform = Locator.GetShipTransform();
			var hatchController = shipTransform.GetComponentInChildren<HatchController>();
			if (message.Value)
			{
				hatchController.Invoke("OpenHatch");
			}
			else
			{
				Resources.FindObjectsOfTypeAll<ShipTractorBeamSwitch>().First().DeactivateTractorBeam();
				hatchController.Invoke("CloseHatch");
			}
		}
	}
}