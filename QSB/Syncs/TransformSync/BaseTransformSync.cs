using OWML.Common;
using QSB.Player;
using QSB.Player.TransformSync;
using QSB.Utility;
using QuantumUNET.Transport;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QSB.Syncs.TransformSync
{
	/*
	 * Rewrite number : 7
	 * God has cursed me for my hubris, and my work is never finished.
	 */

	public abstract class BaseTransformSync : SyncBase
	{
		private static readonly Dictionary<PlayerInfo, Dictionary<Type, BaseTransformSync>> _storedTransformSyncs = new Dictionary<PlayerInfo, Dictionary<Type, BaseTransformSync>>();

		public static T GetPlayers<T>(PlayerInfo player)
			where T : BaseTransformSync
		{
			var dictOfOwnedSyncs = _storedTransformSyncs[player];
			var wantedSync = dictOfOwnedSyncs[typeof(T)];
			if (wantedSync == default)
			{
				DebugLog.ToConsole($"Error -  _storedTransformSyncs does not contain type:{typeof(T)} under player {player.PlayerId}. Attempting to find manually...", MessageType.Error);
				var allSyncs = Resources.FindObjectsOfTypeAll<T>();
				wantedSync = allSyncs.First(x => x.Player == player);
				if (wantedSync == default)
				{
					DebugLog.ToConsole($"Error -  Could not find type:{typeof(T)} for player {player.PlayerId} manually!", MessageType.Error);
					return default;
				}
			}

			return (T)wantedSync;
		}

		public virtual void Start()
		{
			var lowestBound = Resources.FindObjectsOfTypeAll<PlayerTransformSync>()
				.Where(x => x.NetId.Value <= NetId.Value).OrderBy(x => x.NetId.Value).Last();
			NetIdentity.SetRootIdentity(lowestBound.NetIdentity);

			DontDestroyOnLoad(gameObject);
			_intermediaryTransform = new IntermediaryTransform(transform);
			QSBSceneManager.OnSceneLoaded += OnSceneLoaded;

			if (!_storedTransformSyncs.ContainsKey(Player))
			{
				_storedTransformSyncs.Add(Player, new Dictionary<Type, BaseTransformSync>());
			}

			var playerDict = _storedTransformSyncs[Player];
			playerDict[GetType()] = this;
			DebugLog.DebugWrite($"Added T:{GetType().Name} to dict of player {Player.PlayerId}", MessageType.Info);
		}

		protected virtual void OnDestroy()
		{
			DebugLog.DebugWrite($"OnDestroy {_logName}");
			if (!HasAuthority && AttachedObject != null)
			{
				Destroy(AttachedObject.gameObject);
			}

			QSBSceneManager.OnSceneLoaded -= OnSceneLoaded;

			if (!QSBPlayerManager.PlayerExists(PlayerId))
			{
				return;
			}
			DebugLog.DebugWrite($"{_logName} remove from dict");


			var playerDict = _storedTransformSyncs[Player];
			playerDict.Remove(GetType());
			DebugLog.DebugWrite($"Removed T:{GetType().Name} from dict of player {Player.PlayerId}", MessageType.Info);
		}

		protected virtual void OnSceneLoaded(OWScene scene, bool isInUniverse)
			=> _isInitialized = false;

		protected override void Init()
		{
			if (!QSBSceneManager.IsInUniverse)
			{
				DebugLog.ToConsole($"Error - {_logName} is being init-ed when not in the universe!", MessageType.Error);
			}

			if (!HasAuthority && AttachedObject != null)
			{
				Destroy(AttachedObject.gameObject);
			}

			AttachedObject = HasAuthority ? InitLocalTransform() : InitRemoteTransform();
			_isInitialized = true;

			if (QSBCore.DebugMode)
			{
				DebugBoxManager.CreateBox(AttachedObject.transform, 0, _logName);
			}
		}

		public override void SerializeTransform(QNetworkWriter writer)
		{
			if (_intermediaryTransform == null)
			{
				_intermediaryTransform = new IntermediaryTransform(transform);
			}

			var worldPos = _intermediaryTransform.GetPosition();
			var worldRot = _intermediaryTransform.GetRotation();
			writer.Write(worldPos);
			SerializeRotation(writer, worldRot);
			_prevPosition = worldPos;
			_prevRotation = worldRot;
		}

		public override void DeserializeTransform(QNetworkReader reader)
		{
			if (!QSBCore.WorldObjectsReady)
			{
				reader.ReadVector3();
				DeserializeRotation(reader);
				return;
			}

			var pos = reader.ReadVector3();
			var rot = DeserializeRotation(reader);

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
				return true;
			}

			var targetPos = _intermediaryTransform.GetTargetPosition_ParentedToReference();
			var targetRot = _intermediaryTransform.GetTargetRotation_ParentedToReference();
			if (targetPos != Vector3.zero && _intermediaryTransform.GetTargetPosition_Unparented() != Vector3.zero)
			{
				if (UseInterpolation)
				{
					AttachedObject.transform.localPosition = SmartSmoothDamp(AttachedObject.transform.localPosition, targetPos);
					AttachedObject.transform.localRotation = QuaternionHelper.SmoothDamp(AttachedObject.transform.localRotation, targetRot, ref _rotationSmoothVelocity, SmoothTime);
				}
				else
				{
					AttachedObject.transform.localPosition = targetPos;
					AttachedObject.transform.localRotation = targetRot;
				}
			}
			return true;
		}

		public override bool HasMoved()
		{
			var displacementMagnitude = (_intermediaryTransform.GetPosition() - _prevPosition).magnitude;
			return displacementMagnitude > 1E-03f
				|| Quaternion.Angle(_intermediaryTransform.GetRotation(), _prevRotation) > 1E-03f;
		}

		public void SetReferenceTransform(Transform transform)
		{
			DebugLog.DebugWrite($"{_logName} set reference transform {transform.name}", MessageType.Info);
			if (ReferenceTransform == transform)
			{
				return;
			}

			ReferenceTransform = transform;
			_intermediaryTransform.SetReferenceTransform(transform);
			if (AttachedObject == null)
			{
				DebugLog.ToConsole($"Warning - AttachedObject was null for {_logName} when trying to set reference transform to {transform?.name}. Waiting until not null...", MessageType.Warning);
				QSBCore.UnityEvents.RunWhen(
					() => AttachedObject != null,
					() => ReparentAttachedObject(transform));
				return;
			}

			if (!HasAuthority)
			{
				ReparentAttachedObject(transform);
			}

			if (HasAuthority)
			{
				_intermediaryTransform.EncodePosition(AttachedObject.transform.position);
				_intermediaryTransform.EncodeRotation(AttachedObject.transform.rotation);
			}
		}

		private void ReparentAttachedObject(Transform newParent)
		{
			if (AttachedObject.transform.parent != null && AttachedObject.transform.parent.GetComponent<Sector>() == null)
			{
				DebugLog.ToConsole($"Warning - Trying to reparent AttachedObject {AttachedObject.name} which wasnt attached to sector!", MessageType.Warning);
			}

			AttachedObject.transform.SetParent(newParent, true);
			AttachedObject.transform.localScale = Vector3.one;
		}
	}
}
