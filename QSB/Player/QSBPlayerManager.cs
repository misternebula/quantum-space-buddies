using OWML.Common;
using QSB.Player.Events;
using QSB.Player.TransformSync;
using QSB.Tools;
using QSB.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QSB.Player
{
	public static class QSBPlayerManager
	{
		public static uint LocalPlayerId
		{
			get
			{
				var localInstance = PlayerTransformSync.LocalInstance;
				if (localInstance == null)
				{
					DebugLog.ToConsole($"Error - Trying to get LocalPlayerId when the local PlayerTransformSync instance is null." +
						$"{Environment.NewLine} Stacktrace : {Environment.StackTrace} ", MessageType.Error);
					return uint.MaxValue;
				}

				if (localInstance.NetIdentity == null)
				{
					DebugLog.ToConsole($"Error - Trying to get LocalPlayerId when the local PlayerTransformSync instance's QNetworkIdentity is null.", MessageType.Error);
					return uint.MaxValue;
				}

				return localInstance.NetIdentity.NetId.Value;
			}
		}

		public static Action<uint> OnRemovePlayer;

		public static PlayerInfo LocalPlayer => GetPlayer(LocalPlayerId);
		public static List<PlayerInfo> PlayerList { get; } = new List<PlayerInfo>();

		private static readonly List<PlayerSyncObject> PlayerSyncObjects = new List<PlayerSyncObject>();

		public static PlayerInfo GetPlayer(uint id)
		{
			if (!QSBNetworkManager.Instance.IsReady)
			{
				DebugLog.ToConsole($"Warning - GetPlayer() (id<{id}>) called when Network Manager not ready! Is a Player Sync Object still active? " +
					$"{Environment.NewLine} Stacktrace : {Environment.StackTrace}", MessageType.Warning);
			}

			if (id == uint.MaxValue || id == 0U)
			{
				return default;
			}

			var player = PlayerList.FirstOrDefault(x => x.PlayerId == id);
			if (player != null)
			{
				return player;
			}

			if (!QSBCore.IsInMultiplayer)
			{
				DebugLog.ToConsole($"Error - Tried to create player id:{id} when not in multiplayer! Stacktrace : {Environment.StackTrace}", MessageType.Error);
				return default;
			}

			DebugLog.DebugWrite($"Create Player : id<{id}> Stacktrace : {Environment.StackTrace}", MessageType.Info);
			player = new PlayerInfo(id);
			PlayerList.Add(player);
			return player;
		}

		public static void RemovePlayer(uint id)
		{
			DebugLog.DebugWrite($"Remove Player : id<{id}> Stacktrace : {Environment.StackTrace}", MessageType.Info);
			PlayerList.RemoveAll(x => x.PlayerId == id);
		}

		public static bool PlayerExists(uint id) =>
			id != uint.MaxValue && PlayerList.Any(x => x.PlayerId == id);

		public static void HandleFullStateMessage(PlayerStateMessage message)
		{
			var player = GetPlayer(message.AboutId);
			player.Name = message.PlayerName;
			player.PlayerStates = message.PlayerState;
			if (LocalPlayer.PlayerStates.IsReady)
			{
				player.UpdateStateObjects();
			}
		}

		public static IEnumerable<T> GetSyncObjects<T>() where T : PlayerSyncObject =>
			PlayerSyncObjects.OfType<T>().Where(x => x != null);

		public static T GetSyncObject<T>(uint id) where T : PlayerSyncObject =>
			GetSyncObjects<T>().FirstOrDefault(x => x != null && x.AttachedNetId == id);

		public static void AddSyncObject(PlayerSyncObject obj) => PlayerSyncObjects.Add(obj);

		public static void RemoveSyncObject(PlayerSyncObject obj) => PlayerSyncObjects.Remove(obj);

		public static bool IsBelongingToLocalPlayer(uint id) => id == LocalPlayerId ||
				PlayerSyncObjects.Any(x => x != null && x.AttachedNetId == id && x.IsLocalPlayer);

		public static List<PlayerInfo> GetPlayersWithCameras(bool includeLocalCamera = true)
		{
			var cameraList = PlayerList.Where(x => x.Camera != null && x.PlayerId != LocalPlayerId).ToList();
			if (includeLocalCamera
				&& LocalPlayer != default
				&& LocalPlayer.Camera != null)
			{
				cameraList.Add(LocalPlayer);
			}

			return cameraList;
		}

		public static Tuple<Flashlight, IEnumerable<QSBFlashlight>> GetPlayerFlashlights()
			=> new Tuple<Flashlight, IEnumerable<QSBFlashlight>>(Locator.GetFlashlight(), PlayerList.Where(x => x.FlashLight != null).Select(x => x.FlashLight));

		public static void ShowAllPlayers()
			=> PlayerList.Where(x => x != LocalPlayer).ToList().ForEach(x => ChangePlayerVisibility(x.PlayerId, true));

		public static void HideAllPlayers()
			=> PlayerList.Where(x => x != LocalPlayer).ToList().ForEach(x => ChangePlayerVisibility(x.PlayerId, false));

		public static void ChangePlayerVisibility(uint playerId, bool visible)
		{
			var player = GetPlayer(playerId);
			if (player.Body == null)
			{
				DebugLog.ToConsole($"Warning - Player {playerId} has a null player model!", MessageType.Warning);
				return;
			}

			foreach (var renderer in player.Body.GetComponentsInChildren<Renderer>())
			{
				renderer.enabled = visible;
			}

			player.Visible = visible;
		}

		public static PlayerInfo GetClosestPlayerToWorldPoint(Vector3 worldPoint, bool includeLocalPlayer) => includeLocalPlayer
				? GetClosestPlayerToWorldPoint(PlayerList, worldPoint)
				: GetClosestPlayerToWorldPoint(PlayerList.Where(x => x != LocalPlayer).ToList(), worldPoint);

		public static PlayerInfo GetClosestPlayerToWorldPoint(List<PlayerInfo> playerList, Vector3 worldPoint)
		{
			if (playerList.Count == 0)
			{
				DebugLog.ToConsole($"Error - Cannot get closest player from empty player list.", MessageType.Error);
				return null;
			}

			return playerList.Where(x => x.PlayerStates.IsReady).OrderBy(x => Vector3.Distance(x.Body.transform.position, worldPoint)).FirstOrDefault();
		}
	}
}