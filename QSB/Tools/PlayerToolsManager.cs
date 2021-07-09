using OWML.Utils;
using QSB.Player;
using QSB.ProbeSync;
using QSB.Utility;
using System.Linq;
using UnityEngine;
using UnityEngine.PostProcessing;

namespace QSB.Tools
{
	public class PlayerToolsManager
	{
		private static Transform _toolStowTransform;
		private static Transform _toolHoldTransform;
		private static Material _playerToolsMaterial;
		private static Material _lightbulbMaterial;

		private static readonly Vector3 FlashlightOffset = new Vector3(0.7196316f, -0.2697681f, 0.3769455f);
		private static readonly Vector3 ProbeLauncherOffset = new Vector3(0.5745087f, -0.26f, 0.4453125f);
		private static readonly Vector3 SignalscopeScale = new Vector3(1.5f, 1.5f, 1.5f);
		private static readonly Vector3 TranslatorScale = new Vector3(0.75f, 0.75f, 0.75f);

		public static void Init(Transform playerCamera)
		{
			CreateStowTransforms(playerCamera);

			_playerToolsMaterial = GameObject.Find("Props_HEA_ProbeLauncher_ProbeCamera/ProbeLauncherChassis").GetComponent<MeshRenderer>().materials[0];
			if (QSBSceneManager.CurrentScene == OWScene.SolarSystem)
			{
				_lightbulbMaterial = GameObject.Find("Props_HEA_Lantern (10)/Lantern_Lamp").GetComponent<MeshRenderer>().materials[0];
			}
			else if (QSBSceneManager.CurrentScene == OWScene.EyeOfTheUniverse)
			{
				_lightbulbMaterial = GameObject.Find("lantern_lamp").GetComponent<MeshRenderer>().materials[0];
			}

			CreateFlashlight(playerCamera);
			CreateSignalscope(playerCamera);
			CreateProbeLauncher(playerCamera);
			CreateTranslator(playerCamera);
		}

		public static void CreateProbe(Transform newProbe, PlayerInfo player)
		{
			var qsbProbe = newProbe.gameObject.AddComponent<QSBProbe>();
			player.Probe = qsbProbe;
			qsbProbe.SetOwner(player);

			// Probe_Body
			Object.Destroy(newProbe.GetComponent<ProbeAnchor>());
			Object.Destroy(newProbe.GetComponent<AlignWithFluid>());
			Object.Destroy(newProbe.GetComponent<MapMarker>());
			Object.Destroy(newProbe.GetComponent<ProbeSeeking>());
			Object.Destroy(newProbe.GetComponent<SurveyorProbe>());
			Object.Destroy(newProbe.GetComponent<ProbeHUDMarker>());
			Object.Destroy(newProbe.GetComponent<LightFlickerController>());
			Object.Destroy(newProbe.GetComponent<OWRigidbody>());
			Object.Destroy(newProbe.GetComponent<Rigidbody>());
			Object.Destroy(newProbe.GetComponent<CenterOfTheUniverseOffsetApplier>());

			// ProbeDetector
			//Object.Destroy(newProbe.Find("ProbeDetector").gameObject);
			var probeDetector = newProbe.Find("ProbeDetector").gameObject;
			Object.Destroy(probeDetector.GetComponent<ForceApplier>());
			Object.Destroy(probeDetector.GetComponent<ProbeFluidDetector>());
			Object.Destroy(probeDetector.GetComponent<AlignmentForceDetector>());

			// CameraPivot
			var cameraPivot = newProbe.Find("CameraPivot");
			Object.Destroy(cameraPivot.GetComponent<ProbeHorizonTracker>());

			// TODO : Sync probe animations

			// CameraPivot/Geometry/Props_HEA_Probe_ANIM
			var animRoot = cameraPivot.Find("Geometry").Find("Props_HEA_Probe_ANIM");
			Object.Destroy(animRoot.GetComponent<ProbeAnimatorController>());
			Object.Destroy(animRoot.GetComponent<Animator>());

			// TODO : Set up QSB cameras for these two cameras - that's why im not just destroying the GOs here

			// CameraPivot/ForwardCamera
			var forwardCamera = cameraPivot.Find("ForwardCamera");
			Object.Destroy(forwardCamera.GetComponent<PostProcessingBehaviour>());
			Object.Destroy(forwardCamera.GetComponent<NoiseImageEffect>());
			Object.Destroy(forwardCamera.GetComponent<PlanetaryFogImageEffect>());
			Object.Destroy(forwardCamera.GetComponent<ProbeCamera>());
			Object.Destroy(forwardCamera.GetComponent<OWCamera>());
			Object.Destroy(forwardCamera.GetComponent<Camera>());
			var oldForwardSpotlight = forwardCamera.GetComponent<ProbeSpotlight>();
			var newForwardSpotlight = forwardCamera.gameObject.AddComponent<QSBProbeSpotlight>();
			newForwardSpotlight._id = oldForwardSpotlight.GetValue<ProbeCamera.ID>("_id");
			newForwardSpotlight._fadeInLength = oldForwardSpotlight.GetValue<float>("_fadeInLength");
			newForwardSpotlight._intensity = 0.8f;
			Object.Destroy(oldForwardSpotlight);

			// CameraPivot/RotatingCameraPivot/RotatingCamera
			var rotatingCamera = cameraPivot.Find("RotatingCameraPivot").Find("RotatingCamera");
			Object.Destroy(rotatingCamera.GetComponent<PostProcessingBehaviour>());
			Object.Destroy(rotatingCamera.GetComponent<NoiseImageEffect>());
			Object.Destroy(rotatingCamera.GetComponent<PlanetaryFogImageEffect>());
			Object.Destroy(rotatingCamera.GetComponent<ProbeCamera>());
			Object.Destroy(rotatingCamera.GetComponent<OWCamera>());
			Object.Destroy(rotatingCamera.GetComponent<Camera>());

			// ProbeEffects
			var probeEffects = newProbe.Find("ProbeEffects");
			var oldEffects = probeEffects.GetComponent<ProbeEffects>();
			var newEffects = probeEffects.gameObject.AddComponent<QSBProbeEffects>();
			newEffects._flightLoopAudio = oldEffects.GetValue<OWAudioSource>("_flightLoopAudio");
			newEffects._anchorAudio = oldEffects.GetValue<OWAudioSource>("_anchorAudio");
			newEffects._anchorParticles = oldEffects.GetValue<ParticleSystem>("_anchorParticles");
			Object.Destroy(oldEffects);

			Object.Destroy(probeEffects.Find("CloudsEffectBubble").gameObject);
			Object.Destroy(probeEffects.Find("SandEffectBubble").gameObject);
			Object.Destroy(probeEffects.Find("ProbeElectricityEffect").gameObject);

			// AmbientLight_Probe
			var ambientLight = newProbe.Find("AmbientLight_Probe");
			var oldAmbLantern = ambientLight.GetComponent<ProbeLantern>();
			var newAmbLantern = ambientLight.gameObject.AddComponent<QSBProbeLantern>();
			newAmbLantern._fadeInDuration = oldAmbLantern.GetValue<float>("_fadeInDuration");
			newAmbLantern._fadeInCurve = oldAmbLantern.GetValue<AnimationCurve>("_fadeInCurve");
			newAmbLantern._fadeOutCurve = oldAmbLantern.GetValue<AnimationCurve>("_fadeOutCurve");
			newAmbLantern._emissiveRenderer = oldAmbLantern.GetValue<OWEmissiveRenderer>("_emissiveRenderer");
			newAmbLantern._originalRange = 60f;
			Object.Destroy(oldAmbLantern);

			// Lantern
			var lantern = newProbe.Find("Lantern");
			var oldLantern = lantern.GetComponent<ProbeLantern>();
			var newLantern = lantern.gameObject.AddComponent<QSBProbeLantern>();
			newLantern._fadeInDuration = oldLantern.GetValue<float>("_fadeInDuration");
			newLantern._fadeInCurve = oldLantern.GetValue<AnimationCurve>("_fadeInCurve");
			newLantern._fadeOutCurve = oldLantern.GetValue<AnimationCurve>("_fadeOutCurve");
			newLantern._emissiveRenderer = oldLantern.GetValue<OWEmissiveRenderer>("_emissiveRenderer");
			newLantern._originalRange = 35f;
			Object.Destroy(oldLantern);

			// RearCamera
			var rearCamera = newProbe.Find("RearCamera");
			Object.Destroy(rearCamera.GetComponent<PostProcessingBehaviour>());
			Object.Destroy(rearCamera.GetComponent<NoiseImageEffect>());
			Object.Destroy(rearCamera.GetComponent<PlanetaryFogImageEffect>());
			Object.Destroy(rearCamera.GetComponent<ProbeCamera>());
			Object.Destroy(rearCamera.GetComponent<OWCamera>());
			Object.Destroy(rearCamera.GetComponent<Camera>());
			var oldRearSpotlight = rearCamera.GetComponent<ProbeSpotlight>();
			var newRearSpotlight = rearCamera.gameObject.AddComponent<QSBProbeSpotlight>();
			newRearSpotlight._id = oldRearSpotlight.GetValue<ProbeCamera.ID>("_id");
			newRearSpotlight._fadeInLength = oldRearSpotlight.GetValue<float>("_fadeInLength");
			newRearSpotlight._intensity = 0.8f;
			Object.Destroy(oldRearSpotlight);

			// PlaneOffsetMarker_Probe
			Object.Destroy(newProbe.Find("PlaneOffsetMarker_Probe").gameObject);

			newProbe.Find("RecallEffect").gameObject.GetComponent<SingularityController>().enabled = true;
			newProbe.Find("RecallEffect").gameObject.GetComponent<SingularityWarpEffect>().enabled = true;
			newProbe.Find("RecallEffect").name = "RemoteProbeRecallEffect";

			newProbe.gameObject.SetActive(true);
		}

		private static void CreateStowTransforms(Transform cameraBody)
		{
			var stow = new GameObject("ToolStowTransform");
			_toolStowTransform = stow.transform;
			stow.transform.parent = cameraBody;
			stow.transform.localPosition = Vector3.zero;
			stow.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);

			var hold = new GameObject("ToolHoldTransform");
			_toolHoldTransform = hold.transform;
			hold.transform.parent = cameraBody;
			hold.transform.localPosition = Vector3.zero;
			hold.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

			var item = new GameObject("ItemSocket");
			item.transform.parent = cameraBody;
			item.transform.localPosition = new Vector3(0.125f, -0.12f, 0.2f);
			item.transform.localRotation = Quaternion.Euler(0, 0, 15);
			item.transform.localScale = new Vector3(0.33f, 0.33f, 0.33f);

			var scroll = new GameObject("ScrollSocket");
			scroll.transform.parent = cameraBody;
			scroll.transform.localPosition = new Vector3(0.148f, -0.0522f, 0.2465f);
			scroll.transform.localRotation = Quaternion.Euler(236.054f, 56.46799f, -152.472f);
			scroll.transform.localScale = new Vector3(0.33f, 0.33f, 0.33f);

			var sharedStone = new GameObject("SharedStoneSocket");
			sharedStone.transform.parent = cameraBody;
			sharedStone.transform.localPosition = new Vector3(0.1375f, -0.119f, 0.2236f);
			sharedStone.transform.localRotation = Quaternion.Euler(-23.053f, -0.263f, 6.704f);
			sharedStone.transform.localScale = new Vector3(0.33f, 0.33f, 0.33f);

			var warpCore = new GameObject("WarpCoreSocket");
			warpCore.transform.parent = cameraBody;
			warpCore.transform.localPosition = new Vector3(0.161f, -0.107f, 0.223f);
			warpCore.transform.localRotation = Quaternion.Euler(179.949f, 82.59f, 163.697f);
			warpCore.transform.localScale = new Vector3(0.33f, 0.33f, 0.33f);

			var vesselCore = new GameObject("VesselCoreSocket");
			vesselCore.transform.parent = cameraBody;
			vesselCore.transform.localPosition = new Vector3(0.177f, -0.106f, 0.2f);
			vesselCore.transform.localRotation = Quaternion.Euler(3.142f, 14.827f, 12.094f);
			vesselCore.transform.localScale = new Vector3(0.27f, 0.27f, 0.27f);
		}

		private static void CreateFlashlight(Transform cameraBody)
		{
			var flashlightRoot = Object.Instantiate(GameObject.Find("FlashlightRoot"));
			flashlightRoot.name += "-RemotePlayer";
			flashlightRoot.SetActive(false);
			var oldComponent = flashlightRoot.GetComponent<Flashlight>();
			var component = flashlightRoot.AddComponent<QSBFlashlight>();

			component.Init(oldComponent);
			oldComponent.enabled = false;

			flashlightRoot.transform.parent = cameraBody;
			flashlightRoot.transform.localPosition = FlashlightOffset;
			flashlightRoot.SetActive(true);
		}

		private static void CreateSignalscope(Transform cameraBody)
		{
			var signalscopeRoot = Object.Instantiate(GameObject.Find("Signalscope"));
			signalscopeRoot.SetActive(false);

			Object.Destroy(signalscopeRoot.GetComponent<SignalscopePromptController>());
			Object.Destroy(signalscopeRoot.transform.Find("Props_HEA_Signalscope")
				.Find("Props_HEA_Signalscope_Prepass").gameObject);

			var oldSignalscope = signalscopeRoot.GetComponent<Signalscope>();
			var tool = signalscopeRoot.AddComponent<QSBTool>();
			tool.MoveSpring = oldSignalscope.GetValue<DampedSpringQuat>("_moveSpring");
			tool.StowTransform = _toolStowTransform;
			tool.HoldTransform = _toolHoldTransform;
			tool.ArrivalDegrees = 5f;
			tool.Type = ToolType.Signalscope;
			tool.ToolGameObject = signalscopeRoot.transform.Find("Props_HEA_Signalscope").gameObject;
			oldSignalscope.enabled = false;

			GetRenderer(signalscopeRoot, "Props_HEA_Signalscope").material = _playerToolsMaterial;

			signalscopeRoot.transform.parent = cameraBody;
			signalscopeRoot.transform.localPosition = Vector3.zero;
			signalscopeRoot.transform.localScale = SignalscopeScale;
			signalscopeRoot.SetActive(true);
		}

		private static void CreateTranslator(Transform cameraBody)
		{
			var original = GameObject.Find("NomaiTranslatorProp");

			var translatorRoot = original.InstantiateInactive();

			var group = translatorRoot.transform.Find("TranslatorGroup");
			var model = group.Find("Props_HEA_Translator");

			Object.Destroy(translatorRoot.GetComponent<NomaiTranslatorProp>());
			Object.Destroy(group.Find("Canvas").gameObject);
			Object.Destroy(group.Find("Lighting").gameObject);
			Object.Destroy(group.Find("TranslatorBeams").gameObject);
			Object.Destroy(model.Find("Props_HEA_Translator_Pivot_RotatingPart")
				.Find("Props_HEA_Translator_RotatingPart")
				.Find("Props_HEA_Translator_RotatingPart_Prepass").gameObject);
			Object.Destroy(model.Find("Props_HEA_Translator_Prepass").gameObject);

			var oldTranslator = translatorRoot.GetComponent<NomaiTranslator>();
			var tool = translatorRoot.AddComponent<QSBTool>();
			tool.MoveSpring = oldTranslator.GetValue<DampedSpringQuat>("_moveSpring");
			tool.StowTransform = _toolStowTransform;
			tool.HoldTransform = _toolHoldTransform;
			tool.ArrivalDegrees = 5f;
			tool.Type = ToolType.Translator;
			tool.ToolGameObject = group.gameObject;
			Object.Destroy(oldTranslator);

			GetRenderer(translatorRoot, "Props_HEA_Translator_Geo").material = _playerToolsMaterial;
			GetRenderer(translatorRoot, "Props_HEA_Translator_RotatingPart").material = _playerToolsMaterial;
			GetRenderer(translatorRoot, "Props_HEA_Translator_Button_L").material = _lightbulbMaterial;
			GetRenderer(translatorRoot, "Props_HEA_Translator_Button_R").material = _lightbulbMaterial;

			translatorRoot.transform.parent = cameraBody;
			translatorRoot.transform.localPosition = Vector3.zero;
			translatorRoot.transform.localScale = TranslatorScale;
			QSBCore.UnityEvents.FireOnNextUpdate(() => translatorRoot.SetActive(true));
		}

		private static void CreateProbeLauncher(Transform cameraBody)
		{
			var launcherRoot = new GameObject("ProbeLauncher");
			var modelOrig = GameObject.Find("PlayerCamera/ProbeLauncher/Props_HEA_ProbeLauncher");
			var model = Object.Instantiate(modelOrig);
			model.transform.parent = launcherRoot.transform;

			Object.Destroy(model.transform.Find("Props_HEA_ProbeLauncher_Prepass").gameObject);
			Object.Destroy(model.transform.Find("Props_HEA_Probe_Prelaunch").Find("Props_HEA_Probe_Prelaunch_Prepass").gameObject);

			var tool = launcherRoot.AddComponent<QSBTool>();
			var spring = new DampedSpringQuat
			{
				velocity = Vector4.zero,
				settings = new DampedSpringSettings
				{
					springConstant = 50f,
					dampingCoefficient = 8.485282f,
					mass = 1
				}
			};
			tool.MoveSpring = spring;
			tool.StowTransform = _toolStowTransform;
			tool.HoldTransform = _toolHoldTransform;
			tool.ArrivalDegrees = 5f;
			tool.Type = ToolType.ProbeLauncher;
			tool.ToolGameObject = model;

			// TODO : investigate why probe is wack
			GetRenderer(launcherRoot, "Props_HEA_Probe_Prelaunch").materials[0] = _playerToolsMaterial;
			GetRenderer(launcherRoot, "Props_HEA_Probe_Prelaunch").materials[1] = _lightbulbMaterial;
			GetRenderer(launcherRoot, "PressureGauge_Arrow").material = _playerToolsMaterial;
			GetRenderer(launcherRoot, "ProbeLauncherChassis").material = _playerToolsMaterial;

			launcherRoot.transform.parent = cameraBody;
			launcherRoot.transform.localPosition = ProbeLauncherOffset;
			launcherRoot.SetActive(true);
		}

		private static MeshRenderer GetRenderer(GameObject root, string gameObjectName) =>
			root.GetComponentsInChildren<MeshRenderer>(true).First(x => x.name == gameObjectName);
	}
}