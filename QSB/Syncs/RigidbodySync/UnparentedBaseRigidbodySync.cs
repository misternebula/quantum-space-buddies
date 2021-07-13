using OWML.Common;
using OWML.Utils;
using QSB.Utility;
using QuantumUNET.Transport;
using UnityEngine;

namespace QSB.Syncs.RigidbodySync
{
	public abstract class UnparentedBaseRigidbodySync : SyncBase
	{
		protected Vector3 _relativeVelocity;
		protected Vector3 _relativeAngularVelocity;
		protected Vector3 _prevVelocity;
		protected Vector3 _prevAngularVelocity;

		protected abstract OWRigidbody GetRigidbody();

		public virtual void Start()
		{
			DontDestroyOnLoad(gameObject);
			_intermediaryTransform = new IntermediaryTransform(transform);
			QSBSceneManager.OnSceneLoaded += OnSceneLoaded;
		}

		protected virtual void OnDestroy()
		{
			DebugLog.DebugWrite($"OnDestroy {_logName}");
			QSBSceneManager.OnSceneLoaded -= OnSceneLoaded;
		}

		private void OnSceneLoaded(OWScene scene, bool isInUniverse)
			=> _isInitialized = false;

		protected override void Init()
		{
			if (!QSBSceneManager.IsInUniverse)
			{
				DebugLog.ToConsole($"Error - {_logName} is being init-ed when not in the universe!", MessageType.Error);
			}

			AttachedObject = GetRigidbody();
			_isInitialized = true;
		}

		public override void SerializeTransform(QNetworkWriter writer)
		{
			if (_intermediaryTransform == null)
			{
				_intermediaryTransform = new IntermediaryTransform(transform);
			}

			/* We need to send :
			 * - Position
			 * - Rotation
			 * - Velocity
			 * - Angular velocity
			 * We can't store the last two on the IntermediaryTransform, so they come from fields.
			 */

			var worldPos = _intermediaryTransform.GetPosition();
			var worldRot = _intermediaryTransform.GetRotation();
			var relativeVelocity = _relativeVelocity;
			var relativeAngularVelocity = _relativeAngularVelocity;

			writer.Write(worldPos);
			SerializeRotation(writer, worldRot);
			writer.Write(relativeVelocity);
			writer.Write(relativeAngularVelocity);

			_prevPosition = worldPos;
			_prevRotation = worldRot;
			_prevVelocity = relativeVelocity;
			_prevAngularVelocity = relativeAngularVelocity;
		}

		public override void DeserializeTransform(QNetworkReader reader)
		{
			if (!QSBCore.WorldObjectsReady)
			{
				reader.ReadVector3();
				DeserializeRotation(reader);
				reader.ReadVector3();
				reader.ReadVector3();
				return;
			}

			var pos = reader.ReadVector3();
			var rot = DeserializeRotation(reader);
			var relativeVelocity = reader.ReadVector3();
			var relativeAngularVelocity = reader.ReadVector3();

			if (HasAuthority)
			{
				return;
			}

			if (_intermediaryTransform == null)
			{
				_intermediaryTransform = new IntermediaryTransform(transform);
			}

			_intermediaryTransform.SetPosition(pos);
			_intermediaryTransform.SetRotation(rot);
			_relativeVelocity = relativeVelocity;
			_relativeAngularVelocity = relativeAngularVelocity;

			if (_intermediaryTransform.GetPosition() == Vector3.zero)
			{
				DebugLog.ToConsole($"Warning - {_logName} at (0,0,0)! - Given position was {pos}", MessageType.Warning);
			}
		}

		protected override bool UpdateTransform()
		{
			if (HasAuthority)
			{
				_intermediaryTransform.EncodePosition(AttachedObject.transform.position);
				_intermediaryTransform.EncodeRotation(AttachedObject.transform.rotation);
				_relativeVelocity = GetRelativeVelocity();
				_relativeAngularVelocity = (AttachedObject as OWRigidbody).GetRelativeAngularVelocity(ReferenceTransform.GetAttachedOWRigidbody());
				return true;
			}

			var targetPos = _intermediaryTransform.GetTargetPosition_Unparented();
			var targetRot = _intermediaryTransform.GetTargetRotation_Unparented();

			if (targetPos == Vector3.zero || _intermediaryTransform.GetTargetPosition_ParentedToReference() == Vector3.zero)
			{
				return false;
			}

			if (UseInterpolation)
			{
				(AttachedObject as OWRigidbody).SetPosition(SmartSmoothDamp(AttachedObject.transform.position, targetPos));
				(AttachedObject as OWRigidbody).SetRotation(QuaternionHelper.SmoothDamp(AttachedObject.transform.rotation, targetRot, ref _rotationSmoothVelocity, SmoothTime));
			}
			else
			{
				(AttachedObject as OWRigidbody).SetPosition(targetPos);
				(AttachedObject as OWRigidbody).SetRotation(targetRot);
			}

			var currentVelocity = GetRelativeVelocity();
			var targetVelocity = ReferenceTransform.GetAttachedOWRigidbody().GetPointVelocity(targetPos) + _relativeVelocity;
			var adjustedTarget = targetVelocity + Locator.GetCenterOfTheUniverse().GetStaticFrameWorldVelocity();

			SetVelocity((AttachedObject as OWRigidbody), targetVelocity);
			(AttachedObject as OWRigidbody).SetAngularVelocity(ReferenceTransform.GetAttachedOWRigidbody().GetAngularVelocity() + _relativeAngularVelocity);

			return true;
		}

		private void SetVelocity(OWRigidbody rigidbody, Vector3 relativeVelocity)
		{
			var isRunningKinematic = rigidbody.RunningKinematicSimulation();
			var currentVelocity = rigidbody.GetValue<Vector3>("_currentVelocity");

			if (isRunningKinematic)
			{
				var kinematicRigidbody = rigidbody.GetValue<KinematicRigidbody>("_kinematicRigidbody");
				kinematicRigidbody.velocity = relativeVelocity + Locator.GetCenterOfTheUniverse().GetStaticFrameWorldVelocity();
			}
			else
			{
				var normalRigidbody = rigidbody.GetValue<Rigidbody>("_rigidbody");
				normalRigidbody.velocity = relativeVelocity + Locator.GetCenterOfTheUniverse().GetStaticFrameWorldVelocity();
			}

			rigidbody.SetValue("_lastVelocity", currentVelocity);
			rigidbody.SetValue("_currentVelocity", relativeVelocity);
		}

		public void SetReferenceTransform(Transform transform)
		{
			if (ReferenceTransform == transform)
			{
				return;
			}

			ReferenceTransform = transform;
			_intermediaryTransform.SetReferenceTransform(transform);

			if (HasAuthority)
			{
				_intermediaryTransform.EncodePosition(AttachedObject.transform.position);
				_intermediaryTransform.EncodeRotation(AttachedObject.transform.rotation);
			}
		}

		// TODO : optimize by using sqrMagnitude
		public override bool HasMoved()
		{
			var displacementMagnitude = (_intermediaryTransform.GetPosition() - _prevPosition).magnitude;

			if (displacementMagnitude > 1E-03f)
			{
				return true;
			}

			if (Quaternion.Angle(_intermediaryTransform.GetRotation(), _prevRotation) > 1E-03f)
			{
				return true;
			}

			var velocityChangeMagnitude = (_relativeVelocity - _prevVelocity).magnitude;
			var angularVelocityChangeMagnitude = (_relativeAngularVelocity - _prevAngularVelocity).magnitude;
			if (velocityChangeMagnitude > 1E-03f)
			{
				return true;
			}

			if (angularVelocityChangeMagnitude > 1E-03f)
			{
				return true;
			}

			return false;
		}

		public float GetVelocityChangeMagnitude()
			=> (_relativeVelocity - _prevVelocity).magnitude;

		public Vector3 GetRelativeVelocity()
		{
			if (AttachedObject == null)
			{
				DebugLog.ToConsole($"Error - Trying to get relative velocity when AttachedObject is null.", MessageType.Error);
				return Vector3.zero;
			}

			if (ReferenceTransform == null)
			{
				DebugLog.ToConsole($"Error - Trying to get relative velocity when ReferenceTransform is null. ({AttachedObject.name})", MessageType.Error);
				return Vector3.zero;
			}

			var attachedRigid = ReferenceTransform.GetAttachedOWRigidbody();
			if (attachedRigid == null)
			{
				DebugLog.ToConsole($"Error - ReferenceTransform ({ReferenceTransform.name}) on {AttachedObject.name} has no attached OWRigidBody.", MessageType.Error);
				return Vector3.zero;
			}

			var pointVelocity = attachedRigid.GetPointVelocity(AttachedObject.transform.position);
			return (AttachedObject as OWRigidbody).GetVelocity() - pointVelocity;
		}
	}
}