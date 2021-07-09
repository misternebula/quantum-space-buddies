using QSB.Player;
using QSB.SectorSync;
using QSB.Syncs.RigidbodySync;
using QSB.Utility;
using UnityEngine;

namespace QSB.ShipSync.TransformSync
{
	public class ShipTransformSync : SectoredRigidbodySync
	{
		public static ShipTransformSync LocalInstance { get; private set; }

		public override bool IsReady
			=> Locator.GetShipBody() != null;

		public override void Start()
		{
			DebugLog.DebugWrite($"START!");
			base.Start();
			LocalInstance = this;
		}

		protected override Component InitLocalTransform() => throw new System.NotImplementedException();
		protected override Component InitRemoteTransform() => throw new System.NotImplementedException();

		protected override OWRigidbody GetRigidbody()
		{
			SectorSync.Init(Locator.GetShipDetector().GetComponent<SectorDetector>(), this);
			return Locator.GetShipBody();
		}

		protected override void UpdateTransform()
		{
			if (HasAuthority && ShipManager.Instance.CurrentFlyer != QSBPlayerManager.LocalPlayerId && ShipManager.Instance.CurrentFlyer != uint.MaxValue)
			{
				DebugLog.ToConsole("Warning - Has authority, but is not current flyer!", OWML.Common.MessageType.Warning);
				return;
			}

			if (!HasAuthority && ShipManager.Instance.CurrentFlyer == QSBPlayerManager.LocalPlayerId)
			{
				DebugLog.ToConsole($"Warning - Doesn't have authority, but is current flyer!", OWML.Common.MessageType.Warning);
				return;
			}

			base.UpdateTransform();
		}

		public override TargetType Type => TargetType.Ship;

		public override bool UseInterpolation => true;
		protected override float DistanceLeeway => 20f;
	}
}