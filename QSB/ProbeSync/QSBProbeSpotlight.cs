using QSB.Utility;
using System.Linq;
using UnityEngine;

namespace QSB.ProbeSync
{
	internal class QSBProbeSpotlight : MonoBehaviour
	{
		public ProbeCamera.ID _id;
		public float _fadeInLength = 1f;
		public float _intensity;

		private QSBProbe _probe;
		private OWLight2 _light;
		private bool _inFlight;
		private float _timer;

		private void Awake()
		{
			_probe = Resources.FindObjectsOfTypeAll<QSBProbe>().First(x => gameObject.transform.IsChildOf(x.transform));
			if (_probe == null)
			{
				DebugLog.ToConsole($"Error - Couldn't find QSBProbe!", OWML.Common.MessageType.Error);
			}

			_light = GetComponent<OWLight2>();
			//_intensity = _light.GetLight().intensity;
			_light.GetLight().enabled = false;
			enabled = false;
			_probe.OnLaunchProbe += OnLaunch;
			_probe.OnAnchorProbe += OnAnchorOrRetrieve;
			_probe.OnRetrieveProbe += OnAnchorOrRetrieve;
		}

		private void OnDestroy()
		{
			_probe.OnLaunchProbe -= OnLaunch;
			_probe.OnAnchorProbe -= OnAnchorOrRetrieve;
			_probe.OnRetrieveProbe -= OnAnchorOrRetrieve;
		}

		private void Update()
		{
			_timer += Time.deltaTime;
			var num = Mathf.Clamp01(_timer / _fadeInLength);
			var intensityScale = (2f - num) * num * _intensity;
			_light.SetIntensityScale(intensityScale);
		}

		private void StartFadeIn()
		{
			if (!enabled)
			{
				_light.GetLight().enabled = true;
				_light.SetIntensityScale(0f);
				_timer = 0f;
				enabled = true;
			}
		}

		private void OnLaunch()
		{
			if (_id == ProbeCamera.ID.Forward)
			{
				StartFadeIn();
			}

			_inFlight = true;
		}

		private void OnAnchorOrRetrieve()
		{
			_light.GetLight().enabled = false;
			enabled = false;
			_inFlight = false;
		}
	}
}
