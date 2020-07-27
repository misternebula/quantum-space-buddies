﻿using OWML.ModHelper.Events;
using UnityEngine;
using UnityEngine.Networking;

namespace QSB.TimeSync
{
    public class PreserveTimeScale : NetworkBehaviour
    {
        private void Start()
        {
            QSB.Helper.Menus.PauseMenu.GetTitleButton("Button-EndCurrentLoop").Hide(); // Remove the meditation button

            // Allow server to sleep at campfires
            if (isServer)
            {
                return;
            }

            var campfires = GameObject.FindObjectsOfType<Campfire>();
            foreach (var campfire in campfires)
            {
                campfire.SetValue("_canSleepHere", false); // Stop players from sleeping at campfires
            }
        }
    }
}
