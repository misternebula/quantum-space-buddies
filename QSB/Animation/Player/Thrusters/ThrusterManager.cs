using OWML.Utils;
using QSB.Player;
using QSB.Utility;
using UnityEngine;

namespace QSB.Animation.Player.Thrusters
{
	internal class ThrusterManager
	{
		public static void CreateRemotePlayerVFX(PlayerInfo player)
		{
			var localPlayerVfx = GameObject.Find("PlayerVFX");
			var newVfx = localPlayerVfx.InstantiateInactive();

			ReplaceParticleSystems(newVfx, player);
			CreatePlayerParticlesController(newVfx);
			CreateThrusterParticlesBehaviour(newVfx, player);
			CreateThrusterWashController(newVfx.transform.Find("ThrusterWash").gameObject, player);
			CreateThrusterFlameController(newVfx, player);

			newVfx.transform.parent = player.Body.transform;
			newVfx.transform.localPosition = Vector3.zero;
			newVfx.transform.rotation = Quaternion.Euler(0, 0, 0);
			newVfx.transform.localScale = new Vector3(1, 1, 1);

			// Deleted objects take 1 update to actually be deleted
			QSBCore.UnityEvents.FireOnNextUpdate(() => newVfx.SetActive(true));
		}

		private static void ReplaceParticleSystems(GameObject root, PlayerInfo player)
		{
			var existingSystems = root.GetComponentsInChildren<RelativisticParticleSystem>(true);
			foreach (var system in existingSystems)
			{
				var gameObject = system.gameObject;
				Object.Destroy(system);
				var newSys = gameObject.AddComponent<CustomRelativisticParticleSystem>();
				newSys.Init(player);
			}
		}

		private static void CreateThrusterFlameController(GameObject root, PlayerInfo player)
		{
			var existingControllers = root.GetComponentsInChildren<ThrusterFlameController>(true);
			foreach (var controller in existingControllers)
			{
				var gameObject = controller.gameObject;
				var oldThruster = controller.GetValue<Thruster>("_thruster");
				var oldLight = controller.GetValue<Light>("_light");
				var oldAnimCurve = controller.GetValue<AnimationCurve>("_scaleByThrust");
				var oldScaleSpring = controller.GetValue<DampedSpring>("_scaleSpring");
				var oldScalar = controller.GetValue<float>("_belowMaxThrustScalar");
				var oldBase = controller.GetValue<float>("_baseLightRadius");
				Object.Destroy(controller);
				var newObj = gameObject.AddComponent<RemoteThrusterFlameController>();
				newObj.InitFromOld(oldThruster, oldLight, oldAnimCurve, oldScaleSpring, oldScalar, oldBase, player);
			}
		}

		private static void CreatePlayerParticlesController(GameObject root) =>
			// TODO : Implement this. (Footsteps / Landing)
			Object.Destroy(root.GetComponent<PlayerParticlesController>());

		private static void CreateThrusterParticlesBehaviour(GameObject root, PlayerInfo player)
		{
			var existingBehaviours = root.GetComponentsInChildren<ThrusterParticlesBehavior>(true);
			foreach (var behaviour in existingBehaviours)
			{
				// TODO : Implement this. (Bubbles for underwater thrusters)
				Object.Destroy(behaviour);
			}
		}

		private static void CreateThrusterWashController(GameObject root, PlayerInfo player)
		{
			var old = root.GetComponent<ThrusterWashController>();
			var oldDistanceScale = old.GetValue<AnimationCurve>("_emissionDistanceScale");
			var oldThrusterScale = old.GetValue<AnimationCurve>("_emissionThrusterScale");
			var defaultParticleSystem = old.GetValue<ParticleSystem>("_defaultParticleSystem");

			Object.Destroy(old);

			var newObj = root.AddComponent<RemoteThrusterWashController>();
			newObj.InitFromOld(oldDistanceScale, oldThrusterScale, defaultParticleSystem, player);
		}
	}
}
