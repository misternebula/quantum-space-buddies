using OWML.Common;
using QSB.Player;
using QSB.SectorSync;
using QSB.Syncs.TransformSync;
using QSB.Tools;
using QSB.Utility;
using UnityEngine;

namespace QSB.ProbeSync.TransformSync
{
	public class PlayerProbeSync : SectoredTransformSync
	{
		public static PlayerProbeSync LocalInstance { get; private set; }

		public override void OnStartAuthority()
		{
			DebugLog.DebugWrite($"OnStartAuthority probe");
			LocalInstance = this;
		}

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

			var body = probe.InstantiateInactive();
			body.name = "RemoteProbeTransform";

			PlayerToolsManager.CreateProbe(body, Player);

			Player.ProbeBody = body.gameObject;

			return body;
		}

		public override bool IsReady => Locator.GetProbe() != null
			&& Player != null
			&& QSBPlayerManager.PlayerExists(Player.PlayerId)
			&& Player.PlayerStates.IsReady
			&& NetId.Value != uint.MaxValue
			&& NetId.Value != 0U;

		protected override float DistanceLeeway => 10f;

		public override bool UseInterpolation => true;

		public override TargetType Type => TargetType.Probe;
	}
}