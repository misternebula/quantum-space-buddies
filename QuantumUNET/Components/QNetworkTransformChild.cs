using QuantumUNET.Messages;
using QuantumUNET.Transport;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace QuantumUNET.Components
{
	public class QNetworkTransformChild : QNetworkBehaviour
	{
		public override int GetNetworkChannel() => 1;
		public override float GetNetworkSendInterval() => m_SendInterval;
		public Transform m_Target;
		public uint m_ChildIndex;
		public QNetworkTransform m_Root;
		public float m_SendInterval = 0.1f;
		public float m_MovementThreshold = 0.001f;
		public float LastSyncTime { get; private set; }
		public Vector3 TargetSyncPosition => _targetSyncPosition;
		public Quaternion TargetSyncRotation3D => _targetSyncRotation3D;

		private Vector3 _targetSyncPosition;
		private Quaternion _targetSyncRotation3D;
		private float _lastClientSendTime;
		private Vector3 _prevPosition;
		private Quaternion _prevRotation;
		private const float LocalMovementThreshold = 1E-05f;
		private const float LocalRotationThreshold = 1E-05f;
		private QNetworkWriter _localTransformWriter;

		public Transform Target
		{
			get => m_Target;
			set => m_Target = value;
		}

		public uint ChildIndex => m_ChildIndex;

		public float SendInterval
		{
			get => m_SendInterval;
			set => m_SendInterval = value;
		}

		public float MovementThreshold
		{
			get => m_MovementThreshold;
			set => m_MovementThreshold = value;
		}

		public void Awake()
		{
			_prevPosition = m_Target.localPosition;
			_prevRotation = m_Target.localRotation;
			if (LocalPlayerAuthority)
			{
				_localTransformWriter = new QNetworkWriter();
			}
		}

		public void FixedUpdate()
		{
			if (IsServer)
			{
				FixedUpdateServer();
			}

			if (IsClient)
			{
				FixedUpdateClient();
			}
		}

		private void FixedUpdateServer()
		{
			if (SyncVarDirtyBits != 0U)
			{
				return;
			}

			if (!QNetworkServer.active)
			{
				return;
			}

			if (!IsServer)
			{
				return;
			}

			if (GetNetworkSendInterval() == 0f)
			{
				return;
			}

			var movementMagnitude = (m_Target.localPosition - _prevPosition).sqrMagnitude;
			if (movementMagnitude < MovementThreshold)
			{
				var rotationAngle = Quaternion.Angle(_prevRotation, m_Target.localRotation);
				if (rotationAngle < MovementThreshold)
				{
					return;
				}
			}

			SetDirtyBit(1U);
		}

		private void FixedUpdateClient()
		{
			if (LastSyncTime == 0f)
			{
				return;
			}

			if (!QNetworkServer.active && !QNetworkClient.active)
			{
				return;
			}

			if (!IsServer && !IsClient)
			{
				return;
			}

			if (GetNetworkSendInterval() == 0f)
			{
				return;
			}

			if (HasAuthority)
			{
				return;
			}

			m_Target.localPosition = _targetSyncPosition;
			m_Target.localRotation = _targetSyncRotation3D;
		}

		public void Update()
		{
			if (!HasAuthority)
			{
				return;
			}

			if (!LocalPlayerAuthority)
			{
				return;
			}

			if (QNetworkServer.active)
			{
				return;
			}

			if (Time.time - _lastClientSendTime > GetNetworkSendInterval())
			{
				SendTransform();
				_lastClientSendTime = Time.time;
			}
		}

		public override bool OnSerialize(QNetworkWriter writer, bool initialState)
		{
			if (!initialState)
			{
				if (SyncVarDirtyBits == 0U)
				{
					writer.WritePackedUInt32(0U);
					return false;
				}

				writer.WritePackedUInt32(1U);
			}

			SerializeModeTransform(writer);
			return true;
		}

		private void SerializeModeTransform(QNetworkWriter writer)
		{
			writer.Write(m_Target.localPosition);
			QNetworkTransform.SerializeRotation(writer, m_Target.localRotation);
			_prevPosition = m_Target.localPosition;
			_prevRotation = m_Target.localRotation;
		}

		public override void OnDeserialize(QNetworkReader reader, bool initialState)
		{
			if (!IsServer || !QNetworkServer.localClientActive)
			{
				if (!initialState)
				{
					if (reader.ReadPackedUInt32() == 0U)
					{
						return;
					}
				}

				UnserializeModeTransform(reader, initialState);
				LastSyncTime = Time.time;
			}
		}

		private void UnserializeModeTransform(QNetworkReader reader, bool initialState)
		{
			if (HasAuthority)
			{
				reader.ReadVector3();
				QNetworkTransform.DeserializeRotation(reader);
			}
			else
			{
				_targetSyncPosition = reader.ReadVector3();
				_targetSyncRotation3D = QNetworkTransform.DeserializeRotation(reader);
			}
		}

		private bool HasMoved()
		{
			var num = (m_Target.localPosition - _prevPosition).sqrMagnitude;
			bool result;
			if (num > LocalMovementThreshold)
			{
				result = true;
			}
			else
			{
				num = Quaternion.Angle(m_Target.localRotation, _prevRotation);
				result = num > LocalRotationThreshold;
			}

			return result;
		}

		[Client]
		private void SendTransform()
		{
			if (HasMoved() && QClientScene.readyConnection != null)
			{
				_localTransformWriter.StartMessage(16);
				_localTransformWriter.Write(NetId);
				_localTransformWriter.WritePackedUInt32(m_ChildIndex);
				SerializeModeTransform(_localTransformWriter);
				_prevPosition = m_Target.localPosition;
				_prevRotation = m_Target.localRotation;
				_localTransformWriter.FinishMessage();
				QClientScene.readyConnection.SendWriter(_localTransformWriter, GetNetworkChannel());
			}
		}

		// Called on the server
		internal static void HandleChildTransform(QNetworkMessage netMsg)
		{
			var networkInstanceId = netMsg.Reader.ReadNetworkId();
			var num = netMsg.Reader.ReadPackedUInt32();
			var gameObject = QNetworkServer.FindLocalObject(networkInstanceId);
			if (gameObject == null)
			{
				Debug.LogError("Received NetworkTransformChild data for GameObject that doesn't exist");
			}
			else
			{
				var components = gameObject.GetComponents<QNetworkTransformChild>();
				if (components == null || components.Length == 0)
				{
					Debug.LogError("HandleChildTransform no children");
				}
				else if (num >= (ulong)components.Length)
				{
					Debug.LogError("HandleChildTransform childIndex invalid");
				}
				else
				{
					var networkTransformChild = components[(int)(UIntPtr)num];
					if (networkTransformChild == null)
					{
						Debug.LogError("HandleChildTransform null target");
					}
					else if (!networkTransformChild.LocalPlayerAuthority)
					{
						Debug.LogError("HandleChildTransform no localPlayerAuthority");
					}
					else if (!netMsg.Connection.ClientOwnedObjects.Contains(networkInstanceId))
					{
						Debug.LogWarning("NetworkTransformChild netId:" + networkInstanceId + " is not for a valid player");
					}
					else
					{
						networkTransformChild.UnserializeModeTransform(netMsg.Reader, false);
						networkTransformChild.LastSyncTime = Time.time;
						if (!networkTransformChild.IsClient)
						{
							networkTransformChild.m_Target.localPosition = networkTransformChild._targetSyncPosition;
							networkTransformChild.m_Target.localRotation = networkTransformChild._targetSyncRotation3D;
						}
					}
				}
			}
		}
	}
}
