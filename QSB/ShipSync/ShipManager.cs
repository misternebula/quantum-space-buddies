﻿using OWML.Common;
using OWML.Utils;
using QSB.Player;
using QSB.ShipSync.TransformSync;
using QSB.Utility;
using QSB.WorldSync;
using QuantumUNET;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QSB.ShipSync
{
	internal class ShipManager : WorldObjectManager
	{
		public static ShipManager Instance;

		public InteractZone HatchInteractZone;
		public HatchController HatchController;
		public ShipTractorBeamSwitch ShipTractorBeam;
		public ShipCockpitController CockpitController;
		public ShipElectricalComponent ShipElectricalComponent;
		public bool HasAuthority
			=> ShipTransformSync.LocalInstance.HasAuthority;
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

		private List<PlayerInfo> _playersInShip = new List<PlayerInfo>();

		private uint _currentFlyer = uint.MaxValue;

		public void Start()
			=> Instance = this;

		protected override void RebuildWorldObjects(OWScene scene)
		{
			var shipTransform = GameObject.Find("Ship_Body");
			HatchController = shipTransform.GetComponentInChildren<HatchController>();
			HatchInteractZone = HatchController.GetComponent<InteractZone>();
			ShipTractorBeam = Resources.FindObjectsOfTypeAll<ShipTractorBeamSwitch>().First();
			CockpitController = Resources.FindObjectsOfTypeAll<ShipCockpitController>().First();
			ShipElectricalComponent = Resources.FindObjectsOfTypeAll<ShipElectricalComponent>().First();

			var sphereShape = HatchController.GetComponent<SphereShape>();
			sphereShape.radius = 2.5f;
			sphereShape.center = new Vector3(0, 0, 1);

			if (QSBCore.IsServer)
			{
				if (ShipTransformSync.LocalInstance != null)
				{
					QNetworkServer.Destroy(ShipTransformSync.LocalInstance.gameObject);
				}

				QNetworkServer.Spawn(Instantiate(QSBNetworkManager.Instance.ShipPrefab));
			}

			var shipComponents = Resources.FindObjectsOfTypeAll<ShipComponent>();
			var electricalComponents = Resources.FindObjectsOfTypeAll<ElectricalComponent>();
			var electricalSystems = Resources.FindObjectsOfTypeAll<ElectricalSystem>();
			var shipModules = Resources.FindObjectsOfTypeAll<ShipModule>();
			var shipHulls = Resources.FindObjectsOfTypeAll<ShipHull>();

			DebugLog.DebugWrite("ShipComponents : ");
			PrintAll(shipComponents);

			DebugLog.DebugWrite("Electrical Components : ");
			PrintAll(electricalComponents);

			DebugLog.DebugWrite("Electrical Systems : ");
			PrintAll(electricalSystems);

			DebugLog.DebugWrite("Ship Modules : ");
			PrintAll(shipModules);

			DebugLog.DebugWrite("Ship Hulls : ");
			PrintAll(shipHulls);
		}

		public void AddPlayerToShip(PlayerInfo player)
		{
			DebugLog.DebugWrite($"{player.PlayerId} enter ship.");
			_playersInShip.Add(player);
			UpdateElectricalComponent();
		}

		public void RemovePlayerFromShip(PlayerInfo player)
		{
			DebugLog.DebugWrite($"{player.PlayerId} leave ship.");
			_playersInShip.Remove(player);
			UpdateElectricalComponent();
		}

		private void UpdateElectricalComponent()
		{
			var electricalSystem = ShipElectricalComponent.GetValue<ElectricalSystem>("_electricalSystem");
			var damaged = ShipElectricalComponent.GetValue<bool>("_damaged");

			if (_playersInShip.Count == 0)
			{
				if (!damaged)
				{
					DebugLog.DebugWrite($"No players left in ship - turning off electricals.");
					electricalSystem.SetPowered(false);
				}
			}
			else
			{
				if (!damaged)
				{
					DebugLog.DebugWrite($"Player in ship - turning on electricals.");
					electricalSystem.SetPowered(true);
				}
			}
		}

		private void PrintAll(Array array)
		{
			foreach (var item in array)
			{
				DebugLog.DebugWrite($" - {(item as MonoBehaviour).name}");
			}
		}
	}
}
