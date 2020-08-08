﻿using QSB.Tools;
using UnityEngine;

namespace QSB.TransformSync
{
    public class PlayerProbeSync : TransformSync
    {
        public static PlayerProbeSync LocalInstance { get; private set; }

        public Transform bodyTransform;

        public override void OnStartLocalPlayer()
        {
            LocalInstance = this;
        }

        protected override uint PlayerId => netId.Value - 3;

        private Transform GetProbe()
        {
            return Locator.GetProbe().transform.Find("CameraPivot").Find("Geometry");
        }

        protected override Transform InitLocalTransform()
        {
            var body = GetProbe();

            bodyTransform = body;

            Player.ProbeBody = body.gameObject;

            return body;
        }

        protected override Transform InitRemoteTransform()
        {
            var probe = GetProbe();

            probe.gameObject.SetActive(false);
            var body = Instantiate(probe);
            probe.gameObject.SetActive(true);

            Destroy(body.GetComponentInChildren<ProbeAnimatorController>());

            PlayerToolsManager.CreateProbe(body, Player);

            bodyTransform = body;

            Player.ProbeBody = body.gameObject;

            return body;
        }

        protected override void UpdateTransform()
        {
            base.UpdateTransform();
            if (Player.GetState(State.ProbeActive))
            {
                return;
            }
            if (hasAuthority)
            {
                transform.position = ReferenceTransform.InverseTransformPoint(Player.ProbeLauncher.ToolGameObject.transform.position);
                return;
            }
            if (SyncedTransform.position == Vector3.zero ||
                SyncedTransform.position == Locator.GetAstroObject(AstroObject.Name.Sun).transform.position)
            {
                return;
            }
            SyncedTransform.localPosition = ReferenceTransform.InverseTransformPoint(Player.ProbeLauncher.ToolGameObject.transform.position);
        }

        protected override bool IsReady => Locator.GetProbe() != null && Player != null;
    }
}
