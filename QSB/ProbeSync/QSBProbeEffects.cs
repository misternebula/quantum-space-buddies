using QSB.Utility;
using System.Linq;
using UnityEngine;

namespace QSB.ProbeSync
{
	internal class QSBProbeEffects : MonoBehaviour
	{
		public OWAudioSource _flightLoopAudio;
		public OWAudioSource _anchorAudio;
		public ParticleSystem _anchorParticles;
		public ParticleSystem _underwaterAnchorParticles;

		private QSBProbe _probe;

		private void Awake()
		{
			_probe = Resources.FindObjectsOfTypeAll<QSBProbe>().First(x => gameObject.transform.IsChildOf(x.transform));
			if (_probe == null)
			{
				DebugLog.ToConsole($"Error - Couldn't find QSBProbe!", OWML.Common.MessageType.Error);
			}

			_probe.OnLaunchProbe += OnLaunch;
			_probe.OnAnchorProbe += OnAnchor;
			_probe.OnUnanchorProbe += OnUnanchor;
			_probe.OnStartRetrieveProbe += OnStartRetrieve;
		}

		private void OnDestroy()
		{
			_probe.OnLaunchProbe -= OnLaunch;
			_probe.OnAnchorProbe -= OnAnchor;
			_probe.OnUnanchorProbe -= OnUnanchor;
			_probe.OnStartRetrieveProbe -= OnStartRetrieve;
		}

		private void OnLaunch() => _flightLoopAudio.FadeIn(0.1f, true, true, 1f);

		private void OnAnchor()
		{
			// TODO : Come up with some other way of doing this

			//if (this._fluidDetector.InFluidType(FluidVolume.Type.WATER))
			//{
			//	this._underwaterAnchorParticles.Play();
			//}
			//else
			//{
			_anchorParticles.Play();
			//}
			_flightLoopAudio.FadeOut(0.5f, OWAudioSource.FadeOutCompleteAction.STOP, 0f);
			_anchorAudio.PlayOneShot(AudioType.ToolProbeAttach, 1f);
		}

		private void OnUnanchor()
			=> _flightLoopAudio.FadeIn(0.5f, false, false, 1f);

		private void OnStartRetrieve(float retrieveLength)
			=> _flightLoopAudio.FadeOut(retrieveLength, OWAudioSource.FadeOutCompleteAction.STOP, 0f);
	}
}
