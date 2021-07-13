using QSB.Events;
using UnityEngine;

namespace QSB.ProbeSync
{
	internal class ProbeListener : MonoBehaviour
	{
		private SurveyorProbe _attachedProbe;

		public void Init(SurveyorProbe localProbe)
		{
			_attachedProbe = localProbe;
			_attachedProbe.OnLaunchProbe += OnLaunchProbe;
			_attachedProbe.OnAnchorProbe += OnAnchorProbe;
			_attachedProbe.OnUnanchorProbe += OnUnanchorProbe;
			_attachedProbe.OnRetrieveProbe += OnRetrieveProbe;
			_attachedProbe.OnProbeDestroyed += OnProbeDestroyed;
			_attachedProbe.OnStartRetrieveProbe += OnStartRetrieveProbe;
		}

		private void OnDestroy()
		{
			if (_attachedProbe == null)
			{
				return;
			}

			_attachedProbe.OnLaunchProbe -= OnLaunchProbe;
			_attachedProbe.OnAnchorProbe -= OnAnchorProbe;
			_attachedProbe.OnUnanchorProbe -= OnUnanchorProbe;
			_attachedProbe.OnRetrieveProbe -= OnRetrieveProbe;
			_attachedProbe.OnProbeDestroyed -= OnProbeDestroyed;
			_attachedProbe.OnStartRetrieveProbe -= OnStartRetrieveProbe;
		}

		private void OnLaunchProbe()
			=> QSBEventManager.FireEvent(EventNames.QSBProbeEvent, ProbeEvent.Launch);

		private void OnAnchorProbe()
			=> QSBEventManager.FireEvent(EventNames.QSBProbeEvent, ProbeEvent.Anchor);

		private void OnUnanchorProbe()
			=> QSBEventManager.FireEvent(EventNames.QSBProbeEvent, ProbeEvent.Unanchor);

		private void OnRetrieveProbe()
			=> QSBEventManager.FireEvent(EventNames.QSBProbeEvent, ProbeEvent.Retrieve);

		private void OnProbeDestroyed()
			=> QSBEventManager.FireEvent(EventNames.QSBProbeEvent, ProbeEvent.Destroy);

		private void OnStartRetrieveProbe(float length)
			=> QSBEventManager.FireEvent(EventNames.QSBProbeStartRetrieve, length);

		public void OnRenderObject()
		{
			if (!QSBCore.WorldObjectsReady || !QSBCore.DebugMode || !QSBCore.ShowLinesInDebug)
			{
				return;
			}

			Popcron.Gizmos.Line(_attachedProbe.transform.position, Locator.GetPlayerTransform().position, Color.blue);
		}
	}
}
