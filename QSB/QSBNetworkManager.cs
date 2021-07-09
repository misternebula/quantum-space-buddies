using OWML.Common;
using OWML.Utils;
using QSB.Animation.Player;
using QSB.Animation.Player.Thrusters;
using QSB.DeathSync;
using QSB.Events;
using QSB.Instruments;
using QSB.OrbSync.TransformSync;
using QSB.Patches;
using QSB.Player;
using QSB.Player.TransformSync;
using QSB.PoolSync;
using QSB.ProbeSync.TransformSync;
using QSB.ShipSync.TransformSync;
using QSB.TimeSync;
using QSB.Utility;
using QSB.WorldSync;
using QuantumUNET;
using QuantumUNET.Components;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace QSB
{
	public class QSBNetworkManager : QNetworkManager
	{
		public static QSBNetworkManager Instance { get; private set; }

		public event Action OnNetworkManagerReady;

		public bool IsReady { get; private set; }
		public GameObject OrbPrefab { get; private set; }
		public GameObject ShipPrefab { get; private set; }

		private const int MaxConnections = 128;
		private const int MaxBufferedPackets = 64;

		private QSBNetworkLobby _lobby;
		private AssetBundle _assetBundle;
		private GameObject _probePrefab;
		private bool _everConnected;

		public new void Awake()
		{
			base.Awake();
			Instance = this;

			_lobby = gameObject.AddComponent<QSBNetworkLobby>();
			_assetBundle = QSBCore.NetworkAssetBundle;

			playerPrefab = _assetBundle.LoadAsset<GameObject>("assets/NETWORK_Player_Body.prefab");
			SetupNetworkId(playerPrefab);
			SetupNetworkTransform(playerPrefab);
			playerPrefab.AddComponent<PlayerTransformSync>();
			playerPrefab.AddComponent<AnimationSync>();
			playerPrefab.AddComponent<CrouchSync>();
			playerPrefab.AddComponent<WakeUpSync>();
			playerPrefab.AddComponent<JetpackAccelerationSync>();
			playerPrefab.AddComponent<InstrumentsManager>();

			ShipPrefab = _assetBundle.LoadAsset<GameObject>("assets/networkship.prefab");
			SetupNetworkId(ShipPrefab);
			SetupNetworkTransform(ShipPrefab);
			ShipPrefab.AddComponent<ShipTransformSync>();
			spawnPrefabs.Add(ShipPrefab);

			_probePrefab = _assetBundle.LoadAsset<GameObject>("assets/networkprobe.prefab");
			SetupNetworkId(_probePrefab);
			SetupNetworkTransform(_probePrefab);
			_probePrefab.AddComponent<PlayerProbeSync>();
			spawnPrefabs.Add(_probePrefab);

			OrbPrefab = _assetBundle.LoadAsset<GameObject>("assets/networkorb.prefab");
			SetupNetworkId(OrbPrefab);
			SetupNetworkTransform(OrbPrefab);
			OrbPrefab.AddComponent<NomaiOrbTransformSync>();
			spawnPrefabs.Add(OrbPrefab);

			ConfigureNetworkManager();
		}

		private void SetupNetworkId(GameObject go)
		{
			var ident = go.AddComponent<QNetworkIdentity>();
			ident.LocalPlayerAuthority = true;
			ident.SetValue("m_AssetId", go.GetComponent<NetworkIdentity>().assetId);
			ident.SetValue("m_SceneId", go.GetComponent<NetworkIdentity>().sceneId);
		}

		private void SetupNetworkTransform(GameObject go)
		{
			foreach (var item in go.GetComponents<NetworkTransformChild>())
			{
				var child = go.AddComponent<QNetworkTransformChild>();
				child.Target = item.target;
				Destroy(item);
			}

			Destroy(go.GetComponent<NetworkTransform>());
			Destroy(go.GetComponent<NetworkIdentity>());
		}

		private void ConfigureNetworkManager()
		{
			networkAddress = QSBCore.DefaultServerIP;
			networkPort = QSBCore.Port;
			maxConnections = MaxConnections;
			customConfig = true;
			connectionConfig.AddChannel(QosType.Reliable);
			connectionConfig.AddChannel(QosType.Unreliable);
			this.SetValue("m_MaxBufferedPackets", MaxBufferedPackets);
			channels.Add(QosType.Reliable);
			channels.Add(QosType.Unreliable);

			DebugLog.DebugWrite("Network Manager ready.", MessageType.Success);
		}

		public override void OnStartServer()
		{
			DebugLog.DebugWrite("OnStartServer", MessageType.Info);
			if (QSBWorldSync.OldDialogueTrees.Count == 0 && QSBSceneManager.IsInUniverse)
			{
				QSBWorldSync.OldDialogueTrees = Resources.FindObjectsOfTypeAll<CharacterDialogueTree>().ToList();
			}
		}

		public override void OnServerAddPlayer(QNetworkConnection connection, short playerControllerId) // Called on the server when a client joins
		{
			DebugLog.DebugWrite($"OnServerAddPlayer {playerControllerId}", MessageType.Info);
			base.OnServerAddPlayer(connection, playerControllerId);

			QNetworkServer.SpawnWithClientAuthority(Instantiate(_probePrefab), connection);
		}

		public override void OnStartClient(QNetworkClient _)
		{
			DebugLog.DebugWrite($"Setting defaultServerIP to {networkAddress}");
			var config = QSBCore.Helper.Config;
			config.SetSettingsValue("defaultServerIP", networkAddress);
			QSBCore.Helper.Storage.Save(config, Constants.ModConfigFileName);
		}

		public override void OnClientConnect(QNetworkConnection connection) // Called on the client when connecting to a server
		{
			DebugLog.DebugWrite("OnClientConnect", MessageType.Info);
			base.OnClientConnect(connection);

			QSBEventManager.Init();

			gameObject.AddComponent<RespawnOnDeath>();

			if (QSBSceneManager.IsInUniverse)
			{
				WorldObjectManager.Rebuild(QSBSceneManager.CurrentScene);
			}

			var specificType = QNetworkServer.active ? QSBPatchTypes.OnServerClientConnect : QSBPatchTypes.OnNonServerClientConnect;
			QSBPatchManager.DoPatchType(specificType);
			QSBPatchManager.DoPatchType(QSBPatchTypes.OnClientConnect);

			_lobby.CanEditName = false;

			OnNetworkManagerReady?.SafeInvoke();
			IsReady = true;

			QSBCore.UnityEvents.RunWhen(() => QSBEventManager.Ready && PlayerTransformSync.LocalInstance != null,
				() => QSBEventManager.FireEvent(EventNames.QSBPlayerJoin, _lobby.PlayerName));

			if (!QSBCore.IsServer)
			{
				QSBCore.UnityEvents.RunWhen(() => QSBEventManager.Ready && PlayerTransformSync.LocalInstance != null,
				() => QSBEventManager.FireEvent(EventNames.QSBPlayerStatesRequest));
			}

			_everConnected = true;
		}

		public override void OnStopClient() // Called on the client when closing connection
		{
			DebugLog.DebugWrite("OnStopClient", MessageType.Info);
			DebugLog.ToConsole("Disconnecting from server...", MessageType.Info);
			Destroy(GetComponent<RespawnOnDeath>());
			QSBEventManager.Reset();
			QSBPlayerManager.PlayerList.ForEach(player => player.HudMarker?.Remove());

			RemoveWorldObjects();
			NomaiOrbTransformSync.OrbTransformSyncs.Clear();
			QSBWorldSync.OldDialogueTrees.Clear();

			if (_everConnected)
			{
				var specificType = QNetworkServer.active ? QSBPatchTypes.OnServerClientConnect : QSBPatchTypes.OnNonServerClientConnect;
				QSBPatchManager.DoUnpatchType(specificType);
				QSBPatchManager.DoUnpatchType(QSBPatchTypes.OnClientConnect);
			}

			_lobby.CanEditName = true;

			IsReady = false;
			_everConnected = false;
		}

		public override void OnServerDisconnect(QNetworkConnection connection) // Called on the server when any client disconnects
		{
			base.OnServerDisconnect(connection);
			DebugLog.DebugWrite("OnServerDisconnect", MessageType.Info);

			foreach (var item in NomaiOrbTransformSync.OrbTransformSyncs)
			{
				var identity = item.GetComponent<QNetworkIdentity>();
				if (identity.ClientAuthorityOwner == connection)
				{
					identity.RemoveClientAuthority(connection);
				}
			}
		}

		public override void OnStopServer()
		{
			DebugLog.DebugWrite("OnStopServer", MessageType.Info);
			Destroy(GetComponent<RespawnOnDeath>());
			QSBEventManager.Reset();
			DebugLog.ToConsole("Server stopped!", MessageType.Info);
			QSBPlayerManager.PlayerList.ForEach(player => player.HudMarker?.Remove());

			base.OnStopServer();
		}

		private void RemoveWorldObjects()
		{
			QSBWorldSync.RemoveWorldObjects<IWorldObjectTypeSubset>();
			QSBWorldSync.RemoveWorldObjects<IWorldObject>();
			foreach (var platform in Resources.FindObjectsOfTypeAll<CustomNomaiRemoteCameraPlatform>())
			{
				Destroy(platform);
			}

			foreach (var camera in Resources.FindObjectsOfTypeAll<CustomNomaiRemoteCamera>())
			{
				Destroy(camera);
			}

			foreach (var streaming in Resources.FindObjectsOfTypeAll<CustomNomaiRemoteCameraStreaming>())
			{
				Destroy(streaming);
			}
		}
	}
}