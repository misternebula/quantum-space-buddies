using QSB.Animation.Player;
using QSB.Animation.Player.Thrusters;
using QSB.CampfireSync.WorldObjects;
using QSB.Player.TransformSync;
using QSB.ProbeSync;
using QSB.QuantumSync;
using QSB.RoastingSync;
using QSB.Tools;
using QSB.Utility;
using System.Linq;
using UnityEngine;

namespace QSB.Player
{
	public class PlayerInfo
	{
		public uint PlayerId { get; }
		public string Name { get; set; }
		public PlayerHUDMarker HudMarker { get; set; }
		public PlayerState PlayerStates { get; set; } = new PlayerState();
		public PlayerTransformSync TransformSync { get; set; }

		// Body Objects
		public OWCamera Camera { get; set; }
		public GameObject CameraBody { get; set; }
		public GameObject Body { get; set; }
		public GameObject RoastingStick { get; set; }
		public bool Visible { get; set; } = true;

		// Tools
		public GameObject ProbeBody { get; set; }
		public QSBProbe Probe { get; set; }
		public QSBFlashlight FlashLight => CameraBody?.GetComponentInChildren<QSBFlashlight>();
		public QSBTool Signalscope => GetToolByType(ToolType.Signalscope);
		public QSBTool Translator => GetToolByType(ToolType.Translator);
		public QSBTool ProbeLauncher => GetToolByType(ToolType.ProbeLauncher);
		public Transform ItemSocket => CameraBody.transform.Find("ItemSocket");
		public Transform ScrollSocket => CameraBody.transform.Find("ScrollSocket");
		public Transform SharedStoneSocket => CameraBody.transform.Find("SharedStoneSocket");
		public Transform WarpCoreSocket => CameraBody.transform.Find("WarpCoreSocket");
		public Transform VesselCoreSocket => CameraBody.transform.Find("VesselCoreSocket");
		public QSBMarshmallow Marshmallow { get; set; }
		public QSBCampfire Campfire { get; set; }

		// Conversation
		public int CurrentCharacterDialogueTreeId { get; set; }
		public GameObject CurrentDialogueBox { get; set; }

		// Animation
		public AnimationSync AnimationSync => QSBPlayerManager.GetSyncObject<AnimationSync>(PlayerId);
		public bool PlayingInstrument => AnimationSync.CurrentType != AnimationType.PlayerSuited
			&& AnimationSync.CurrentType != AnimationType.PlayerUnsuited;
		public JetpackAccelerationSync JetpackAcceleration { get; set; }

		// Misc
		public bool IsInMoon; // TODO : move into PlayerStates?
		public bool IsInShrine; // TODO : move into PlayerStates?
		public IQSBQuantumObject EntangledObject;
		public bool IsDead { get; set; }

		// Local only
		public PlayerProbeLauncher LocalProbeLauncher
		{
			get
			{
				if (QSBPlayerManager.LocalPlayer != this)
				{
					DebugLog.ToConsole($"Warning - Tried to access local-only property LocalProbeLauncher in PlayerInfo for non local player!", OWML.Common.MessageType.Warning);
					return null;
				}

				return CameraBody.transform.Find("ProbeLauncher").GetComponent<PlayerProbeLauncher>();
			}
		}

		public Flashlight LocalFlashlight 
		{
			get
			{
				if (QSBPlayerManager.LocalPlayer != this)
				{
					DebugLog.ToConsole($"Warning - Tried to access local-only property LocalFlashlight in PlayerInfo for non local player!", OWML.Common.MessageType.Warning);
					return null;
				}

				return Locator.GetFlashlight();
			}
		}

		public Signalscope LocalSignalscope
		{
			get
			{
				if (QSBPlayerManager.LocalPlayer != this)
				{
					DebugLog.ToConsole($"Warning - Tried to access local-only property LocalSignalscope in PlayerInfo for non local player!", OWML.Common.MessageType.Warning);
					return null;
				}

				return CameraBody.transform.Find("Signalscope").GetComponent<Signalscope>();
			}
		}

		public NomaiTranslator LocalTranslator
		{
			get
			{
				if (QSBPlayerManager.LocalPlayer != this)
				{
					DebugLog.ToConsole($"Warning - Tried to access local-only property LocalTranslator in PlayerInfo for non local player!", OWML.Common.MessageType.Warning);
					return null;
				}

				return CameraBody.transform.Find("NomaiTranslatorProp").GetComponent<NomaiTranslator>();
			}
		}


		public PlayerInfo(uint id)
		{
			PlayerId = id;
			CurrentCharacterDialogueTreeId = -1;
		}

		public void UpdateStateObjects()
		{
			if (OWInput.GetInputMode() == InputMode.None)
			{
				return;
			}

			FlashLight?.UpdateState(PlayerStates.FlashlightActive);
			Translator?.ChangeEquipState(PlayerStates.TranslatorEquipped);
			ProbeLauncher?.ChangeEquipState(PlayerStates.ProbeLauncherEquipped);
			Signalscope?.ChangeEquipState(PlayerStates.SignalscopeEquipped);
			QSBCore.UnityEvents.RunWhen(() => QSBPlayerManager.GetSyncObject<AnimationSync>(PlayerId) != null,
				() => QSBPlayerManager.GetSyncObject<AnimationSync>(PlayerId).SetSuitState(PlayerStates.SuitedUp));
		}

		private QSBTool GetToolByType(ToolType type) => CameraBody?.GetComponentsInChildren<QSBTool>()
				.FirstOrDefault(x => x.Type == type);
	}
}