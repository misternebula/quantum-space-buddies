using QuantumUNET.Components;
using QuantumUNET.Logging;
using QuantumUNET.Messages;
using QuantumUNET.Transport;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Types;

namespace QuantumUNET
{
	public class QNetworkServer
	{
		private QNetworkServer()
		{
			NetworkTransport.Init();
			m_RemoveList = new HashSet<NetworkInstanceId>();
			m_ExternalConnections = new HashSet<int>();
			m_NetworkScene = new QNetworkScene();
			m_SimpleServerSimple = new ServerSimpleWrapper(this);
		}

		public static List<QNetworkConnection> localConnections => instance.m_LocalConnectionsFakeList;

		public static int listenPort => instance.m_SimpleServerSimple.listenPort;

		public static int serverHostId => instance.m_SimpleServerSimple.serverHostId;

		public static ReadOnlyCollection<QNetworkConnection> connections => instance.m_SimpleServerSimple.connections;

		public static Dictionary<short, QNetworkMessageDelegate> handlers => instance.m_SimpleServerSimple.handlers;

		public static HostTopology hostTopology => instance.m_SimpleServerSimple.hostTopology;

		public static Dictionary<NetworkInstanceId, QNetworkIdentity> objects => instance.m_NetworkScene.localObjects;

		public static bool dontListen { get; set; }

		public static bool useWebSockets
		{
			get => instance.m_SimpleServerSimple.useWebSockets;
			set => instance.m_SimpleServerSimple.useWebSockets = value;
		}

		internal static QNetworkServer instance
		{
			get
			{
				if (s_Instance == null)
				{
					var obj = s_Sync;
					lock (obj)
					{
						if (s_Instance == null)
						{
							s_Instance = new QNetworkServer();
						}
					}
				}

				return s_Instance;
			}
		}

		public static bool active { get; private set; }

		public static bool localClientActive => instance.m_LocalClientActive;

		public static int numChannels => instance.m_SimpleServerSimple.hostTopology.DefaultConfig.ChannelCount;

		public static float maxDelay
		{
			get => instance.m_MaxDelay;
			set => instance.InternalSetMaxDelay(value);
		}

		public static Type networkConnectionClass => instance.m_SimpleServerSimple.networkConnectionClass;

		public static void SetNetworkConnectionClass<T>() where T : QNetworkConnection => instance.m_SimpleServerSimple.SetNetworkConnectionClass<T>();

		public static bool Configure(ConnectionConfig config, int maxConnections) => instance.m_SimpleServerSimple.Configure(config, maxConnections);

		public static bool Configure(HostTopology topology) => instance.m_SimpleServerSimple.Configure(topology);

		public static void Reset()
		{
			NetworkTransport.Shutdown();
			NetworkTransport.Init();
			s_Instance = null;
			active = false;
		}

		public static void Shutdown()
		{
			if (s_Instance != null)
			{
				s_Instance.InternalDisconnectAll();
				if (!dontListen)
				{
					s_Instance.m_SimpleServerSimple.Stop();
				}

				s_Instance = null;
			}

			dontListen = false;
			active = false;
		}

		internal void RegisterMessageHandlers()
		{
			m_SimpleServerSimple.RegisterHandlerSafe(QMsgType.ClientUpdateVars, OnUpdateVarsMessage);
			m_SimpleServerSimple.RegisterHandlerSafe(QMsgType.Ready, OnClientReadyMessage);
			m_SimpleServerSimple.RegisterHandlerSafe(QMsgType.Command, OnCommandMessage);
			m_SimpleServerSimple.RegisterHandlerSafe(QMsgType.LocalPlayerTransform, QNetworkTransform.HandleTransform);
			m_SimpleServerSimple.RegisterHandlerSafe(QMsgType.LocalChildTransform, QNetworkTransformChild.HandleChildTransform);
			m_SimpleServerSimple.RegisterHandlerSafe(QMsgType.RemovePlayer, OnRemovePlayerMessage);
			m_SimpleServerSimple.RegisterHandlerSafe(QMsgType.Animation, QNetworkAnimator.OnAnimationServerMessage);
			m_SimpleServerSimple.RegisterHandlerSafe(QMsgType.AnimationParameters, QNetworkAnimator.OnAnimationParametersServerMessage);
			m_SimpleServerSimple.RegisterHandlerSafe(QMsgType.AnimationTrigger, QNetworkAnimator.OnAnimationTriggerServerMessage);
			maxPacketSize = hostTopology.DefaultConfig.PacketSize;
		}

		private static void OnUpdateVarsMessage(QNetworkMessage netMsg)
		{
			var networkInstanceId = netMsg.Reader.ReadNetworkId();
			var typeName = netMsg.Reader.ReadString();
			if (instance.m_NetworkScene.GetNetworkIdentity(networkInstanceId, out var networkIdentity))
			{
				var allBehaviours = networkIdentity.GetNetworkBehaviours();
				var target = allBehaviours.First(x => x.GetType().Name == typeName);
				target.OnDeserialize(netMsg.Reader, false);
			}
			else
			{
				Debug.LogError($"Did not find target for sync message for {networkInstanceId}");
			}
		}

		public static void ListenRelay(string relayIp, int relayPort, NetworkID netGuid, SourceID sourceId, NodeID nodeId) => instance.InternalListenRelay(relayIp, relayPort, netGuid, sourceId, nodeId);

		private void InternalListenRelay(string relayIp, int relayPort, NetworkID netGuid, SourceID sourceId, NodeID nodeId)
		{
			m_SimpleServerSimple.ListenRelay(relayIp, relayPort, netGuid, sourceId, nodeId);
			active = true;
			RegisterMessageHandlers();
		}

		public static bool Listen(int serverPort) => instance.InternalListen(null, serverPort);

		public static bool Listen(string ipAddress, int serverPort) => instance.InternalListen(ipAddress, serverPort);

		internal bool InternalListen(string ipAddress, int serverPort)
		{
			if (dontListen)
			{
				m_SimpleServerSimple.Initialize();
			}
			else if (!m_SimpleServerSimple.Listen(ipAddress, serverPort))
			{
				return false;
			}

			maxPacketSize = hostTopology.DefaultConfig.PacketSize;
			active = true;
			RegisterMessageHandlers();
			return true;
		}

		private void InternalSetMaxDelay(float seconds)
		{
			foreach (var networkConnection in connections)
			{
				networkConnection?.SetMaxDelay(seconds);
			}

			m_MaxDelay = seconds;
		}

		internal int AddLocalClient(QLocalClient localClient)
		{
			int result;
			if (m_LocalConnectionsFakeList.Count != 0)
			{
				QLog.Error("Local Connection already exists");
				result = -1;
			}
			else
			{
				m_LocalConnection = new QULocalConnectionToClient(localClient)
				{
					connectionId = 0
				};
				m_SimpleServerSimple.SetConnectionAtIndex(m_LocalConnection);
				m_LocalConnectionsFakeList.Add(m_LocalConnection);
				m_LocalConnection.InvokeHandlerNoData(32);
				result = 0;
			}

			return result;
		}

		internal void RemoveLocalClient(QNetworkConnection localClientConnection)
		{
			for (var i = 0; i < m_LocalConnectionsFakeList.Count; i++)
			{
				if (m_LocalConnectionsFakeList[i].connectionId == localClientConnection.connectionId)
				{
					m_LocalConnectionsFakeList.RemoveAt(i);
					break;
				}
			}

			if (m_LocalConnection != null)
			{
				m_LocalConnection.Disconnect();
				m_LocalConnection.Dispose();
				m_LocalConnection = null;
			}

			m_LocalClientActive = false;
			m_SimpleServerSimple.RemoveConnectionAtIndex(0);
		}

		internal void SetLocalObjectOnServer(NetworkInstanceId netId, GameObject obj)
		{
			QLog.Debug($"SetLocalObjectOnServer {netId} {obj}");
			m_NetworkScene.SetLocalObject(netId, obj, false, true);
		}

		internal void ActivateLocalClientScene()
		{
			if (!m_LocalClientActive)
			{
				m_LocalClientActive = true;
				foreach (var networkIdentity in objects.Values)
				{
					if (!networkIdentity.IsClient)
					{
						QLog.Log($"ActivateClientScene {networkIdentity.NetId} {networkIdentity.gameObject}");
						QClientScene.SetLocalObject(networkIdentity.NetId, networkIdentity.gameObject);
						networkIdentity.OnStartClient();
					}
				}
			}
		}

		public static bool SendToAll(short msgType, QMessageBase msg)
		{
			QLog.Debug($"Server.SendToAll msgType:{msgType}");
			var flag = true;
			foreach (var networkConnection in connections)
			{
				if (networkConnection != null)
				{
					flag &= networkConnection.Send(msgType, msg);
				}
			}

			return flag;
		}

		private static bool SendToObservers(GameObject contextObj, short msgType, QMessageBase msg)
		{
			QLog.Debug($"Server.SendToObservers id:{msgType}");
			var flag = true;
			var component = contextObj.GetComponent<QNetworkIdentity>();
			bool result;
			if (component == null || component.Observers == null)
			{
				result = false;
			}
			else
			{
				var count = component.Observers.Count;
				for (var i = 0; i < count; i++)
				{
					var networkConnection = component.Observers[i];
					flag &= networkConnection.Send(msgType, msg);
				}

				result = flag;
			}

			return result;
		}

		public static bool SendToReady(GameObject contextObj, short msgType, QMessageBase msg)
		{
			QLog.Debug($"Server.SendToReady id:{msgType}");
			bool result;
			if (contextObj == null)
			{
				foreach (var networkConnection in connections)
				{
					if (networkConnection != null && networkConnection.isReady)
					{
						networkConnection.Send(msgType, msg);
					}
				}

				result = true;
			}
			else
			{
				var flag = true;
				var component = contextObj.GetComponent<QNetworkIdentity>();
				if (component == null || component.Observers == null)
				{
					result = false;
				}
				else
				{
					var count = component.Observers.Count;
					for (var j = 0; j < count; j++)
					{
						var networkConnection2 = component.Observers[j];
						if (networkConnection2.isReady)
						{
							flag &= networkConnection2.Send(msgType, msg);
						}
					}

					result = flag;
				}
			}

			return result;
		}

		public static void SendWriterToReady(GameObject contextObj, QNetworkWriter writer, int channelId)
		{
			var arraySegment = writer.AsArraySegment();
			if (arraySegment.Count > 32767)
			{
				throw new UnityException("NetworkWriter used buffer is too big!");
			}

			SendBytesToReady(contextObj, arraySegment.Array, arraySegment.Count, channelId);
		}

		public static void SendBytesToReady(GameObject contextObj, byte[] buffer, int numBytes, int channelId)
		{
			if (contextObj == null)
			{
				var flag = true;
				foreach (var networkConnection in connections)
				{
					if (networkConnection != null && networkConnection.isReady)
					{
						if (!networkConnection.SendBytes(buffer, numBytes, channelId))
						{
							flag = false;
						}
					}
				}

				if (!flag)
				{
					QLog.Error("SendBytesToReady failed");
				}
			}
			else
			{
				var component = contextObj.GetComponent<QNetworkIdentity>();
				try
				{
					var flag2 = true;
					var count = component.Observers.Count;
					for (var j = 0; j < count; j++)
					{
						var networkConnection2 = component.Observers[j];
						if (networkConnection2.isReady)
						{
							if (!networkConnection2.SendBytes(buffer, numBytes, channelId))
							{
								flag2 = false;
							}
						}
					}

					if (!flag2)
					{
						QLog.Error($"SendBytesToReady failed for {contextObj}");
					}
				}
				catch (NullReferenceException)
				{
					QLog.Error($"SendBytesToReady object {contextObj} has not been spawned");
				}
			}
		}

		public static void SendBytesToPlayer(GameObject player, byte[] buffer, int numBytes, int channelId)
		{
			foreach (var networkConnection in connections)
			{
				if (networkConnection != null)
				{
					foreach (var controller in networkConnection.PlayerControllers)
					{
						if (controller.IsValid && controller.Gameobject == player)
						{
							networkConnection.SendBytes(buffer, numBytes, channelId);
							break;
						}
					}
				}
			}
		}

		public static bool SendUnreliableToAll(short msgType, QMessageBase msg)
		{
			QLog.Log($"Server.SendUnreliableToAll msgType:{msgType}");
			var flag = true;
			foreach (var networkConnection in connections)
			{
				if (networkConnection != null)
				{
					flag &= networkConnection.SendUnreliable(msgType, msg);
				}
			}

			return flag;
		}

		public static bool SendUnreliableToReady(GameObject contextObj, short msgType, QMessageBase msg)
		{
			QLog.Log($"Server.SendUnreliableToReady id:{msgType}");
			bool result;
			if (contextObj == null)
			{
				foreach (var networkConnection in connections)
				{
					if (networkConnection != null && networkConnection.isReady)
					{
						networkConnection.SendUnreliable(msgType, msg);
					}
				}

				result = true;
			}
			else
			{
				var flag = true;
				var component = contextObj.GetComponent<QNetworkIdentity>();
				var count = component.Observers.Count;
				for (var j = 0; j < count; j++)
				{
					var networkConnection2 = component.Observers[j];
					if (networkConnection2.isReady)
					{
						flag &= networkConnection2.SendUnreliable(msgType, msg);
					}
				}

				result = flag;
			}

			return result;
		}

		public static bool SendByChannelToAll(short msgType, QMessageBase msg, int channelId)
		{
			QLog.Log($"Server.SendByChannelToAll id:{msgType}");
			var flag = true;
			foreach (var networkConnection in connections)
			{
				if (networkConnection != null)
				{
					flag &= networkConnection.SendByChannel(msgType, msg, channelId);
				}
			}

			return flag;
		}

		public static bool SendByChannelToReady(GameObject contextObj, short msgType, QMessageBase msg, int channelId)
		{
			QLog.Log($"Server.SendByChannelToReady msgType:{msgType}");
			bool result;
			if (contextObj == null)
			{
				foreach (var networkConnection in connections)
				{
					if (networkConnection != null && networkConnection.isReady)
					{
						networkConnection.SendByChannel(msgType, msg, channelId);
					}
				}

				result = true;
			}
			else
			{
				var flag = true;
				var component = contextObj.GetComponent<QNetworkIdentity>();
				var count = component.Observers.Count;
				for (var j = 0; j < count; j++)
				{
					var networkConnection2 = component.Observers[j];
					if (networkConnection2.isReady)
					{
						flag &= networkConnection2.SendByChannel(msgType, msg, channelId);
					}
				}

				result = flag;
			}

			return result;
		}

		public static void DisconnectAll() => instance.InternalDisconnectAll();

		internal void InternalDisconnectAll()
		{
			m_SimpleServerSimple.DisconnectAllConnections();
			if (m_LocalConnection != null)
			{
				m_LocalConnection.Disconnect();
				m_LocalConnection.Dispose();
				m_LocalConnection = null;
			}

			m_LocalClientActive = false;
		}

		internal static void Update() => s_Instance?.InternalUpdate();

		private void UpdateServerObjects()
		{
			foreach (var networkIdentity in objects.Values)
			{
				try
				{
					networkIdentity.UNetUpdate();
				}
				catch (NullReferenceException)
				{
				}
				catch (MissingReferenceException)
				{
				}
			}

			if (m_RemoveListCount++ % 100 == 0)
			{
				CheckForNullObjects();
			}
		}

		private void CheckForNullObjects()
		{
			foreach (var networkInstanceId in objects.Keys)
			{
				var networkIdentity = objects[networkInstanceId];
				if (networkIdentity == null || networkIdentity.gameObject == null)
				{
					m_RemoveList.Add(networkInstanceId);
				}
			}

			if (m_RemoveList.Count > 0)
			{
				foreach (var key in m_RemoveList)
				{
					objects.Remove(key);
				}

				m_RemoveList.Clear();
			}
		}

		internal void InternalUpdate()
		{
			m_SimpleServerSimple.Update();
			if (dontListen)
			{
				m_SimpleServerSimple.UpdateConnections();
			}

			UpdateServerObjects();
		}

		private void OnConnected(QNetworkConnection conn)
		{
			QLog.Log($"Server accepted client:{conn.connectionId}");
			conn.SetMaxDelay(m_MaxDelay);
			conn.InvokeHandlerNoData(32);
			SendCrc(conn);
		}

		private void OnDisconnected(QNetworkConnection conn)
		{
			conn.InvokeHandlerNoData(33);
			foreach (var controller in conn.PlayerControllers)
			{
				if (controller.Gameobject != null)
				{
					QLog.Warning("Player not destroyed when connection disconnected.");
				}
			}

			QLog.Log($"Server lost client:{conn.connectionId}");
			conn.RemoveObservers();
			conn.Dispose();
		}

		private void OnData(QNetworkConnection conn, int receivedSize, int channelId) => conn.TransportReceive(m_SimpleServerSimple.messageBuffer, receivedSize, channelId);

		private void GenerateConnectError(int error)
		{
			QLog.Error($"UNet Server Connect Error: {error}");
			GenerateError(null, error);
		}

		private void GenerateDataError(QNetworkConnection conn, int error)
		{
			QLog.Error($"UNet Server Data Error: {(NetworkError)error}");
			GenerateError(conn, error);
		}

		private void GenerateDisconnectError(QNetworkConnection conn, int error)
		{
			QLog.Error($"UNet Server Disconnect Error: {(NetworkError)error} conn:[{conn}]:{conn.connectionId}");
			GenerateError(conn, error);
		}

		private void GenerateError(QNetworkConnection conn, int error)
		{
			if (handlers.ContainsKey(34))
			{
				var errorMessage = new QErrorMessage
				{
					errorCode = error
				};
				var writer = new QNetworkWriter();
				errorMessage.Serialize(writer);
				var reader = new QNetworkReader(writer);
				conn.InvokeHandler(34, reader, 0);
			}
		}

		public static void RegisterHandler(short msgType, QNetworkMessageDelegate handler) => instance.m_SimpleServerSimple.RegisterHandler(msgType, handler);

		public static void UnregisterHandler(short msgType) => instance.m_SimpleServerSimple.UnregisterHandler(msgType);

		public static void ClearHandlers() => instance.m_SimpleServerSimple.ClearHandlers();

		public static void ClearSpawners() => QNetworkScene.ClearSpawners();

		public static void GetStatsOut(out int numMsgs, out int numBufferedMsgs, out int numBytes, out int lastBufferedPerSecond)
		{
			numMsgs = 0;
			numBufferedMsgs = 0;
			numBytes = 0;
			lastBufferedPerSecond = 0;
			foreach (var networkConnection in connections)
			{
				if (networkConnection != null)
				{
					networkConnection.GetStatsOut(out var num, out var num2, out var num3, out var num4);
					numMsgs += num;
					numBufferedMsgs += num2;
					numBytes += num3;
					lastBufferedPerSecond += num4;
				}
			}
		}

		public static void GetStatsIn(out int numMsgs, out int numBytes)
		{
			numMsgs = 0;
			numBytes = 0;
			foreach (var networkConnection in connections)
			{
				if (networkConnection != null)
				{
					networkConnection.GetStatsIn(out var num, out var num2);
					numMsgs += num;
					numBytes += num2;
				}
			}
		}

		public static void SendToClientOfPlayer(GameObject player, short msgType, QMessageBase msg)
		{
			foreach (var networkConnection in connections)
			{
				if (networkConnection != null)
				{
					foreach (var controller in networkConnection.PlayerControllers)
					{
						if (controller.IsValid && controller.Gameobject == player)
						{
							networkConnection.Send(msgType, msg);
							return;
						}
					}
				}
			}

			QLog.Error($"Failed to send message to player object '{player.name}, not found in connection list");
		}

		public static void SendToClient(int connectionId, short msgType, QMessageBase msg)
		{
			if (connectionId < connections.Count)
			{
				var networkConnection = connections[connectionId];
				if (networkConnection != null)
				{
					networkConnection.Send(msgType, msg);
					return;
				}
			}

			QLog.Error($"Failed to send message to connection ID '{connectionId}, not found in connection list");
		}

		public static bool AddPlayerForConnection(QNetworkConnection conn, GameObject player, short playerControllerId) => instance.InternalAddPlayerForConnection(conn, player, playerControllerId);

		internal bool InternalAddPlayerForConnection(QNetworkConnection conn, GameObject playerGameObject, short playerControllerId)
		{
			bool result;
			if (!GetNetworkIdentity(playerGameObject, out var networkIdentity))
			{
				QLog.Log(
					$"AddPlayer: playerGameObject has no NetworkIdentity. Please add a NetworkIdentity to {playerGameObject}");
				result = false;
			}
			else
			{
				networkIdentity.Reset();
				if (!CheckPlayerControllerIdForConnection(conn, playerControllerId))
				{
					result = false;
				}
				else
				{
					GameObject x = null;
					if (conn.GetPlayerController(playerControllerId, out var playerController))
					{
						x = playerController.Gameobject;
					}

					if (x != null)
					{
						QLog.Log(
							$"AddPlayer: player object already exists for playerControllerId of {playerControllerId}");
						result = false;
					}
					else
					{
						var playerController2 = new QPlayerController(playerGameObject, playerControllerId);
						conn.SetPlayerController(playerController2);
						networkIdentity.SetConnectionToClient(conn, playerController2.PlayerControllerId);
						SetClientReady(conn);
						if (SetupLocalPlayerForConnection(conn, networkIdentity, playerController2))
						{
							result = true;
						}
						else
						{
							QLog.Log(
								$"Adding new playerGameObject object netId: {playerGameObject.GetComponent<QNetworkIdentity>().NetId} asset ID {playerGameObject.GetComponent<QNetworkIdentity>().AssetId}");
							FinishPlayerForConnection(conn, networkIdentity, playerGameObject);
							if (networkIdentity.LocalPlayerAuthority)
							{
								networkIdentity.SetClientOwner(conn);
							}

							result = true;
						}
					}
				}
			}

			return result;
		}

		private static bool CheckPlayerControllerIdForConnection(QNetworkConnection conn, short playerControllerId)
		{
			bool result;
			if (playerControllerId < 0)
			{
				QLog.Error($"AddPlayer: playerControllerId of {playerControllerId} is negative");
				result = false;
			}
			else if (playerControllerId > 32)
			{
				QLog.Log($"AddPlayer: playerControllerId of {playerControllerId} is too high. max is {32}");
				result = false;
			}
			else
			{
				if (playerControllerId > 16)
				{
					QLog.Warning($"AddPlayer: playerControllerId of {playerControllerId} is unusually high");
				}

				result = true;
			}

			return result;
		}

		private bool SetupLocalPlayerForConnection(QNetworkConnection conn, QNetworkIdentity uv, QPlayerController newPlayerController)
		{
			QLog.Log($"NetworkServer SetupLocalPlayerForConnection netID:{uv.NetId}");
			bool result;
			if (conn is QULocalConnectionToClient ulocalConnectionToClient)
			{
				QLog.Log("NetworkServer AddPlayer handling ULocalConnectionToClient");
				if (uv.NetId.IsEmpty())
				{
					uv.OnStartServer(true);
				}

				uv.RebuildObservers(true);
				SendSpawnMessage(uv, null);
				ulocalConnectionToClient.LocalClient.AddLocalPlayer(newPlayerController);
				uv.SetClientOwner(conn);
				uv.ForceAuthority(true);
				uv.SetLocalPlayer(newPlayerController.PlayerControllerId);
				result = true;
			}
			else
			{
				result = false;
			}

			return result;
		}

		private static void FinishPlayerForConnection(QNetworkConnection conn, QNetworkIdentity uv, GameObject playerGameObject)
		{
			if (uv.NetId.IsEmpty())
			{
				Spawn(playerGameObject);
			}

			conn.Send(4, new QOwnerMessage
			{
				NetId = uv.NetId,
				PlayerControllerId = uv.PlayerControllerId
			});
		}

		private static bool GetNetworkIdentity(GameObject go, out QNetworkIdentity view)
		{
			view = go.GetComponent<QNetworkIdentity>();
			bool result;
			if (view == null)
			{
				QLog.Error("UNET failure. GameObject doesn't have NetworkIdentity.");
				result = false;
			}
			else
			{
				result = true;
			}

			return result;
		}

		public static void SetClientReady(QNetworkConnection conn) => instance.SetClientReadyInternal(conn);

		internal void SetClientReadyInternal(QNetworkConnection conn)
		{
			QLog.Log($"SetClientReadyInternal for conn:{conn.connectionId}");
			if (conn.isReady)
			{
				QLog.Log($"SetClientReady conn {conn.connectionId} already ready");
			}
			else
			{
				if (conn.PlayerControllers.Count == 0)
				{
					QLog.Warning("Ready with no player object");
				}

				conn.isReady = true;
				if (conn is QULocalConnectionToClient)
				{
					QLog.Log("NetworkServer Ready handling ULocalConnectionToClient");
					foreach (var networkIdentity in objects.Values)
					{
						if (networkIdentity != null && networkIdentity.gameObject != null)
						{
							var flag = networkIdentity.OnCheckObserver(conn);
							if (flag)
							{
								networkIdentity.AddObserver(conn);
							}

							if (!networkIdentity.IsClient)
							{
								QLog.Log("LocalClient.SetSpawnObject calling OnStartClient");
								networkIdentity.OnStartClient();
							}
						}
					}
				}
				else
				{
					QLog.Log($"Spawning {objects.Count} objects for conn {conn.connectionId}");
					var objectSpawnFinishedMessage = new QObjectSpawnFinishedMessage
					{
						State = 0U
					};
					conn.Send(12, objectSpawnFinishedMessage);
					foreach (var networkIdentity2 in objects.Values)
					{
						if (networkIdentity2 == null)
						{
							QLog.Warning("Invalid object found in server local object list (null NetworkIdentity).");
						}
						else if (networkIdentity2.gameObject.activeSelf)
						{
							QLog.Debug(
								$"Sending spawn message for current server objects name='{networkIdentity2.gameObject.name}' netId={networkIdentity2.NetId}");
							var flag2 = networkIdentity2.OnCheckObserver(conn);
							if (flag2)
							{
								networkIdentity2.AddObserver(conn);
							}
						}
					}

					objectSpawnFinishedMessage.State = 1U;
					conn.Send(12, objectSpawnFinishedMessage);
				}
			}
		}

		internal static void ShowForConnection(QNetworkIdentity uv, QNetworkConnection conn)
		{
			if (conn.isReady)
			{
				instance.SendSpawnMessage(uv, conn);
			}
		}

		internal static void HideForConnection(QNetworkIdentity uv, QNetworkConnection conn) => conn.Send(13, new QObjectDestroyMessage
		{
			NetId = uv.NetId
		});

		public static void SetAllClientsNotReady()
		{
			foreach (var networkConnection in connections)
			{
				if (networkConnection != null)
				{
					SetClientNotReady(networkConnection);
				}
			}
		}

		public static void SetClientNotReady(QNetworkConnection conn) => instance.InternalSetClientNotReady(conn);

		internal void InternalSetClientNotReady(QNetworkConnection conn)
		{
			if (conn.isReady)
			{
				QLog.Log($"PlayerNotReady {conn}");
				conn.isReady = false;
				conn.RemoveObservers();
				var msg = new QNotReadyMessage();
				conn.Send(36, msg);
			}
		}

		private static void OnClientReadyMessage(QNetworkMessage netMsg)
		{
			QLog.Log($"Default handler for ready message from {netMsg.Connection}");
			SetClientReady(netMsg.Connection);
		}

		private static void OnRemovePlayerMessage(QNetworkMessage netMsg)
		{
			netMsg.ReadMessage(s_RemovePlayerMessage);
			netMsg.Connection.GetPlayerController(s_RemovePlayerMessage.PlayerControllerId, out var playerController);
			if (playerController != null)
			{
				netMsg.Connection.RemovePlayerController(s_RemovePlayerMessage.PlayerControllerId);
				Destroy(playerController.Gameobject);
			}
			else
			{
				QLog.Error(
					$"Received remove player message but could not find the player ID: {s_RemovePlayerMessage.PlayerControllerId}");
			}
		}

		private static void OnCommandMessage(QNetworkMessage netMsg)
		{
			var cmdHash = (int)netMsg.Reader.ReadPackedUInt32();
			var networkInstanceId = netMsg.Reader.ReadNetworkId();
			var gameObject = FindLocalObject(networkInstanceId);
			if (gameObject == null)
			{
				QLog.Warning($"Instance not found when handling Command message [netId={networkInstanceId}]");
			}
			else
			{
				var component = gameObject.GetComponent<QNetworkIdentity>();
				if (component == null)
				{
					QLog.Warning(
						$"NetworkIdentity deleted when handling Command message [netId={networkInstanceId}]");
				}
				else
				{
					var flag = false;
					foreach (var playerController in netMsg.Connection.PlayerControllers)
					{
						if (playerController.Gameobject != null && playerController.Gameobject.GetComponent<QNetworkIdentity>().NetId == component.NetId)
						{
							flag = true;
							break;
						}
					}

					if (!flag)
					{
						if (component.ClientAuthorityOwner != netMsg.Connection)
						{
							QLog.Warning($"Command for object without authority [netId={networkInstanceId}]");
							return;
						}
					}

					QLog.Log($"OnCommandMessage for netId={networkInstanceId} conn={netMsg.Connection}");
					component.HandleCommand(cmdHash, netMsg.Reader);
				}
			}
		}

		internal void SpawnObject(GameObject obj)
		{
			if (!active)
			{
				QLog.Error(
					$"SpawnObject for {obj}, NetworkServer is not active. Cannot spawn objects without an active server.");
			}
			else if (!GetNetworkIdentity(obj, out var networkIdentity))
			{
				QLog.Error($"SpawnObject {obj} has no QSBNetworkIdentity. Please add a NetworkIdentity to {obj}");
			}
			else
			{
				networkIdentity.Reset();
				networkIdentity.OnStartServer(false);
				networkIdentity.RebuildObservers(true);
			}
		}

		internal void SendSpawnMessage(QNetworkIdentity uv, QNetworkConnection conn)
		{
			if (!uv.ServerOnly)
			{
				if (uv.SceneId.IsEmpty())
				{
					var objectSpawnMessage = new QObjectSpawnMessage
					{
						NetId = uv.NetId,
						assetId = uv.AssetId,
						Position = uv.transform.position,
						Rotation = uv.transform.rotation
					};
					var networkWriter = new QNetworkWriter();
					uv.UNetSerializeAllVars(networkWriter);
					if (networkWriter.Position > 0)
					{
						objectSpawnMessage.Payload = networkWriter.ToArray();
					}

					if (conn != null)
					{
						conn.Send(3, objectSpawnMessage);
					}
					else
					{
						SendToReady(uv.gameObject, 3, objectSpawnMessage);
					}
				}
				else
				{
					var objectSpawnSceneMessage = new QObjectSpawnSceneMessage
					{
						NetId = uv.NetId,
						SceneId = uv.SceneId,
						Position = uv.transform.position
					};
					var networkWriter2 = new QNetworkWriter();
					uv.UNetSerializeAllVars(networkWriter2);
					if (networkWriter2.Position > 0)
					{
						objectSpawnSceneMessage.Payload = networkWriter2.ToArray();
					}

					if (conn != null)
					{
						conn.Send(10, objectSpawnSceneMessage);
					}
					else
					{
						SendToReady(uv.gameObject, 3, objectSpawnSceneMessage);
					}
				}
			}
		}

		public static void DestroyPlayersForConnection(QNetworkConnection conn)
		{
			if (conn.PlayerControllers.Count == 0)
			{
				QLog.Warning("Empty player list given to NetworkServer.Destroy(), nothing to do.");
			}
			else
			{
				if (conn.ClientOwnedObjects != null)
				{
					var hashSet = new HashSet<NetworkInstanceId>(conn.ClientOwnedObjects);
					foreach (var gameObject in hashSet.Select(FindLocalObject).Where(gameObject => gameObject != null))
					{
						DestroyObject(gameObject);
					}
				}

				foreach (var playerController in conn.PlayerControllers)
				{
					if (playerController.IsValid)
					{
						if (!(playerController.UnetView == null))
						{
							DestroyObject(playerController.UnetView, true);
						}

						playerController.Gameobject = null;
					}
				}

				conn.PlayerControllers.Clear();
			}
		}

		private static void UnSpawnObject(GameObject obj)
		{
			if (obj == null)
			{
				QLog.Log("NetworkServer UnspawnObject is null");
			}
			else if (GetNetworkIdentity(obj, out var uv))
			{
				UnSpawnObject(uv);
			}
		}

		private static void UnSpawnObject(QNetworkIdentity uv) => DestroyObject(uv, false);

		private static void DestroyObject(GameObject obj)
		{
			if (obj == null)
			{
				QLog.Log("NetworkServer DestroyObject is null");
			}
			else if (GetNetworkIdentity(obj, out var uv))
			{
				DestroyObject(uv, true);
			}
		}

		private static void DestroyObject(QNetworkIdentity uv, bool destroyServerObject)
		{
			QLog.Log($"DestroyObject instance:{uv.NetId}");
			if (objects.ContainsKey(uv.NetId))
			{
				objects.Remove(uv.NetId);
			}

			uv.ClientAuthorityOwner?.RemoveOwnedObject(uv);
			var objectDestroyMessage = new QObjectDestroyMessage
			{
				NetId = uv.NetId
			};
			SendToObservers(uv.gameObject, 1, objectDestroyMessage);
			uv.ClearObservers();
			if (QNetworkClient.active && instance.m_LocalClientActive)
			{
				uv.OnNetworkDestroy();
				QClientScene.SetLocalObject(objectDestroyMessage.NetId, null);
			}

			if (destroyServerObject)
			{
				UnityEngine.Object.Destroy(uv.gameObject);
			}

			uv.MarkForReset();
		}

		public static void ClearLocalObjects() => objects.Clear();

		public static void Spawn(GameObject obj)
		{
			if (VerifyCanSpawn(obj))
			{
				instance.SpawnObject(obj);
			}
		}

		private static bool CheckForPrefab(GameObject obj) => false;

		private static bool VerifyCanSpawn(GameObject obj)
		{
			bool result;
			if (CheckForPrefab(obj))
			{
				QLog.Error($"GameObject {obj.name} is a prefab, it can't be spawned. This will cause errors in builds.");
				result = false;
			}
			else
			{
				result = true;
			}

			return result;
		}

		public static bool SpawnWithClientAuthority(GameObject obj, GameObject player)
		{
			var component = player.GetComponent<QNetworkIdentity>();
			bool result;
			if (component == null)
			{
				QLog.Error("SpawnWithClientAuthority player object has no NetworkIdentity");
				result = false;
			}
			else if (component.ConnectionToClient == null)
			{
				QLog.Error("SpawnWithClientAuthority player object is not a player.");
				result = false;
			}
			else
			{
				result = SpawnWithClientAuthority(obj, component.ConnectionToClient);
			}

			return result;
		}

		public static bool SpawnWithClientAuthority(GameObject obj, QNetworkConnection conn)
		{
			bool result;
			if (!conn.isReady)
			{
				QLog.Error("SpawnWithClientAuthority NetworkConnection is not ready!");
				result = false;
			}
			else
			{
				Spawn(obj);
				var component = obj.GetComponent<QNetworkIdentity>();
				result = !(component == null) && component.IsServer && component.AssignClientAuthority(conn);
			}

			return result;
		}

		public static bool SpawnWithClientAuthority(GameObject obj, NetworkHash128 assetId, QNetworkConnection conn)
		{
			Spawn(obj, assetId);
			var component = obj.GetComponent<QNetworkIdentity>();
			return !(component == null) && component.IsServer && component.AssignClientAuthority(conn);
		}

		public static void Spawn(GameObject obj, NetworkHash128 assetId)
		{
			if (VerifyCanSpawn(obj))
			{
				if (GetNetworkIdentity(obj, out var networkIdentity))
				{
					networkIdentity.SetDynamicAssetId(assetId);
				}

				instance.SpawnObject(obj);
			}
		}

		public static void Destroy(GameObject obj) => DestroyObject(obj);

		public static void UnSpawn(GameObject obj) => UnSpawnObject(obj);

		internal bool InvokeBytes(QULocalConnectionToServer conn, byte[] buffer, int numBytes, int channelId)
		{
			var networkReader = new QNetworkReader(buffer);
			networkReader.ReadInt16();
			var num = networkReader.ReadInt16();
			bool result;
			if (handlers.ContainsKey(num) && m_LocalConnection != null)
			{
				m_LocalConnection.InvokeHandler(num, networkReader, channelId);
				result = true;
			}
			else
			{
				result = false;
			}

			return result;
		}

		internal bool InvokeHandlerOnServer(QULocalConnectionToServer conn, short msgType, QMessageBase msg, int channelId)
		{
			bool result;
			if (handlers.ContainsKey(msgType) && m_LocalConnection != null)
			{
				var writer = new QNetworkWriter();
				msg.Serialize(writer);
				var reader = new QNetworkReader(writer);
				m_LocalConnection.InvokeHandler(msgType, reader, channelId);
				result = true;
			}
			else
			{
				QLog.Error($"Local invoke: Failed to find local connection to invoke handler on [connectionId={conn.connectionId}] for MsgId:{msgType}");
				result = false;
			}

			return result;
		}

		public static GameObject FindLocalObject(NetworkInstanceId netId) => instance.m_NetworkScene.FindLocalObject(netId);

		private static bool ValidateSceneObject(QNetworkIdentity netId) => netId.gameObject.hideFlags != HideFlags.NotEditable && netId.gameObject.hideFlags != HideFlags.HideAndDontSave && !netId.SceneId.IsEmpty();

		public static bool SpawnObjects()
		{
			bool result;
			if (!active)
			{
				result = true;
			}
			else
			{
				var objectsOfTypeAll = Resources.FindObjectsOfTypeAll<QNetworkIdentity>();
				foreach (var networkIdentity in objectsOfTypeAll)
				{
					if (ValidateSceneObject(networkIdentity))
					{
						Debug.Log(
							$"SpawnObjects sceneId:{networkIdentity.SceneId} name:{networkIdentity.gameObject.name}");
						networkIdentity.Reset();
						networkIdentity.gameObject.SetActive(true);
					}
				}

				foreach (var networkIdentity2 in objectsOfTypeAll)
				{
					if (ValidateSceneObject(networkIdentity2))
					{
						Spawn(networkIdentity2.gameObject);
						networkIdentity2.ForceAuthority(true);
					}
				}

				result = true;
			}

			return result;
		}

		private static void SendCrc(QNetworkConnection targetConnection)
		{
			if (QNetworkCRC.singleton != null)
			{
				if (QNetworkCRC.scriptCRCCheck)
				{
					var crcmessage = new QCRCMessage();
					var list = new List<QCRCMessageEntry>();
					foreach (var text in QNetworkCRC.singleton.scripts.Keys)
					{
						list.Add(new QCRCMessageEntry
						{
							name = text,
							channel = (byte)QNetworkCRC.singleton.scripts[text]
						});
					}

					crcmessage.scripts = list.ToArray();
					targetConnection.Send(14, crcmessage);
				}
			}
		}

		private static volatile QNetworkServer s_Instance;

		private static readonly object s_Sync = new UnityEngine.Object();

		private bool m_LocalClientActive;

		private readonly List<QNetworkConnection> m_LocalConnectionsFakeList = new List<QNetworkConnection>();

		private QULocalConnectionToClient m_LocalConnection;

		private readonly QNetworkScene m_NetworkScene;

		private readonly HashSet<int> m_ExternalConnections;

		private readonly ServerSimpleWrapper m_SimpleServerSimple;

		private float m_MaxDelay = 0.1f;

		private readonly HashSet<NetworkInstanceId> m_RemoveList;

		private int m_RemoveListCount;

		private const int k_RemoveListInterval = 100;

		internal static ushort maxPacketSize;

		private static readonly QRemovePlayerMessage s_RemovePlayerMessage = new QRemovePlayerMessage();

		private class ServerSimpleWrapper : QNetworkServerSimple
		{
			public ServerSimpleWrapper(QNetworkServer server) => m_Server = server;

			public override void OnConnectError(int connectionId, byte error) => m_Server.GenerateConnectError(error);

			public override void OnDataError(QNetworkConnection conn, byte error) => m_Server.GenerateDataError(conn, error);

			public override void OnDisconnectError(QNetworkConnection conn, byte error) => m_Server.GenerateDisconnectError(conn, error);

			public override void OnConnected(QNetworkConnection conn) => m_Server.OnConnected(conn);

			public override void OnDisconnected(QNetworkConnection conn) => m_Server.OnDisconnected(conn);

			public override void OnData(QNetworkConnection conn, int receivedSize, int channelId) => m_Server.OnData(conn, receivedSize, channelId);

			private readonly QNetworkServer m_Server;
		}
	}
}