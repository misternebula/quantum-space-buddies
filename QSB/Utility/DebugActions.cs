using OWML.Utils;
using QSB.ShipSync;
using UnityEngine;

namespace QSB.Utility
{
	public class DebugActions : MonoBehaviour
	{
		private void GoToVessel()
		{
			var spawnPoint = GameObject.Find("Spawn_Vessel").GetComponent<SpawnPoint>();

			var playerBody = Locator.GetPlayerBody();
			playerBody.WarpToPositionRotation(spawnPoint.transform.position, spawnPoint.transform.rotation);
			playerBody.SetVelocity(spawnPoint.GetPointVelocity());
		}

		private void InsertWarpCore()
		{
			var warpCore = GameObject.Find("Prefab_NOM_WarpCoreVessel").GetComponent<WarpCoreItem>();
			var socket = GameObject.Find("Interactibles_VesselBridge").GetComponentInChildren<WarpCoreSocket>();
			socket.PlaceIntoSocket(warpCore);
			var bridgeVolume = FindObjectOfType<VesselWarpController>().GetValue<OWTriggerVolume>("_bridgeVolume");
			bridgeVolume.AddObjectToVolume(Locator.GetPlayerDetector());
			bridgeVolume.AddObjectToVolume(Locator.GetPlayerCameraDetector());
		}

		private void DamageShipElectricalSystem() => ShipManager.Instance.ShipElectricalComponent.SetDamaged(true);

		public void Update()
		{
			if (!QSBCore.DebugMode)
			{
				return;
			}

			if (Input.GetKeyDown(KeyCode.Keypad4))
			{
				DamageShipElectricalSystem();
			}

			if (Input.GetKeyDown(KeyCode.Keypad7))
			{
				GoToVessel();
			}

			if (Input.GetKeyDown(KeyCode.Keypad8))
			{
				InsertWarpCore();
			}

			if (Input.GetKeyDown(KeyCode.Keypad9))
			{
				LoadManager.LoadSceneAsync(OWScene.EyeOfTheUniverse, true, LoadManager.FadeType.ToWhite);
			}
		}
	}
}