using OWML.Common;
using QSB.Utility;
using System.Reflection;
using UnityEngine;

namespace QSB.Syncs
{
	public class IntermediaryTransform
	{
		private Transform _attachedTransform;
		private Transform _referenceTransform;

		public IntermediaryTransform(Transform transform)
			=> _attachedTransform = transform;

		/// <summary>
		/// Get the world position of this INVISIBLE transform.
		/// </summary>
		public Vector3 GetPosition()
			=> _attachedTransform.position;

		/// <summary>
		/// Set the world position of this INVISIBLE transform.
		/// </summary>
		public void SetPosition(Vector3 worldPos)
			=> _attachedTransform.position = worldPos;

		/// <summary>
		/// Get the world rotation of this INVISIBLE transform.
		/// </summary>
		public Quaternion GetRotation()
			=> _attachedTransform.rotation;

		/// <summary>
		/// Set the world rotation of this INVISIBLE transform.
		/// </summary>
		public void SetRotation(Quaternion worldRot)
			=> _attachedTransform.rotation = worldRot;

		/// <summary>
		/// Sets the reference transform - what transform this transform is syncing to.
		/// </summary>
		/// <param name="sector">The new reference sector.</param>
		public void SetReferenceTransform(Transform transform)
			=> _referenceTransform = transform;

		/// <summary>
		/// Returns the reference transform - what transform this transform is syncing to.
		/// </summary>
		public Transform GetReferenceTransform()
			=> _referenceTransform;

		/// <summary>
		/// Sets the position of the INVISIBLE transform to be correct, according to the reference sector and the position of the VISIBLE transform.
		/// </summary>
		/// <param name="worldPosition">The world position of the VISIBLE transform.</param>
		public void EncodePosition(Vector3 worldPosition)
		{
			if (_referenceTransform == null)
			{
				DebugLog.ToConsole($"Error - _referenceTransform has not been set for {_attachedTransform.name} ({MethodBase.GetCurrentMethod().Name})", MessageType.Error);
				return;
			}

			SetPosition(_referenceTransform.InverseTransformPoint(worldPosition));
		}

		/// <summary>
		/// Sets the rotation of the INVISIBLE transform to be correct, according to the reference sector and the rotation of the VISIBLE transform.
		/// </summary>
		/// <param name="worldPosition">The world rotation of the VISIBLE transform.</param>
		public void EncodeRotation(Quaternion worldRotation)
		{
			if (_referenceTransform == null)
			{
				DebugLog.ToConsole($"Error - _referenceTransform has not been set for {_attachedTransform.name} ({MethodBase.GetCurrentMethod().Name})", MessageType.Error);
				return;
			}

			SetRotation(_referenceTransform.InverseTransformRotation(worldRotation));
		}

		/// <summary>
		/// Returns the local position the VISIBLE transform should be set to, from the INVISIBLE transform.
		/// </summary>
		public Vector3 GetTargetPosition_ParentedToReference()
			=> GetPosition();

		/// <summary>
		/// Returns the local rotation the VISIBLE transform should be set to, from the INVISIBLE transform.
		/// </summary>
		public Quaternion GetTargetRotation_ParentedToReference()
			=> GetRotation();

		/// <summary>
		/// Returns the world position the VISIBLE transform should be set to, from the INVISIBLE transform.
		/// </summary>
		public Vector3 GetTargetPosition_Unparented()
		{
			if (_referenceTransform == null)
			{
				DebugLog.ToConsole($"Error - _referenceTransform has not been set for {_attachedTransform.name} ({MethodBase.GetCurrentMethod().Name})", MessageType.Error);
				return Vector3.zero;
			}

			return _referenceTransform.TransformPoint(GetPosition());
		}

		/// <summary>
		/// Returns the world rotation the VISIBLE transform should be set to, from the INVISIBLE transform.
		/// </summary>
		public Quaternion GetTargetRotation_Unparented()
		{
			if (_referenceTransform == null)
			{
				DebugLog.ToConsole($"Error - _referenceTransform has not been set for {_attachedTransform.name} ({MethodBase.GetCurrentMethod().Name})", MessageType.Error);
				return Quaternion.identity;
			}

			return _referenceTransform.TransformRotation(GetRotation());
		}
	}
}
