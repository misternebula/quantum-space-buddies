using QSB.Patches;
using QSB.Player;
using QSB.Utility;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace QSB.QuantumSync.Patches
{
	public class QuantumVisibilityPatches : QSBPatch
	{
		public override QSBPatchTypes Type => QSBPatchTypes.OnClientConnect;

		public override void DoPatches()
		{
			Prefix(nameof(ShapeVisibilityTracker_IsVisibleUsingCameraFrustum));
			Prefix(nameof(ShapeVisibilityTracker_IsVisible));
			Prefix(nameof(RendererVisibilityTracker_IsVisibleUsingCameraFrustum));
			Prefix(nameof(VisibilityObject_CheckIllumination));
			Postfix(nameof(Shape_OnEnable));
			Postfix(nameof(Shape_OnDisable));
		}

		public static void Shape_OnEnable(Shape __instance)
			=> __instance.RaiseEvent("OnShapeActivated", __instance);

		public static void Shape_OnDisable(Shape __instance)
			=> __instance.RaiseEvent("OnShapeDeactivated", __instance);

		// ShapeVisibilityTracker patches

		public static bool ShapeVisibilityTracker_IsVisibleUsingCameraFrustum(ShapeVisibilityTracker __instance, ref bool __result)
		{
			__result = QuantumManager.IsVisibleUsingCameraFrustum(__instance, false).First;
			return false;
		}

		public static bool ShapeVisibilityTracker_IsVisible(ShapeVisibilityTracker __instance, ref bool __result)
		{
			__result = QuantumManager.IsVisible(__instance, false);
			return false;
		}

		// RendererVisibilityTracker patches - probably not needed as i don't think RendererVisibilityTracker is ever used?

		public static bool RendererVisibilityTracker_IsVisibleUsingCameraFrustum(RendererVisibilityTracker __instance, ref bool __result, Renderer ____renderer, bool ____checkFrustumOcclusion)
		{
			__result = QSBPlayerManager.GetPlayersWithCameras()
					.Any(x => GeometryUtility.TestPlanesAABB(x.Camera.GetFrustumPlanes(), ____renderer.bounds))
				&& (!____checkFrustumOcclusion || QSBPlayerManager.GetPlayersWithCameras()
					.Any(x => !(bool)__instance.GetType()
					.GetMethod("IsOccludedFromPosition", BindingFlags.NonPublic | BindingFlags.Instance)
					.Invoke(__instance, new object[] { x.Camera.transform.position })));
			return false;
		}

		// VisibilityObject

		public static bool VisibilityObject_CheckIllumination(VisibilityObject __instance, ref bool __result, bool ____checkIllumination, Vector3 ____localIlluminationOffset, float ____illuminationRadius, Light[] ____lightSources)
		{
			if (!____checkIllumination)
			{
				__result = true;
				return false;
			}

			var point = __instance.transform.TransformPoint(____localIlluminationOffset);
			var tupleFlashlights = QSBPlayerManager.GetPlayerFlashlights();
			var localFlashlight = tupleFlashlights.First;
			var playerFlashlights = tupleFlashlights.Second;

			// local player flashlight
			if (localFlashlight.CheckIlluminationAtPoint(point, ____illuminationRadius))
			{
				__result = true;
				return false;
			}

			// all other player flashlights
			if (playerFlashlights.Any(x => x.CheckIlluminationAtPoint(point, ____illuminationRadius)))
			{
				__result = true;
				return false;
			}

			// TODO : Implement checking for other probes!
			if (Locator.GetProbe() != null && Locator.GetProbe().IsLaunched() && Locator.GetProbe().CheckIlluminationAtPoint(point, ____illuminationRadius))
			{
				__result = true;
				return false;
			}

			// TODO : Implement checking for other player's thrusters!
			if (Locator.GetThrusterLightTracker().CheckIlluminationAtPoint(point, ____illuminationRadius))
			{
				__result = true;
				return false;
			}

			if (____lightSources != null)
			{
				foreach (var light in ____lightSources)
				{
					if (light.intensity > 0f && light.range > 0f)
					{
						__result = true;
						return false;
					}
				}
			}

			__result = false;
			return false;
		}
	}
}
