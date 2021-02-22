﻿using OWML.Utils;
using UnityEngine;

namespace QSB.Player.Tools
{
	public class QSBFlashlight : MonoBehaviour
	{
		private OWLight2[] _lights;
		private Transform _root;
		private Transform _basePivot;
		private Transform _wobblePivot;
		private Vector3 _baseForward;
		private Quaternion _baseRotation;

		public bool FlashlightOn;

		public void Start()
		{
			_baseForward = _basePivot.forward;
			_baseRotation = _basePivot.rotation;
		}

		public void Init(Flashlight oldComponent)
		{
			_lights = oldComponent.GetValue<OWLight2[]>("_lights");
			_root = oldComponent.GetValue<Transform>("_root");
			_basePivot = oldComponent.GetValue<Transform>("_basePivot");
			_wobblePivot = oldComponent.GetValue<Transform>("_wobblePivot");
			Destroy(oldComponent.GetComponent<LightLOD>());

			foreach (var light in _lights)
			{
				light.GetLight().enabled = false;
				light.GetLight().shadows = LightShadows.Soft;
			}
			FlashlightOn = false;
		}

		public void UpdateState(bool value)
		{
			if (value)
			{
				TurnOn();
			}
			else
			{
				TurnOff();
			}
		}

		private void TurnOn()
		{
			if (FlashlightOn)
			{
				return;
			}
			foreach (var light in _lights)
			{
				light.GetLight().enabled = true;
			}
			FlashlightOn = true;
			var rotation = _root.rotation;
			_basePivot.rotation = rotation;
			_baseRotation = rotation;
			_baseForward = _basePivot.forward;
		}

		private void TurnOff()
		{
			if (!FlashlightOn)
			{
				return;
			}
			foreach (var light in _lights)
			{
				light.GetLight().enabled = false;
			}
			FlashlightOn = false;
		}

		public bool CheckIlluminationAtPoint(Vector3 point, float buffer = 0f, float maxDistance = float.PositiveInfinity)
			=> FlashlightOn
				&& _lights[1].CheckIlluminationAtPoint(point, buffer, maxDistance);

		public void FixedUpdate()
		{
			// This really isn't needed... but it makes it look that extra bit nicer. ^_^
			var lhs = Quaternion.FromToRotation(_basePivot.up, _root.up) * Quaternion.FromToRotation(_baseForward, _root.forward);
			var b = lhs * _baseRotation;
			_baseRotation = Quaternion.Slerp(_baseRotation, b, 6f * Time.deltaTime);
			_basePivot.rotation = _baseRotation;
			_baseForward = _basePivot.forward;
			_wobblePivot.localRotation = OWUtilities.GetWobbleRotation(0.3f, 0.15f) * Quaternion.identity;
		}

		private void OnRenderObject()
		{
			if (!QSBCore.HasWokenUp || !QSBCore.DebugMode || !QSBCore.ShowLinesInDebug)
			{
				return;
			}
			var light = _lights[1].GetLight();
			if (light.enabled)
			{
				Popcron.Gizmos.Cone(light.transform.position, light.transform.rotation, light.range, light.spotAngle, Color.yellow);
			}
		}
	}
}