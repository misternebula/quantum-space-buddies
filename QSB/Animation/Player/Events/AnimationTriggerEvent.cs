﻿using QSB.Animation.Player;
using QSB.Events;
using QSB.Player;
using QSB.Utility;

namespace QSB.Animation.Player.Events
{
	class AnimationTriggerEvent : QSBEvent<AnimationTriggerMessage>
	{
		public override EventType Type => EventType.AnimTrigger;

		public override void SetupListener() => GlobalMessenger<uint, string>.AddListener(EventNames.QSBAnimTrigger, Handler);
		public override void CloseListener() => GlobalMessenger<uint, string>.RemoveListener(EventNames.QSBAnimTrigger, Handler);

		private void Handler(uint attachedNetId, string name) => SendEvent(CreateMessage(attachedNetId, name));

		private AnimationTriggerMessage CreateMessage(uint attachedNetId, string name) => new AnimationTriggerMessage
		{
			AboutId = LocalPlayerId,
			AttachedNetId = attachedNetId,
			Name = name
		};

		public override void OnReceiveRemote(bool server, AnimationTriggerMessage message)
		{
			var animationSync = QSBPlayerManager.GetSyncObject<AnimationSync>(message.AttachedNetId);
			animationSync.VisibleAnimator.SetTrigger(message.Name);
		}
	}
}