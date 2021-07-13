using QSB.Utility;
using System.Linq;
using UnityEngine;

namespace QSB.ProbeSync
{
	internal class QSBProbeLantern : MonoBehaviour
	{
		public float _fadeInDuration;
		public AnimationCurve _fadeInCurve;
		public AnimationCurve _fadeOutCurve;
		public OWEmissiveRenderer _emissiveRenderer;
		public float _originalRange;

		private QSBProbe _probe;
		private OWLight2 _light;
		private float _fadeFraction;
		private float _targetFade;
		private float _startFade;
		private float _startFadeTime;
		private float _fadeDuration;

		private void Awake()
		{
			_probe = Resources.FindObjectsOfTypeAll<QSBProbe>().First(x => gameObject.transform.IsChildOf(x.transform));
			if (_probe == null)
			{
				DebugLog.ToConsole($"Error - Couldn't find QSBProbe!", OWML.Common.MessageType.Error);
			}

			_light = GetComponent<OWLight2>();
			_probe.OnAnchorProbe += OnProbeAnchorToSurface;
			_probe.OnStartRetrieveProbe += OnStartRetrieveProbe;
			_probe.OnRetrieveProbe += OnFinishRetrieveProbe;
		}

		private void Start()
		{
			if (_emissiveRenderer != null)
			{
				_emissiveRenderer.SetEmissiveScale(0f);
			}

			_light.GetLight().enabled = false;
			//_originalRange = _light.range;
			enabled = false;
		}

		private void OnDestroy()
		{
			_probe.OnAnchorProbe -= OnProbeAnchorToSurface;
			_probe.OnStartRetrieveProbe -= OnStartRetrieveProbe;
			_probe.OnRetrieveProbe -= OnFinishRetrieveProbe;
		}

		private void Update()
		{
			var animationCurve = (_targetFade <= 0f)
				? _fadeOutCurve
				: _fadeInCurve;

			var fadeTime = Mathf.InverseLerp(_startFadeTime, _startFadeTime + _fadeDuration, Time.time);
			_fadeFraction = Mathf.Lerp(_startFade, _targetFade, animationCurve.Evaluate(fadeTime));

			var probeRuleSet = _probe.GetRulesetDetector().GetProbeRuleSet();

			var lanternRange = (!(probeRuleSet != null) || !probeRuleSet.GetOverrideLanternRange())
				? _originalRange
				: probeRuleSet.GetLanternRange();

			_light.range = lanternRange * _fadeFraction;

			if (_emissiveRenderer != null)
			{
				_emissiveRenderer.SetEmissiveScale(_fadeFraction);
			}

			if (fadeTime >= 1f)
			{
				enabled = false;
			}
		}

		private void FadeTo(float fade, float duration)
		{
			_startFadeTime = Time.time;
			_fadeDuration = duration;
			_startFade = _fadeFraction;
			_targetFade = fade;
			enabled = true;
		}

		private void OnProbeAnchorToSurface()
		{
			if (!_probe.IsRetrieving())
			{
				_light.GetLight().enabled = true;
				_light.range = 0f;
				FadeTo(1f, _fadeInDuration);
			}
		}

		private void OnStartRetrieveProbe(float retrieveLength)
			=> FadeTo(0f, retrieveLength);

		private void OnFinishRetrieveProbe()
		{
			_light.GetLight().enabled = false;
			_light.range = 0f;
			_fadeFraction = 0f;
			enabled = false;
		}
	}
}
