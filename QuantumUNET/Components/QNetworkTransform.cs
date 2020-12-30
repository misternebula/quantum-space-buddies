﻿using QuantumUNET.Logging;
using QuantumUNET.Messages;
using QuantumUNET.Transport;
using UnityEngine;
using UnityEngine.Networking;

namespace QuantumUNET.Components
{
	public class QNetworkTransform : QNetworkBehaviour
	{
		public float SendInterval { get; set; } = 0.1f;
		public AxisSyncMode SyncRotationAxis { get; set; } = AxisSyncMode.AxisXYZ;
		public CompressionSyncMode RotationSyncCompression { get; set; } = CompressionSyncMode.None;
		public ClientMoveCallback3D clientMoveCallback3D { get; set; }
		public float LastSyncTime { get; private set; }

		public void Awake()
		{
			m_PrevPosition = transform.position;
			m_PrevRotation = transform.rotation;
			if (LocalPlayerAuthority)
			{
				m_LocalTransformWriter = new QNetworkWriter();
			}
		}

		public override void OnStartServer() => LastSyncTime = 0f;

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
			writer.Write(transform.position);
			if (SyncRotationAxis != AxisSyncMode.None)
			{
				SerializeRotation3D(writer, transform.rotation, SyncRotationAxis, RotationSyncCompression);
			}
			m_PrevPosition = transform.position;
			m_PrevRotation = transform.rotation;
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
				if (SyncRotationAxis != AxisSyncMode.None)
				{
					UnserializeRotation3D(reader, SyncRotationAxis, RotationSyncCompression);
				}
			}
			else if (IsServer && clientMoveCallback3D != null)
			{
				var position = reader.ReadVector3();
				var zero = Vector3.zero;
				var rotation = Quaternion.identity;
				if (SyncRotationAxis != AxisSyncMode.None)
				{
					rotation = UnserializeRotation3D(reader, SyncRotationAxis, RotationSyncCompression);
				}
				if (clientMoveCallback3D(ref position, ref zero, ref rotation))
				{
					transform.position = position;
					if (SyncRotationAxis != AxisSyncMode.None)
					{
						transform.rotation = rotation;
					}
				}
			}
			else
			{
				transform.position = reader.ReadVector3();
				if (SyncRotationAxis != AxisSyncMode.None)
				{
					transform.rotation = UnserializeRotation3D(reader, SyncRotationAxis, RotationSyncCompression);
				}
			}
		}

		private void FixedUpdate()
		{
			if (IsServer)
			{
				FixedUpdateServer();
			}
		}

		private void FixedUpdateServer()
		{
			if (SyncVarDirtyBits == 0U)
			{
				if (QNetworkServer.active)
				{
					if (IsServer)
					{
						if (GetNetworkSendInterval() != 0f)
						{
							if (HasMoved())
							{
								SetDirtyBit(1U);
							}
						}
					}
				}
			}
		}

		private void Update()
		{
			if (HasAuthority)
			{
				if (LocalPlayerAuthority)
				{
					if (!QNetworkServer.active)
					{
						if (Time.time - m_LastClientSendTime > GetNetworkSendInterval())
						{
							SendTransform();
							m_LastClientSendTime = Time.time;
						}
					}
				}
			}
		}

		private bool HasMoved()
		{
			var num = (transform.position - m_PrevPosition).magnitude;
			bool result;
			if (num > 1E-05f)
			{
				result = true;
			}
			else
			{
				num = Quaternion.Angle(transform.rotation, m_PrevRotation);
				if (num > 1E-05f)
				{
					result = true;
				}
				else
				{
					result = num > 1E-05f;
				}
			}
			return result;
		}

		[Client]
		private void SendTransform()
		{
			if (HasMoved() && QClientScene.readyConnection != null)
			{
				m_LocalTransformWriter.StartMessage(6);
				m_LocalTransformWriter.Write(NetId);
				SerializeModeTransform(m_LocalTransformWriter);
				m_PrevPosition = transform.position;
				m_PrevRotation = transform.rotation;
				m_LocalTransformWriter.FinishMessage();
				QClientScene.readyConnection.SendWriter(m_LocalTransformWriter, GetNetworkChannel());
			}
		}

		public static void HandleTransform(QNetworkMessage netMsg)
		{
			var networkInstanceId = netMsg.Reader.ReadNetworkId();
			var gameObject = QNetworkServer.FindLocalObject(networkInstanceId);
			if (gameObject == null)
			{
				QLog.Warning("Received NetworkTransform data for GameObject that doesn't exist");
			}
			else
			{
				var component = gameObject.GetComponent<QNetworkTransform>();
				if (component == null)
				{
					QLog.Warning("HandleTransform null target");
				}
				else if (!component.LocalPlayerAuthority)
				{
					QLog.Warning("HandleTransform no localPlayerAuthority");
				}
				else if (netMsg.Connection.ClientOwnedObjects == null)
				{
					QLog.Warning("HandleTransform object not owned by connection");
				}
				else if (netMsg.Connection.ClientOwnedObjects.Contains(networkInstanceId))
				{
					component.UnserializeModeTransform(netMsg.Reader, false);
					component.LastSyncTime = Time.time;
				}
				else
				{
					QLog.Warning(
						$"HandleTransform netId:{networkInstanceId} is not for a valid player");
				}
			}
		}

		private static void WriteAngle(QNetworkWriter writer, float angle, CompressionSyncMode compression)
		{
			if (compression != CompressionSyncMode.None)
			{
				if (compression != CompressionSyncMode.Low)
				{
					if (compression == CompressionSyncMode.High)
					{
						writer.Write((short)angle);
					}
				}
				else
				{
					writer.Write((short)angle);
				}
			}
			else
			{
				writer.Write(angle);
			}
		}

		private static float ReadAngle(QNetworkReader reader, CompressionSyncMode compression)
		{
			float result;
			if (compression != CompressionSyncMode.None)
			{
				if (compression != CompressionSyncMode.Low)
				{
					if (compression != CompressionSyncMode.High)
					{
						result = 0f;
					}
					else
					{
						result = reader.ReadInt16();
					}
				}
				else
				{
					result = reader.ReadInt16();
				}
			}
			else
			{
				result = reader.ReadSingle();
			}
			return result;
		}

		public static void SerializeVelocity3D(QNetworkWriter writer, Vector3 velocity, CompressionSyncMode compression) =>
			writer.Write(velocity);

		public static void SerializeRotation3D(QNetworkWriter writer, Quaternion rot, AxisSyncMode mode, CompressionSyncMode compression)
		{
			switch (mode)
			{
				case AxisSyncMode.AxisX:
					WriteAngle(writer, rot.eulerAngles.x, compression);
					break;

				case AxisSyncMode.AxisY:
					WriteAngle(writer, rot.eulerAngles.y, compression);
					break;

				case AxisSyncMode.AxisZ:
					WriteAngle(writer, rot.eulerAngles.z, compression);
					break;

				case AxisSyncMode.AxisXY:
					WriteAngle(writer, rot.eulerAngles.x, compression);
					WriteAngle(writer, rot.eulerAngles.y, compression);
					break;

				case AxisSyncMode.AxisXZ:
					WriteAngle(writer, rot.eulerAngles.x, compression);
					WriteAngle(writer, rot.eulerAngles.z, compression);
					break;

				case AxisSyncMode.AxisYZ:
					WriteAngle(writer, rot.eulerAngles.y, compression);
					WriteAngle(writer, rot.eulerAngles.z, compression);
					break;

				case AxisSyncMode.AxisXYZ:
					WriteAngle(writer, rot.eulerAngles.x, compression);
					WriteAngle(writer, rot.eulerAngles.y, compression);
					WriteAngle(writer, rot.eulerAngles.z, compression);
					break;
			}
		}

		public static void SerializeSpin3D(QNetworkWriter writer, Vector3 angularVelocity, AxisSyncMode mode, CompressionSyncMode compression)
		{
			switch (mode)
			{
				case AxisSyncMode.AxisX:
					WriteAngle(writer, angularVelocity.x, compression);
					break;

				case AxisSyncMode.AxisY:
					WriteAngle(writer, angularVelocity.y, compression);
					break;

				case AxisSyncMode.AxisZ:
					WriteAngle(writer, angularVelocity.z, compression);
					break;

				case AxisSyncMode.AxisXY:
					WriteAngle(writer, angularVelocity.x, compression);
					WriteAngle(writer, angularVelocity.y, compression);
					break;

				case AxisSyncMode.AxisXZ:
					WriteAngle(writer, angularVelocity.x, compression);
					WriteAngle(writer, angularVelocity.z, compression);
					break;

				case AxisSyncMode.AxisYZ:
					WriteAngle(writer, angularVelocity.y, compression);
					WriteAngle(writer, angularVelocity.z, compression);
					break;

				case AxisSyncMode.AxisXYZ:
					WriteAngle(writer, angularVelocity.x, compression);
					WriteAngle(writer, angularVelocity.y, compression);
					WriteAngle(writer, angularVelocity.z, compression);
					break;
			}
		}

		public static Vector3 UnserializeVelocity3D(QNetworkReader reader, CompressionSyncMode compression) => reader.ReadVector3();

		public static Quaternion UnserializeRotation3D(QNetworkReader reader, AxisSyncMode mode, CompressionSyncMode compression)
		{
			var identity = Quaternion.identity;
			var zero = Vector3.zero;
			switch (mode)
			{
				case AxisSyncMode.AxisX:
					zero.Set(ReadAngle(reader, compression), 0f, 0f);
					identity.eulerAngles = zero;
					break;

				case AxisSyncMode.AxisY:
					zero.Set(0f, ReadAngle(reader, compression), 0f);
					identity.eulerAngles = zero;
					break;

				case AxisSyncMode.AxisZ:
					zero.Set(0f, 0f, ReadAngle(reader, compression));
					identity.eulerAngles = zero;
					break;

				case AxisSyncMode.AxisXY:
					zero.Set(ReadAngle(reader, compression), ReadAngle(reader, compression), 0f);
					identity.eulerAngles = zero;
					break;

				case AxisSyncMode.AxisXZ:
					zero.Set(ReadAngle(reader, compression), 0f, ReadAngle(reader, compression));
					identity.eulerAngles = zero;
					break;

				case AxisSyncMode.AxisYZ:
					zero.Set(0f, ReadAngle(reader, compression), ReadAngle(reader, compression));
					identity.eulerAngles = zero;
					break;

				case AxisSyncMode.AxisXYZ:
					zero.Set(ReadAngle(reader, compression), ReadAngle(reader, compression), ReadAngle(reader, compression));
					identity.eulerAngles = zero;
					break;
			}
			return identity;
		}

		public static Vector3 UnserializeSpin3D(QNetworkReader reader, AxisSyncMode mode, CompressionSyncMode compression)
		{
			var zero = Vector3.zero;
			switch (mode)
			{
				case AxisSyncMode.AxisX:
					zero.Set(ReadAngle(reader, compression), 0f, 0f);
					break;

				case AxisSyncMode.AxisY:
					zero.Set(0f, ReadAngle(reader, compression), 0f);
					break;

				case AxisSyncMode.AxisZ:
					zero.Set(0f, 0f, ReadAngle(reader, compression));
					break;

				case AxisSyncMode.AxisXY:
					zero.Set(ReadAngle(reader, compression), ReadAngle(reader, compression), 0f);
					break;

				case AxisSyncMode.AxisXZ:
					zero.Set(ReadAngle(reader, compression), 0f, ReadAngle(reader, compression));
					break;

				case AxisSyncMode.AxisYZ:
					zero.Set(0f, ReadAngle(reader, compression), ReadAngle(reader, compression));
					break;

				case AxisSyncMode.AxisXYZ:
					zero.Set(ReadAngle(reader, compression), ReadAngle(reader, compression), ReadAngle(reader, compression));
					break;
			}
			return zero;
		}

		public override int GetNetworkChannel() => 1;

		public override float GetNetworkSendInterval() => SendInterval;

		public override void OnStartAuthority() => LastSyncTime = 0f;

		private float m_LastClientSendTime;

		private Vector3 m_PrevPosition;

		private Quaternion m_PrevRotation;

		private QNetworkWriter m_LocalTransformWriter;

		public enum AxisSyncMode
		{
			None,
			AxisX,
			AxisY,
			AxisZ,
			AxisXY,
			AxisXZ,
			AxisYZ,
			AxisXYZ
		}

		public enum CompressionSyncMode
		{
			None,
			Low,
			High
		}

		public delegate bool ClientMoveCallback3D(ref Vector3 position, ref Vector3 velocity, ref Quaternion rotation);
	}
}