﻿using QuantumUNET.Components;
using UnityEngine;

namespace QuantumUNET
{
	public class QPlayerController
	{
		public const int MaxPlayersPerClient = 32;

		public short PlayerControllerId = -1;
		public QNetworkIdentity UnetView;
		public GameObject Gameobject;
		public bool IsValid => PlayerControllerId != -1;

		public QPlayerController()
		{
		}

		internal QPlayerController(GameObject go, short playerControllerId)
		{
			Gameobject = go;
			UnetView = go.GetComponent<QNetworkIdentity>();
			PlayerControllerId = playerControllerId;
		}

		public override string ToString()
		{
			return string.Format("ID={0} NetworkIdentity NetID={1} Player={2}", new object[]
			{
				PlayerControllerId,
				UnetView == null ? "null" : UnetView.NetId.ToString(),
				Gameobject == null ? "null" : Gameobject.name
			});
		}
	}
}