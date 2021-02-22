﻿using QSB.Player;
using UnityEngine;

namespace QSB.TransformSync
{
	public class ShipTransformSync : TransformSync
	{
		private Transform GetShipModel() => Locator.GetShipTransform();

		protected override Transform InitLocalTransform() =>
			GetShipModel().Find("Module_Cockpit/Geo_Cockpit/Cockpit_Geometry/Cockpit_Exterior");

		protected override Transform InitRemoteTransform()
		{
			var shipModel = GetShipModel();

			var remoteTransform = new GameObject("RemoteShipTransform").transform;

			Instantiate(shipModel.Find("Module_Cockpit/Geo_Cockpit/Cockpit_Geometry/Cockpit_Exterior"), remoteTransform);
			Instantiate(shipModel.Find("Module_Cabin/Geo_Cabin/Cabin_Geometry/Cabin_Exterior"), remoteTransform);
			Instantiate(shipModel.Find("Module_Cabin/Geo_Cabin/Cabin_Tech/Cabin_Tech_Exterior"), remoteTransform);
			Instantiate(shipModel.Find("Module_Supplies/Geo_Supplies/Supplies_Geometry/Supplies_Exterior"), remoteTransform);
			Instantiate(shipModel.Find("Module_Engine/Geo_Engine/Engine_Geometry/Engine_Exterior"), remoteTransform);

			var landingGearFront = Instantiate(shipModel.Find("Module_LandingGear/LandingGear_Front/Geo_LandingGear_Front"), remoteTransform);
			var landingGearLeft = Instantiate(shipModel.Find("Module_LandingGear/LandingGear_Left/Geo_LandingGear_Left"), remoteTransform);
			var landingGearRight = Instantiate(shipModel.Find("Module_LandingGear/LandingGear_Right/Geo_LandingGear_Right"), remoteTransform);

			Destroy(landingGearFront.Find("LandingGear_FrontCollision").gameObject);
			Destroy(landingGearLeft.Find("LandingGear_LeftCollision").gameObject);
			Destroy(landingGearRight.Find("LandingGear_RightCollision").gameObject);

			landingGearFront.localPosition
				= landingGearLeft.localPosition
				= landingGearRight.localPosition
				+= Vector3.up * 3.762f;

			return remoteTransform;
		}

		public override bool IsReady => GetShipModel() != null
			&& Player != null
			&& PlayerManager.PlayerExists(Player.PlayerId)
			&& Player.IsReady
			&& NetId.Value != uint.MaxValue
			&& NetId.Value != 0U;
	}
}