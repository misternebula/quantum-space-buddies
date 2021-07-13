using OWML.Common;
using OWML.ModHelper;
using OWML.ModHelper.Input;
using OWML.Utils;
using QSB.Animation.NPC;
using QSB.CampfireSync;
using QSB.ConversationSync;
using QSB.DeathSync;
using QSB.ElevatorSync;
using QSB.GeyserSync;
using QSB.Inputs;
using QSB.ItemSync;
using QSB.OrbSync;
using QSB.Patches;
using QSB.Player;
using QSB.Player.TransformSync;
using QSB.PoolSync;
using QSB.QuantumSync;
using QSB.SectorSync;
using QSB.ShipSync;
using QSB.StatueSync;
using QSB.TimeSync;
using QSB.TranslationSync;
using QSB.Utility;
using QSB.WorldSync;
using QuantumUNET;
using QuantumUNET.Components;
using UnityEngine;

/*
	Copyright (C) 2020 - 2021
			Henry Pointer (_nebula / misternebula), 
			Aleksander Waage (AmazingAlek), 
			Ricardo Lopes (Raicuparta)
	
	This program is free software: you can redistribute it and/or
	modify it under the terms of the GNU Affero General Public License
	as published by the Free Software Foundation, either version 3 of
	the License, or (at your option) any later version.

	This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
	without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
	See the GNU Affero General Public License for more details.

	You should have received a copy of the GNU Affero General Public License along with this program. If not, see https://www.gnu.org/licenses/.
*/

namespace QSB
{
	public class QSBCore : ModBehaviour
	{
		public static IModHelper Helper { get; private set; }
		public static IModUnityEvents UnityEvents => Helper.Events.Unity;
		public static string DefaultServerIP { get; private set; }
		public static int Port { get; private set; }
		public static bool DebugMode { get; private set; }
		public static bool ShowLinesInDebug { get; private set; }
		public static AssetBundle NetworkAssetBundle { get; private set; }
		public static AssetBundle InstrumentAssetBundle { get; private set; }
		public static AssetBundle ConversationAssetBundle { get; private set; }
		public static bool WorldObjectsReady => WorldObjectManager.AllReady && IsInMultiplayer && PlayerTransformSync.LocalInstance != null;
		public static bool IsServer => QNetworkServer.active;
		public static bool IsInMultiplayer => QNetworkManager.singleton.isNetworkActive;
		public static string QSBVersion => Helper.Manifest.Version;

		public void Awake()
		{
			var instance = TextTranslation.Get().GetValue<TextTranslation.TranslationTable>("m_table");
			instance.theUITable[(int)UITextType.PleaseUseController] =
				"<color=orange>Quantum Space Buddies</color> is best experienced with friends...";
		}

		public void Start()
		{
			Helper = ModHelper;
			DebugLog.ToConsole($"* Start of QSB version {QSBVersion} - authored by {Helper.Manifest.Author}", MessageType.Info);

			NetworkAssetBundle = Helper.Assets.LoadBundle("assets/network");
			InstrumentAssetBundle = Helper.Assets.LoadBundle("assets/instruments");
			ConversationAssetBundle = Helper.Assets.LoadBundle("assets/conversation");

			QSBPatchManager.Init();

			gameObject.AddComponent<QSBNetworkManager>();
			gameObject.AddComponent<QNetworkManagerHUD>();
			gameObject.AddComponent<DebugActions>();
			gameObject.AddComponent<ConversationManager>();
			gameObject.AddComponent<QSBInputManager>();
			gameObject.AddComponent<TimeSyncUI>();
			gameObject.AddComponent<RepeatingManager>();
			gameObject.AddComponent<PlayerEntanglementWatcher>();
			gameObject.AddComponent<DebugGUI>();
			gameObject.AddComponent<RespawnManager>();

			// WorldObject managers
			gameObject.AddComponent<QuantumManager>();
			gameObject.AddComponent<SpiralManager>();
			gameObject.AddComponent<ElevatorManager>();
			gameObject.AddComponent<GeyserManager>();
			gameObject.AddComponent<OrbManager>();
			gameObject.AddComponent<QSBSectorManager>();
			gameObject.AddComponent<ItemManager>();
			gameObject.AddComponent<StatueManager>();
			gameObject.AddComponent<PoolManager>();
			gameObject.AddComponent<CampfireManager>();
			gameObject.AddComponent<CharacterAnimManager>();
			gameObject.AddComponent<ShipManager>();

			DebugBoxManager.Init();

			Helper.HarmonyHelper.EmptyMethod<ModCommandListener>("Update");

			// Stop players being able to pause
			Helper.HarmonyHelper.EmptyMethod(typeof(OWTime).GetMethod("Pause"));

			QSBPatchManager.OnPatchType += OnPatchType;
			QSBPatchManager.OnUnpatchType += OnUnpatchType;
		}

		private void OnPatchType(QSBPatchTypes type)
		{
			if (type == QSBPatchTypes.OnClientConnect)
			{
				Application.runInBackground = true;
			}
		}

		private void OnUnpatchType(QSBPatchTypes type)
		{
			if (type == QSBPatchTypes.OnClientConnect)
			{
				Application.runInBackground = false;
			}
		}

		public void Update() =>
			QNetworkIdentity.UNetStaticUpdate();

		public override void Configure(IModConfig config)
		{
			DefaultServerIP = config.GetSettingsValue<string>("defaultServerIP");
			Port = config.GetSettingsValue<int>("port");
			if (QSBNetworkManager.Instance != null)
			{
				QSBNetworkManager.Instance.networkPort = Port;
			}

			DebugMode = config.GetSettingsValue<bool>("debugMode");
			ShowLinesInDebug = config.GetSettingsValue<bool>("showLinesInDebug");
		}
	}
}

/*
 * _nebula's music thanks
 * I listen to music constantly while programming/working - here's my thanks to them for keeping me entertained :P
 * 
 * Wintergatan
 * HOME
 * C418
 * Lupus Nocte
 * Max Cooper
 * Darren Korb
 * Harry Callaghan
 * Toby Fox
 * Andrew Prahlow
 * Valve (Mike Morasky, Kelly Bailey)
 * Joel Nielsen
 * Vulfpeck
 * Detektivbyrån
 * Ben Prunty
 * ConcernedApe
 * Jake Chudnow
 * Murray Gold
 * Teleskärm
 * Daft Punk
 */