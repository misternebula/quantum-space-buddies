﻿using System;
using UnityEngine;

namespace QSB
{
    public class ShipTransformSync : TransformSync
    {
        public static ShipTransformSync LocalInstance { get; private set; }
        Transform _shipModel;

        Transform GetShipModel()
        {
            if (!_shipModel)
            {
                _shipModel = Locator.GetShipBody().transform;
            }
            return _shipModel;
        }

        protected override Transform GetLocalTransform()
        {
            LocalInstance = this;
            return GetShipModel().Find("Module_Cockpit/Geo_Cockpit/Cockpit_Geometry/Cockpit_Exterior");
        }

        protected override Transform GetRemoteTransform()
        {
            var shipModel = GetShipModel();
            var cockpit = Instantiate(shipModel.Find("Module_Cockpit/Geo_Cockpit/Cockpit_Geometry/Cockpit_Exterior"));
            var cabin = Instantiate(shipModel.Find("Module_Cabin/Geo_Cabin/Cabin_Geometry/Cabin_Exterior"));
            var supplies = Instantiate(shipModel.Find("Module_Supplies/Geo_Supplies/Supplies_Geometry/Supplies_Exterior"));
            var engine = Instantiate(shipModel.Find("Module_Engine/Geo_Engine/Engine_Geometry/Engine_Exterior"));
            var landingGearFront = Instantiate(shipModel.Find("Module_LandingGear/LandingGear_Front/Geo_LandingGear_Front"));
            var landingGearLeft = Instantiate(shipModel.Find("Module_LandingGear/LandingGear_Left/Geo_LandingGear_Left"));
            var landingGearRight = Instantiate(shipModel.Find("Module_LandingGear/LandingGear_Right/Geo_LandingGear_Right"));

            Destroy(landingGearFront.Find("LandingGear_FrontCollision").gameObject);
            Destroy(landingGearLeft.Find("LandingGear_LeftCollision").gameObject);
            Destroy(landingGearRight.Find("LandingGear_RightCollision").gameObject);

            var remoteTransform = new GameObject().transform;

            cockpit.parent
                = cabin.parent
                = supplies.parent
                = engine.parent
                = landingGearFront.parent
                = landingGearLeft.parent
                = landingGearRight.parent
                = remoteTransform;

            landingGearFront.localPosition
                = landingGearLeft.localPosition
                = landingGearRight.localPosition
                += Vector3.up * 3.762f;

            return remoteTransform;
        }
    }
}
