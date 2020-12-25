﻿using OWML.Utils;
using QSB.Events;
using QSB.QuantumSync.WorldObjects;
using QSB.WorldSync;

namespace QSB.QuantumSync.Events
{
	public class MultiStateChangeEvent : QSBEvent<MultiStateChangeMessage>
	{
		public override QSB.Events.EventType Type => QSB.Events.EventType.MultiStateChange;

		public override void SetupListener() => GlobalMessenger<int, int>.AddListener(EventNames.QSBMultiStateChange, Handler);
		public override void CloseListener() => GlobalMessenger<int, int>.RemoveListener(EventNames.QSBMultiStateChange, Handler);

		private void Handler(int objid, int stateIndex) => SendEvent(CreateMessage(objid, stateIndex));

		private MultiStateChangeMessage CreateMessage(int objid, int stateIndex) => new MultiStateChangeMessage
		{
			AboutId = LocalPlayerId,
			ObjectId = objid,
			StateIndex = stateIndex
		};

		public override void OnReceiveLocal(bool server, MultiStateChangeMessage message)
		{
			if (!QSBCore.DebugMode)
			{
				return;
			}
			var qsbObj = QSBWorldSync.GetWorldObject<QSBMultiStateQuantumObject>(message.ObjectId);
			qsbObj.DebugBoxText.text = message.StateIndex.ToString();
		}

		public override void OnReceiveRemote(bool server, MultiStateChangeMessage message)
		{
			if (!QSBCore.HasWokenUp)
			{
				return;
			}
			var qsbObj = QSBWorldSync.GetWorldObject<QSBMultiStateQuantumObject>(message.ObjectId);
			var unityObj = qsbObj.AttachedObject;
			var currentStateIndex = unityObj.GetValue<int>("_stateIndex");
			if (currentStateIndex != -1)
			{
				qsbObj.QuantumStates[currentStateIndex].SetVisible(false);
			}
			qsbObj.QuantumStates[message.StateIndex].SetVisible(true);
			unityObj.SetValue("_stateIndex", message.StateIndex);
			if (QSBCore.DebugMode)
			{
				qsbObj.DebugBoxText.text = message.StateIndex.ToString();
			}
		}
	}
}