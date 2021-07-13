using OWML.Utils;
using QSB.Events;
using QSB.Patches;
using QSB.Player;
using QSB.Utility;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QSB.DeathSync
{
	internal class RespawnManager : MonoBehaviour
	{
		public static RespawnManager Instance;

		public bool RespawnNeeded => _playersPendingRespawn.Count != 0;

		private List<PlayerInfo> _playersPendingRespawn = new List<PlayerInfo>();
		private NotificationData _previousNotification;

		private void Start()
			=> Instance = this;

		public void TriggerRespawnMap()
		{
			QSBPatchManager.DoPatchType(QSBPatchTypes.RespawnTime);
			QSBCore.UnityEvents.FireOnNextUpdate(() => GlobalMessenger.FireEvent("TriggerObservatoryMap"));
		}

		public void Respawn()
		{
			var mapController = FindObjectOfType<MapController>();
			QSBPatchManager.DoUnpatchType(QSBPatchTypes.RespawnTime);

			var playerSpawner = FindObjectOfType<PlayerSpawner>();
			playerSpawner.DebugWarp(playerSpawner.GetSpawnPoint(SpawnLocation.Ship));

			mapController.GetType().GetAnyMethod("ExitMapView").Invoke(mapController, null);

			var cameraEffectController = Locator.GetPlayerCamera().GetComponent<PlayerCameraEffectController>();
			cameraEffectController.OpenEyes(1f, false);
		}

		public void OnPlayerDeath(PlayerInfo player)
		{
			if (_playersPendingRespawn.Contains(player))
			{
				DebugLog.ToConsole($"Warning - Received death message for player who is already in _playersPendingRespawn!", OWML.Common.MessageType.Warning);
				return;
			}

			player.IsDead = true;

			_playersPendingRespawn.Add(player);
			UpdateRespawnNotification();

			QSBPlayerManager.ChangePlayerVisibility(player.PlayerId, false);
		}

		public void OnPlayerRespawn(PlayerInfo player)
		{
			if (!_playersPendingRespawn.Contains(player))
			{
				DebugLog.ToConsole($"Warning - Received respawn message for player who is not in _playersPendingRespawn!", OWML.Common.MessageType.Warning);
				return;
			}

			player.IsDead = false;

			_playersPendingRespawn.Remove(player);
			UpdateRespawnNotification();

			QSBPlayerManager.ChangePlayerVisibility(player.PlayerId, true);
		}

		public void RespawnSomePlayer()
		{
			var playerToRespawn = _playersPendingRespawn.First();
			QSBEventManager.FireEvent(EventNames.QSBPlayerRespawn, playerToRespawn.PlayerId);
		}

		private void UpdateRespawnNotification()
		{
			NotificationManager.SharedInstance.UnpinNotification(_previousNotification);

			if (_playersPendingRespawn.Count == 0)
			{
				return;
			}

			var data = new NotificationData(NotificationTarget.Player, $"[{_playersPendingRespawn.Count}] PLAYER(S) AWAITING RESPAWN");
			NotificationManager.SharedInstance.PostNotification(data, true);
			_previousNotification = data;
		}
	}
}
