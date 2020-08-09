﻿using OWML.Common;
using QSB.Events;
using QSB.Utility;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace QSB.TransformSync
{
    public abstract class TransformSync : NetworkBehaviour
    {
        public PlayerInfo Player => PlayerRegistry.GetPlayer(PlayerId);

        private const float SmoothTime = 0.1f;
        private bool _isInitialized;

        public Transform SyncedTransform { get; private set; }
        public Transform ReferenceTransform { get; set; }

        private Vector3 _positionSmoothVelocity;
        private Quaternion _rotationSmoothVelocity;

        protected virtual void Awake()
        {
            PlayerRegistry.TransformSyncs.Add(this);
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _isInitialized = false;
        }

        protected abstract Transform InitLocalTransform();
        protected abstract Transform InitRemoteTransform();
        protected abstract bool IsReady { get; }
        protected abstract uint PlayerId { get; }

        protected void Init()
        {
            ReferenceTransform = Locator.GetAstroObject(AstroObject.Name.TimberHearth).transform;
            SyncedTransform = hasAuthority ? InitLocalTransform() : InitRemoteTransform();
            if (!hasAuthority)
            {
                SyncedTransform.position = Locator.GetAstroObject(AstroObject.Name.Sun).transform.position;
            }
            _isInitialized = true;
        }

        private void Update()
        {
            if (!_isInitialized && IsReady)
            {
                QSB.Helper.Events.Unity.FireOnNextUpdate(Init);
            }
            else if (_isInitialized && !IsReady)
            {
                _isInitialized = false;
            }

            if (SyncedTransform == null || !_isInitialized)
            {
                return;
            }

            // Get which sector should be used as a reference point

            if (ReferenceTransform == null)
            {
                DebugLog.ToConsole($"Error - TransformSync with id {netId.Value} doesn't have a reference sector", MessageType.Error);
            }

            UpdateTransform();
        }

        protected virtual void UpdateTransform()
        {
            if (hasAuthority) // If this script is attached to the client's own body on the client's side.
            {
                transform.position = ReferenceTransform.InverseTransformPoint(SyncedTransform.position);
                transform.rotation = ReferenceTransform.InverseTransformRotation(SyncedTransform.rotation);
                return;
            }

            // If this script is attached to any other body, eg the representations of other players
            if (SyncedTransform.position == Vector3.zero)
            {
                // Fix bodies staying at 0,0,0 by chucking them into the sun

                DebugLog.ToConsole("Warning - TransformSync at (0,0,0)!", MessageType.Warning);

                FullStateRequest.Instance.Request();

                SyncedTransform.position = Locator.GetAstroObject(AstroObject.Name.Sun).transform.position;

                return;
            }

            SyncedTransform.parent = ReferenceTransform;

            SyncedTransform.localPosition = Vector3.SmoothDamp(SyncedTransform.localPosition, transform.position, ref _positionSmoothVelocity, SmoothTime);
            SyncedTransform.localRotation = QuaternionHelper.SmoothDamp(SyncedTransform.localRotation, transform.rotation, ref _rotationSmoothVelocity, Time.deltaTime);
        }

    }
}
