﻿using OWML.Common;
using OWML.ModHelper;
using QSB.ElevatorSync;
using QSB.GeyserSync;
using QSB.Tools;
using QSB.TransformSync;
using QSB.Utility;
using UnityEngine;
using UnityEngine.Networking;

namespace QSB
{
    public class QSB : ModBehaviour
    {
        public static IModHelper Helper { get; private set; }
        public static string DefaultServerIP { get; private set; }
        public static int Port { get; private set; }
        public static bool DebugMode { get; private set; }
        public static AssetBundle NetworkAssetBundle { get; private set; }

        private void Awake()
        {
            Application.runInBackground = true;
        }

        private void Start()
        {
            Helper = ModHelper;
            DebugLog.ToConsole($"* Start of QSB version {Helper.Manifest.Version} - authored by {Helper.Manifest.Author}", MessageType.Info);

            NetworkAssetBundle = Helper.Assets.LoadBundle("assets/network");
            DebugLog.LogState("NetworkBundle", NetworkAssetBundle);
            ProbePatches.DoPatches();

            // Turns out these are very finicky about what order they go. QSBNetworkManager seems to 
            // want to go first-ish, otherwise the NetworkManager complains about the PlayerPrefab being 
            // null (even though it isn't...)
            gameObject.AddComponent<QSBNetworkManager>();
            gameObject.AddComponent<NetworkManagerHUD>();
            gameObject.AddComponent<DebugActions>();
            gameObject.AddComponent<ElevatorManager>();
            gameObject.AddComponent<GeyserManager>();
            gameObject.AddComponent<QSBSectorManager>();
        }

        public override void Configure(IModConfig config)
        {
            DefaultServerIP = config.GetSettingsValue<string>("defaultServerIP");
            Port = config.GetSettingsValue<int>("port");
            QSBNetworkManager.Instance.networkPort = Port;
            DebugMode = config.GetSettingsValue<bool>("debugMode");
        }
    }
}
