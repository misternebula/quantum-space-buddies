﻿using OWML.Common;
using OWML.Utils;
using QSB.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace QSB.ShipSync
{
	class ShipManager : MonoBehaviour
	{
		public static ShipManager Instance;

		private uint _currentFlyer = uint.MaxValue;
		public uint CurrentFlyer
		{
			get => _currentFlyer;
			set
			{
				if (_currentFlyer != uint.MaxValue && value != uint.MaxValue)
				{
					DebugLog.ToConsole($"Warning - Trying to set current flyer while someone is still flying? Current:{_currentFlyer}, New:{value}", MessageType.Warning);
				}
				_currentFlyer = value;
			}
		}

		private void Awake()
		{
			QSBSceneManager.OnUniverseSceneLoaded += OnSceneLoaded;
			Instance = this;
		}

		private void OnSceneLoaded(OWScene scene)
		{
			if (scene == OWScene.EyeOfTheUniverse)
			{
				return;
			}
			var shipHatchControls = GameObject.Find("Hatch/HatchControls");
			var interactZone = shipHatchControls.GetComponent<InteractZone>();
			interactZone.SetValue("_viewingWindow", 360f);

			var sphereShape = shipHatchControls.GetComponent<SphereShape>();
			sphereShape.radius = 2.5f;
			sphereShape.center = new Vector3(0, 0, 1);
		}
	}
}