using OWML.Utils;
using QSB.CampfireSync.WorldObjects;
using QSB.Events;
using QSB.Messaging;
using QSB.QuantumSync;
using QSB.TranslationSync;
using QSB.TranslationSync.WorldObjects;
using QSB.Utility;
using QSB.WorldSync;
using System.Linq;

namespace QSB.Player.Events
{
	public class PlayerStatesRequestEvent : QSBEvent<PlayerMessage>
	{
		public override EventType Type => EventType.PlayerStatesRequest;

		public override void SetupListener() => GlobalMessenger.AddListener(EventNames.QSBPlayerStatesRequest, Handler);
		public override void CloseListener() => GlobalMessenger.RemoveListener(EventNames.QSBPlayerStatesRequest, Handler);

		private void Handler() => SendEvent(CreateMessage());

		private PlayerMessage CreateMessage() => new PlayerMessage
		{
			AboutId = LocalPlayerId,
			OnlySendToServer = true
		};

		public override void OnReceiveRemote(bool server, PlayerMessage message)
		{
			DebugLog.DebugWrite($"Get state request from {message.FromId} - isServer?{server}");
			QSBEventManager.FireEvent(EventNames.QSBServerSendPlayerStates);

			if (!server)
			{
				return;
			}

			QSBWorldSync.DialogueConditions.ForEach(condition
				=> QSBEventManager.FireEvent(EventNames.DialogueCondition, condition.Key, condition.Value));

			QSBWorldSync.ShipLogFacts.ForEach(fact
				=> QSBEventManager.FireEvent(EventNames.QSBRevealFact, fact.Id, fact.SaveGame, false));

			foreach (var wallText in QSBWorldSync.GetWorldObjects<QSBWallText>().Where(x => x.AttachedObject.GetValue<bool>("_initialized") && x.AttachedObject.GetNumTextBlocks() > 0))
			{
				wallText.GetTranslatedIds().ForEach(id
					=> QSBEventManager.FireEvent(EventNames.QSBTextTranslated, NomaiTextType.WallText, wallText.ObjectId, id));
			}

			foreach (var computer in QSBWorldSync.GetWorldObjects<QSBComputer>().Where(x => x.AttachedObject.GetValue<bool>("_initialized") && x.AttachedObject.GetNumTextBlocks() > 0))
			{
				computer.GetTranslatedIds().ForEach(id
					=> QSBEventManager.FireEvent(EventNames.QSBTextTranslated, NomaiTextType.Computer, computer.ObjectId, id));
			}

			foreach (var vesselComputer in QSBWorldSync.GetWorldObjects<QSBVesselComputer>().Where(x => x.AttachedObject.GetValue<bool>("_initialized") && x.AttachedObject.GetNumTextBlocks() > 0))
			{
				vesselComputer.GetTranslatedIds().ForEach(id
					=> QSBEventManager.FireEvent(EventNames.QSBTextTranslated, NomaiTextType.VesselComputer, vesselComputer.ObjectId, id));
			}

			var list = QSBWorldSync.GetWorldObjects<IQSBQuantumObject>().ToList();
			for (var i = 0; i < list.Count; i++)
			{
				QSBEventManager.FireEvent(EventNames.QSBQuantumAuthority, i, list[i].ControllingPlayer);
			}

			QSBWorldSync.GetWorldObjects<QSBCampfire>().ForEach(campfire
				=> QSBEventManager.FireEvent(EventNames.QSBCampfireState, campfire.ObjectId, campfire.GetState()));
		}
	}
}