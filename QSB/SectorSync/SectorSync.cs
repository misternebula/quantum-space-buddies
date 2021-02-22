﻿using QSB.Events;
using QSB.Player;
using QSB.SectorSync.WorldObjects;
using QSB.Utility;
using System.Linq;
using UnityEngine;

namespace QSB.SectorSync
{
	public class SectorSync : MonoBehaviour, IRepeating
	{
		private void OnEnable() => RepeatingManager.Repeatings.Add(this);
		private void OnDisable() => RepeatingManager.Repeatings.Remove(this);

		public void Invoke()
		{
			if (!QSBSectorManager.Instance.IsReady)
			{
				return;
			}
			PlayerManager.GetSyncObjects<TransformSync.TransformSync>()
				.Where(x => x.HasAuthority).ToList().ForEach(CheckTransformSyncSector);
		}

		private void CheckTransformSyncSector(TransformSync.TransformSync transformSync)
		{
			var syncedTransform = transformSync.SyncedTransform;
			if (syncedTransform == null || syncedTransform.position == Vector3.zero)
			{
				return;
			}
			var closestSector = QSBSectorManager.Instance.GetClosestSector(syncedTransform);
			if (closestSector == transformSync.ReferenceSector)
			{
				return;
			}
			transformSync.SetReferenceSector(closestSector);
			SendSector(transformSync.NetId.Value, closestSector);
		}

		private void SendSector(uint id, QSBSector sector) =>
			EventManager.FireEvent(EventNames.QSBSectorChange, id, sector);
	}
}