﻿using OWML.Common;
using QSB.SectorSync.WorldObjects;
using QSB.Utility;
using QSB.WorldSync;
using System.Linq;
using UnityEngine;

namespace QSB.SectorSync
{
	public class QSBSectorManager : MonoBehaviour
	{
		public static QSBSectorManager Instance { get; private set; }

		public bool IsReady { get; private set; }

		private readonly Sector.Name[] _sectorBlacklist =
		{
			Sector.Name.Ship
		};

		public void Awake()
		{
			Instance = this;
			QSBSceneManager.OnUniverseSceneLoaded += (OWScene scene) => RebuildSectors();
			DebugLog.DebugWrite("Sector Manager ready.", MessageType.Success);
		}

		public void OnDestroy() => QSBSceneManager.OnUniverseSceneLoaded -= (OWScene scene) => RebuildSectors();

		public void RebuildSectors()
		{
			DebugLog.DebugWrite("Rebuilding sectors...", MessageType.Warning);
			WorldObjectManager.RemoveWorldObjects<QSBSector>();
			WorldObjectManager.Init<QSBSector, Sector>();
			IsReady = WorldObjectManager.GetWorldObjects<QSBSector>().Any();
		}

		public QSBSector GetClosestSector(Transform trans) // trans rights \o/
		{
			if (WorldObjectManager.GetWorldObjects<QSBSector>().Count() == 0)
			{
				DebugLog.ToConsole($"Error - Can't get closest sector, as there are no QSBSectors!", MessageType.Error);
				return null;
			}
			return WorldObjectManager.GetWorldObjects<QSBSector>()
				.Where(sector => sector.AttachedObject != null
					&& !_sectorBlacklist.Contains(sector.Type)
					&& sector.Transform.gameObject.activeInHierarchy)
				.OrderBy(sector => Vector3.Distance(sector.Position, trans.position))
				.First();
		}
	}
}