using OWML.Common;
using QSB.Events;
using QSB.Player;
using QSB.QuantumSync.WorldObjects;
using QSB.Utility;
using QSB.WorldSync;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace QSB.QuantumSync
{
	internal class QuantumManager : WorldObjectManager
	{
		public static QuantumShrine Shrine { get; private set; }
		public static QuantumManager Instance { get; private set; }

		public override void Awake()
		{
			base.Awake();
			Instance = this;
			QSBPlayerManager.OnRemovePlayer += PlayerLeave;
		}

		public override void OnDestroy()
		{
			base.OnDestroy();
			QSBPlayerManager.OnRemovePlayer -= PlayerLeave;
		}

		protected override void RebuildWorldObjects(OWScene scene)
		{
			DebugLog.DebugWrite("Rebuilding quantum objects...", MessageType.Warning);
			QSBWorldSync.Init<QSBQuantumState, QuantumState>();
			QSBWorldSync.Init<QSBSocketedQuantumObject, SocketedQuantumObject>();
			QSBWorldSync.Init<QSBMultiStateQuantumObject, MultiStateQuantumObject>();
			QSBWorldSync.Init<QSBQuantumSocket, QuantumSocket>();
			QSBWorldSync.Init<QSBQuantumShuffleObject, QuantumShuffleObject>();
			QSBWorldSync.Init<QSBQuantumMoon, QuantumMoon>();
			QSBWorldSync.Init<QSBEyeProxyQuantumMoon, EyeProxyQuantumMoon>();
			if (scene == OWScene.SolarSystem)
			{
				Shrine = Resources.FindObjectsOfTypeAll<QuantumShrine>().First();
			}
		}

		public void PlayerLeave(uint playerId)
		{
			if (!QSBCore.IsServer)
			{
				return;
			}

			var quantumObjects = QSBWorldSync.GetWorldObjects<IQSBQuantumObject>().ToList();
			for (var i = 0; i < quantumObjects.Count; i++)
			{
				var obj = quantumObjects[i];
				if (obj.ControllingPlayer == playerId)
				{
					var idToSend = obj.IsEnabled ? QSBPlayerManager.LocalPlayerId : 0u;
					QSBEventManager.FireEvent(EventNames.QSBQuantumAuthority, i, idToSend);
				}
			}
		}

		public void OnRenderObject()
		{
			if (!QSBCore.WorldObjectsReady || !QSBCore.DebugMode || !QSBCore.ShowLinesInDebug)
			{
				return;
			}

			if (Shrine != null)
			{
				Popcron.Gizmos.Sphere(Shrine.transform.position, 10f, Color.magenta);
			}
		}

		public static Tuple<bool, List<PlayerInfo>> IsVisibleUsingCameraFrustum(ShapeVisibilityTracker tracker, bool ignoreLocalCamera)
		{
			if (!AllReady)
			{
				return new Tuple<bool, List<PlayerInfo>>(false, new List<PlayerInfo>());
			}

			var playersWithCameras = QSBPlayerManager.GetPlayersWithCameras(!ignoreLocalCamera);
			if (playersWithCameras.Count == 0)
			{
				DebugLog.ToConsole($"Warning - Trying to run IsVisibleUsingCameraFrustum when there are no players!", MessageType.Warning);
				return new Tuple<bool, List<PlayerInfo>>(false, new List<PlayerInfo>());
			}

			if (!tracker.gameObject.activeInHierarchy)
			{
				return new Tuple<bool, List<PlayerInfo>>(false, new List<PlayerInfo>());
			}

			var frustumMethod = tracker.GetType().GetMethod("IsInFrustum", BindingFlags.NonPublic | BindingFlags.Instance);

			var playersWhoCanSee = new List<PlayerInfo>();
			var foundPlayers = false;
			foreach (var player in playersWithCameras)
			{
				if (player.Camera == null)
				{
					DebugLog.ToConsole($"Warning - Camera is null for id:{player.PlayerId}!", MessageType.Warning);
					continue;
				}

				var isInFrustum = (bool)frustumMethod.Invoke(tracker, new object[] { player.Camera.GetFrustumPlanes() });
				if (isInFrustum)
				{
					playersWhoCanSee.Add(player);
					foundPlayers = true;
				}
			}

			return new Tuple<bool, List<PlayerInfo>>(foundPlayers, playersWhoCanSee);
		}

		public static bool IsVisible(ShapeVisibilityTracker tracker, bool ignoreLocalCamera) => tracker.gameObject.activeInHierarchy
				&& IsVisibleUsingCameraFrustum(tracker, ignoreLocalCamera).First
				&& QSBPlayerManager.GetPlayersWithCameras(!ignoreLocalCamera)
					.Any(x => VisibilityOccluder.CanYouSee(tracker, x.Camera.mainCamera.transform.position));

		public static IEnumerable<PlayerInfo> GetEntangledPlayers(QuantumObject obj)
		{
			if (!WorldObjectManager.AllReady)
			{
				return Enumerable.Empty<PlayerInfo>();
			}

			var worldObj = GetObject(obj);
			return QSBPlayerManager.PlayerList.Where(x => x.EntangledObject == worldObj);
		}

		public static IQSBQuantumObject GetObject(QuantumObject unityObject)
		{
			IQSBQuantumObject worldObj = null;
			if (unityObject.GetType() == typeof(SocketedQuantumObject) || unityObject.GetType() == typeof(QuantumShrine))
			{
				worldObj = QSBWorldSync.GetWorldFromUnity<QSBSocketedQuantumObject, SocketedQuantumObject>((SocketedQuantumObject)unityObject);
			}
			else if (unityObject.GetType() == typeof(MultiStateQuantumObject))
			{
				worldObj = QSBWorldSync.GetWorldFromUnity<QSBMultiStateQuantumObject, MultiStateQuantumObject>((MultiStateQuantumObject)unityObject);
			}
			else if (unityObject.GetType() == typeof(QuantumShuffleObject))
			{
				worldObj = QSBWorldSync.GetWorldFromUnity<QSBQuantumShuffleObject, QuantumShuffleObject>((QuantumShuffleObject)unityObject);
			}
			else if (unityObject.GetType() == typeof(QuantumMoon))
			{
				worldObj = QSBWorldSync.GetWorldFromUnity<QSBQuantumMoon, QuantumMoon>((QuantumMoon)unityObject);
			}
			else if (unityObject.GetType() == typeof(EyeProxyQuantumMoon))
			{
				worldObj = QSBWorldSync.GetWorldFromUnity<QSBEyeProxyQuantumMoon, EyeProxyQuantumMoon>((EyeProxyQuantumMoon)unityObject);
			}
			else
			{
				DebugLog.ToConsole($"Warning - couldn't work out type of QuantumObject {unityObject.name}.", MessageType.Warning);
			}

			return worldObj;
		}
	}
}