using QSB.Syncs.TransformSync;
using QSB.Utility;
using QSB.WorldSync;
using System.Collections.Generic;
using UnityEngine;

namespace QSB.OrbSync.TransformSync
{
	internal class NomaiOrbTransformSync : UnparentedBaseTransformSync
	{
		public static List<NomaiOrbTransformSync> OrbTransformSyncs = new List<NomaiOrbTransformSync>();

		private int _index => OrbTransformSyncs.IndexOf(this);

		public override void OnStartClient() => OrbTransformSyncs.Add(this);

		protected override void OnDestroy()
		{
			OrbTransformSyncs.Remove(this);
			QSBSceneManager.OnSceneLoaded -= OnSceneLoaded;
		}

		protected override void Init()
		{
			base.Init();
			var originalParent = AttachedObject.GetAttachedOWRigidbody().GetOrigParent();
			if (originalParent == Locator.GetRootTransform())
			{
				DebugLog.DebugWrite($"{_logName} with AttachedObject {AttachedObject.name} had it's original parent as SolarSystemRoot - Destroying...");
				Destroy(this);
			}

			SetReferenceTransform(originalParent);
		}

		private Transform GetTransform()
		{
			if (_index == -1)
			{
				DebugLog.ToConsole($"Error - Index cannot be found.", OWML.Common.MessageType.Error);
				return null;
			}

			if (QSBWorldSync.OldOrbList == null || QSBWorldSync.OldOrbList.Count <= _index)
			{
				DebugLog.ToConsole($"Error - OldOrbList is null or does not contain index {_index}.", OWML.Common.MessageType.Error);
				return null;
			}

			if (QSBWorldSync.OldOrbList[_index] == null)
			{
				DebugLog.ToConsole($"Error - OldOrbList index {_index} is null.", OWML.Common.MessageType.Error);
				return null;
			}

			return QSBWorldSync.OldOrbList[_index].transform;
		}

		protected override Component InitLocalTransform() => GetTransform();
		protected override Component InitRemoteTransform() => GetTransform();

		public override bool IsReady => QSBCore.WorldObjectsReady;
		public override bool UseInterpolation => false;
	}
}
