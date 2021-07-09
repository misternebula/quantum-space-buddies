using QSB.Player;
using QSB.ProbeSync.TransformSync;
using QSB.Syncs.TransformSync;
using QSB.TimeSync;
using System.Linq;
using UnityEngine;

namespace QSB.Utility
{
	class DebugGUI : MonoBehaviour
	{
		private const float _debugLineSpacing = 11f;

		public void OnGUI()
		{
			if (!QSBCore.DebugMode)
			{
				return;
			}

			var offset = 10f;
			GUI.Label(new Rect(220, 10, 200f, 20f), $"FPS : {Mathf.Round(1f / Time.smoothDeltaTime)}");
			offset += _debugLineSpacing;
			GUI.Label(new Rect(220, offset, 200f, 20f), $"HasWokenUp : {QSBCore.WorldObjectsReady}");
			offset += _debugLineSpacing;
			if (WakeUpSync.LocalInstance != null)
			{
				GUI.Label(new Rect(220, offset, 200f, 20f), $"Time Difference : {WakeUpSync.LocalInstance.GetTimeDifference()}");
				offset += _debugLineSpacing;
				GUI.Label(new Rect(220, offset, 200f, 20f), $"Timescale : {OWTime.GetTimeScale()}");
				offset += _debugLineSpacing;
			}

			if (!QSBCore.WorldObjectsReady)
			{
				return;
			}

			var offset2 = 10f;
			GUI.Label(new Rect(320, offset2, 200f, 20f), $"Player data :");
			offset2 += _debugLineSpacing;
			foreach (var player in QSBPlayerManager.PlayerList.Where(x => x.PlayerStates.IsReady))
			{
				var networkTransform = player.TransformSync;
				var sector = networkTransform.ReferenceSector;

				GUI.Label(new Rect(320, offset2, 400f, 20f), $"{player.PlayerId}.{player.Name}");
				offset2 += _debugLineSpacing;
				GUI.Label(new Rect(320, offset2, 400f, 20f), $" - L.Pos : {networkTransform.transform.localPosition}");
				offset2 += _debugLineSpacing;
				GUI.Label(new Rect(320, offset2, 400f, 20f), $" - Sector : {(sector == null ? "NULL" : sector.Name)}");
				offset2 += _debugLineSpacing;
				var probeSync = BaseTransformSync.GetPlayers<PlayerProbeSync>(player);
				if (probeSync == default)
				{
					return;
				}

				var probeSector = probeSync.ReferenceSector;
				GUI.Label(new Rect(320, offset2, 400f, 20f), $" - Probe Sector : {(probeSector == null ? "NULL" : probeSector.Name)}");
				offset2 += _debugLineSpacing;
			}
		}
	}
}