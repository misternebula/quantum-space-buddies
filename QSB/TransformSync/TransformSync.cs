﻿using OWML.Common;
using QSB.Utility;
using UnityEngine;
using UnityEngine.Networking;

namespace QSB.TransformSync
{
    public abstract class TransformSync : NetworkBehaviour
    {
        public PlayerInfo Player => PlayerRegistry.GetPlayer(PlayerId);

        private const float SmoothTime = 0.1f;
        private bool _isInitialized;

        public Transform SyncedTransform { get; private set; }
        public Sector ReferenceSector { get; set; }

        private Vector3 _positionSmoothVelocity;
        private Quaternion _rotationSmoothVelocity;

        protected virtual void Awake()
        {
            PlayerRegistry.TransformSyncs.Add(this);
            DontDestroyOnLoad(gameObject);
            LoadManager.OnCompleteSceneLoad += OnCompleteSceneLoad;
        }

        private void OnCompleteSceneLoad(OWScene oldScene, OWScene newScene)
        {
            _isInitialized = false;
        }

        protected abstract Transform InitLocalTransform();
        protected abstract Transform InitRemoteTransform();
        protected abstract bool IsReady { get; }
        public abstract uint PlayerId { get; }

        protected void Init()
        {
            ReferenceSector = GetStartPlanet().GetRootSector();
            SyncedTransform = hasAuthority ? InitLocalTransform() : InitRemoteTransform();
            if (!hasAuthority)
            {
                SyncedTransform.position = ReferenceSector.transform.position;
            }
            _isInitialized = true;
        }

        private void Update()
        {
            if (!_isInitialized && IsReady)
            {
                Init();
            }
            else if (_isInitialized && !IsReady)
            {
                _isInitialized = false;
            }

            if (SyncedTransform == null || !_isInitialized || Player == null || !Player.IsAwake)
            {
                return;
            }

            // Get which sector should be used as a reference point

            if (ReferenceSector == null)
            {
                DebugLog.ToConsole($"Error - TransformSync with id {netId.Value} doesn't have a reference sector", MessageType.Error);
            }

            UpdateTransform();
        }

        protected virtual void UpdateTransform()
        {
            if (hasAuthority) // If this script is attached to the client's own body on the client's side.
            {
                transform.position = ReferenceSector.transform.InverseTransformPoint(SyncedTransform.position);
                transform.rotation = ReferenceSector.transform.InverseTransformRotation(SyncedTransform.rotation);
                return;
            }

            // If this script is attached to any other body, eg the representations of other players
            if (SyncedTransform.position == Vector3.zero)
            {
                // Fix bodies staying at 0,0,0 by chucking them into the sun

                DebugLog.ToConsole("Warning - TransformSync at (0,0,0)!", MessageType.Warning);

                SyncedTransform.position = GetStartPlanet().GetRootSector().transform.position;

                return;
            }

            SyncedTransform.localPosition = Vector3.SmoothDamp(SyncedTransform.localPosition, transform.position, ref _positionSmoothVelocity, SmoothTime);
            SyncedTransform.localRotation = QuaternionHelper.SmoothDamp(SyncedTransform.localRotation, transform.rotation, ref _rotationSmoothVelocity, Time.deltaTime);
        }

        public void SetReferenceSector(Sector sector)
        {
            ReferenceSector = sector;
            SyncedTransform.parent = ReferenceSector.transform;
            SyncedTransform.localPosition += sector.transform.position - SyncedTransform.parent.position;
            transform.position = ReferenceSector.transform.InverseTransformPoint(SyncedTransform.position);
            transform.rotation = ReferenceSector.transform.InverseTransformRotation(SyncedTransform.rotation);
        }

        private AstroObject GetStartPlanet()
        {
            return LoadManager.GetCurrentScene() == OWScene.SolarSystem
                ? Locator.GetAstroObject(AstroObject.Name.TimberHearth)
                : Locator.GetAstroObject(AstroObject.Name.Eye);
        }
    }
}
