using OWML.Common;
using OWML.Utils;
using QSB.Player;
using QSB.SectorSync;
using QSB.Syncs.TransformSync;
using QSB.Tools;
using QSB.Utility;
using QSB.WorldSync;
using UnityEngine;

namespace QSB.ProbeSync.TransformSync
{
	public class PlayerProbeSync : SectoredTransformSync
	{
		protected override float DistanceLeeway => 10f;
		public override bool UseInterpolation => true;
		public override TargetType Type => TargetType.Probe;
		public override bool IgnoreDisabledAttachedObject => true;

		public static PlayerProbeSync LocalInstance { get; private set; }

		public override void OnStartAuthority() => LocalInstance = this;

		protected override Component InitLocalTransform()
		{
			SectorSync.Init(Locator.GetProbe().GetSectorDetector(), this);

			var body = Locator.GetProbe().transform;
			Player.ProbeBody = body.gameObject;

			if (Player.Body == null)
			{
				DebugLog.ToConsole($"Warning - Player.Body is null!", MessageType.Warning);
				return null;
			}

			var listener = Player.Body.AddComponent<ProbeListener>();
			listener.Init(Locator.GetProbe());

			return body;
		}

		protected override Component InitRemoteTransform()
		{
			var probe = Locator.GetProbe().transform;

			if (probe == null)
			{
				DebugLog.ToConsole("Error - Probe is null!", MessageType.Error);
				return default;
			}

			var body = probe.gameObject.activeSelf 
				? probe.InstantiateInactive() 
				: Instantiate(probe);

			body.name = "RemoteProbeTransform";

			PlayerToolsManager.CreateProbe(body, Player);

			Player.ProbeBody = body.gameObject;

			return body;
		}

		protected override bool UpdateTransform()
		{
			if (!base.UpdateTransform())
			{
				return false;
			}

			if (HasAuthority)
			{
				if (!AttachedObject.gameObject.activeInHierarchy)
				{
					var probeOWRigidbody = Locator.GetProbe().GetComponent<SurveyorProbe>().GetOWRigidbody();
					if (probeOWRigidbody == null)
					{
						DebugLog.ToConsole($"Warning - Could not find OWRigidbody of local probe.", MessageType.Warning);
					}

					var probeLauncher = Player.LocalProbeLauncher;
					var launcherTransform = probeLauncher.GetValue<Transform>("_launcherTransform");
					probeOWRigidbody.SetPosition(launcherTransform.position);
					probeOWRigidbody.SetRotation(launcherTransform.rotation);

					_intermediaryTransform.EncodePosition(AttachedObject.transform.position);
					_intermediaryTransform.EncodeRotation(AttachedObject.transform.rotation);

					var currentReferenceSector = ReferenceSector;
					var playerReferenceSector = Player.TransformSync.ReferenceSector;

					if (currentReferenceSector != playerReferenceSector)
					{
						SetReferenceSector(playerReferenceSector);
					}
				}
			}

			return true;
		}

		public override bool IsReady => Locator.GetProbe() != null
			&& Player != null
			&& QSBPlayerManager.PlayerExists(Player.PlayerId)
			&& Player.PlayerStates.IsReady
			&& NetId.Value != uint.MaxValue
			&& NetId.Value != 0U
			&& WorldObjectManager.AllReady;
	}
}