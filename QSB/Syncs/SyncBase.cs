using OWML.Common;
using QSB.Player;
using QSB.Utility;
using QuantumUNET.Components;
using UnityEngine;

namespace QSB.Syncs
{
	public abstract class SyncBase : QNetworkTransform
	{
		public uint AttachedNetId
		{
			get
			{
				if (NetIdentity == null)
				{
					DebugLog.ToConsole($"Error - Trying to get AttachedNetId with null NetIdentity! Type:{GetType().Name} GrandType:{GetType().GetType().Name}", MessageType.Error);
					return uint.MaxValue;
				}

				return NetIdentity.NetId.Value;
			}
		}

		public uint PlayerId
		{
			get
			{
				if (NetIdentity == null)
				{
					DebugLog.ToConsole($"Error - Trying to get PlayerId with null NetIdentity! Type:{GetType().Name} GrandType:{GetType().GetType().Name}", MessageType.Error);
					return uint.MaxValue;
				}

				return NetIdentity.RootIdentity != null
					? NetIdentity.RootIdentity.NetId.Value
					: AttachedNetId;
			}
		}

		public PlayerInfo Player => QSBPlayerManager.GetPlayer(PlayerId);

		public abstract bool IsReady { get; }
		public abstract bool UseInterpolation { get; }
		public abstract bool IgnoreDisabledAttachedObject { get; }
		public abstract bool IgnoreNullReferenceTransform { get; }

		public Component AttachedObject { get; set; }
		public Transform ReferenceTransform { get; set; }

		protected string _logName => $"{PlayerId}.{GetType().Name}";
		protected virtual float DistanceLeeway { get; } = 5f;
		private float _previousDistance;
		protected const float SmoothTime = 0.1f;
		protected Vector3 _positionSmoothVelocity;
		protected Quaternion _rotationSmoothVelocity;
		protected IntermediaryTransform _intermediaryTransform;
		protected bool _isInitialized;

		protected abstract Component InitLocalTransform();
		protected abstract Component InitRemoteTransform();
		protected abstract bool UpdateTransform();
		protected abstract void Init();

		protected Vector3 SmartSmoothDamp(Vector3 currentPosition, Vector3 targetPosition)
		{
			var distance = Vector3.Distance(currentPosition, targetPosition);
			if (distance > _previousDistance + DistanceLeeway)
			{
				_previousDistance = distance;
				return targetPosition;
			}

			_previousDistance = distance;
			return Vector3.SmoothDamp(currentPosition, targetPosition, ref _positionSmoothVelocity, SmoothTime);
		}

		public override void Update()
		{
			if (!_isInitialized && IsReady)
			{
				Init();
			}
			else if (_isInitialized && !IsReady)
			{
				_isInitialized = false;
				base.Update();
				return;
			}

			if (!_isInitialized)
			{
				base.Update();
				return;
			}

			if (AttachedObject == null)
			{
				DebugLog.ToConsole($"Warning - AttachedObject {_logName} is null.", MessageType.Warning);
				_isInitialized = false;
				base.Update();
				return;
			}

			if (ReferenceTransform != null && ReferenceTransform.position == Vector3.zero)
			{
				DebugLog.ToConsole($"Warning - {_logName}'s ReferenceTransform is at (0,0,0). ReferenceTransform:{ReferenceTransform.name}, AttachedObject:{AttachedObject.name}", MessageType.Warning);
			}

			if (!AttachedObject.gameObject.activeInHierarchy && !IgnoreDisabledAttachedObject)
			{
				base.Update();
				return;
			}

			if (ReferenceTransform == null && !IgnoreNullReferenceTransform)
			{
				DebugLog.ToConsole($"Warning - {_logName}'s ReferenceTransform is null. AttachedObject:{AttachedObject.name}", MessageType.Warning);
				base.Update();
				return;
			}

			if (ReferenceTransform != _intermediaryTransform.GetReferenceTransform())
			{
				DebugLog.ToConsole($"Warning - {_logName}'s ReferenceTransform does not match the reference transform set for the intermediary. ReferenceTransform null : {ReferenceTransform == null}, Intermediary reference null : {_intermediaryTransform.GetReferenceTransform() == null}");
				base.Update();
				return;
			}

			var state = UpdateTransform();
			if (!state)
			{
				DebugLog.ToConsole($"{_logName} UpdateTransform() fail.", MessageType.Error);
				base.Update();
				return;
			}

			/*
			var expectedPosition = _intermediaryTransform.GetTargetPosition_Unparented();
			var actualPosition = AttachedObject.transform.position;
			var distance = Vector3.Distance(expectedPosition, actualPosition);
			if (distance > 20)
			{
				var intermediaryReference = _intermediaryTransform.GetReferenceTransform();
				DebugLog.ToConsole($"Warning - {_logName}'s AttachedObject ({AttachedObject?.name}) is far away from it's expected position! Info:" +
					$"\r\n AttachedObject's parent : {AttachedObject?.transform.parent?.name}" +
					$"\r\n Distance : {distance}" +
					$"\r\n ReferenceTransform : {(ReferenceTransform == null ? "NULL" : ReferenceTransform.name)}" +
					$"\r\n Intermediary's ReferenceTransform : {(intermediaryReference == null ? "NULL" : intermediaryReference.name)}", MessageType.Warning);
			}
			*/

			base.Update();
		}

		protected virtual void OnRenderObject()
		{
			if (!QSBCore.WorldObjectsReady
				|| !QSBCore.DebugMode
				|| !QSBCore.ShowLinesInDebug
				|| !IsReady
				|| ReferenceTransform == null
				|| _intermediaryTransform.GetReferenceTransform() == null)
			{
				return;
			}

			/* Red Cube = Where visible object should be
			 * Green/Yellow Cube = Where visible object is
			 * Magenta cube = Reference transform
			 * Red Line = Connection between Red Cube and Green/Yellow Cube
			 * Cyan Line = Connection between Green/Yellow cube and reference transform
			 */

			Popcron.Gizmos.Cube(_intermediaryTransform.GetTargetPosition_Unparented(), _intermediaryTransform.GetTargetRotation_Unparented(), Vector3.one / 2, Color.red);
			Popcron.Gizmos.Line(_intermediaryTransform.GetTargetPosition_Unparented(), AttachedObject.transform.position, Color.red);
			var color = HasMoved() ? Color.green : Color.yellow;
			Popcron.Gizmos.Cube(AttachedObject.transform.position, AttachedObject.transform.rotation, Vector3.one / 2, color);
			Popcron.Gizmos.Cube(ReferenceTransform.position, ReferenceTransform.rotation, Vector3.one / 2, Color.magenta);
			Popcron.Gizmos.Line(AttachedObject.transform.position, ReferenceTransform.position, Color.cyan);
		}
	}
}
