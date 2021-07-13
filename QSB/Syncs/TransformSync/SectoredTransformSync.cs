using QSB.SectorSync;
using QSB.SectorSync.WorldObjects;
using QSB.Utility;
using QSB.WorldSync;
using QuantumUNET.Transport;
using UnityEngine;

namespace QSB.Syncs.TransformSync
{
	public abstract class SectoredTransformSync : BaseTransformSync, ISectoredSync<Transform>
	{
		public QSBSector ReferenceSector { get; set; }
		public SectorSync.SectorSync SectorSync { get; private set; }
		public abstract TargetType Type { get; }

		public override bool IgnoreNullReferenceTransform => true;
		public override bool IgnoreDisabledAttachedObject => false;

		public override void Start()
		{
			SectorSync = gameObject.AddComponent<SectorSync.SectorSync>();
			QSBSectorManager.Instance.SectoredTransformSyncs.Add(this);
			base.Start();
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			QSBSectorManager.Instance.SectoredTransformSyncs.Remove(this);
			if (SectorSync != null)
			{
				Destroy(SectorSync);
			}
		}

		protected override void Init()
		{
			base.Init();
			if (!QSBSectorManager.Instance.IsReady)
			{
				return;
			}

			if (!HasAuthority)
			{
				return;
			}

			var closestSector = SectorSync.GetClosestSector(AttachedObject.transform);
			if (closestSector != null)
			{
				SetReferenceSector(closestSector);
			}
			else
			{
				DebugLog.DebugWrite($"INIT - {PlayerId}.{GetType().Name}'s closest sector is null!");
			}
		}

		public override void SerializeTransform(QNetworkWriter writer)
		{
			if (_intermediaryTransform == null)
			{
				_intermediaryTransform = new IntermediaryTransform(transform);
			}

			if (ReferenceSector != null)
			{
				writer.Write(ReferenceSector.ObjectId);
			}
			else
			{
				DebugLog.ToConsole($"Warning - ReferenceSector of {PlayerId}.{GetType().Name} is null.", OWML.Common.MessageType.Warning);
				writer.Write(-1);
			}

			base.SerializeTransform(writer);
		}

		public override void DeserializeTransform(QNetworkReader reader)
		{
			if (!QSBCore.WorldObjectsReady)
			{
				reader.ReadInt32();
				reader.ReadVector3();
				DeserializeRotation(reader);
				return;
			}

			var sectorId = reader.ReadInt32();
			var sector = sectorId == -1
				? null
				: QSBWorldSync.GetWorldFromId<QSBSector>(sectorId);

			if (sector != ReferenceSector)
			{
				if (sector == null)
				{
					DebugLog.ToConsole($"Error - {PlayerId}.{GetType().Name} got sector of ID -1.", OWML.Common.MessageType.Error);
					base.DeserializeTransform(reader);
					return;
				}

				SetReferenceSector(sector);
			}

			base.DeserializeTransform(reader);
		}

		protected override bool UpdateTransform()
		{
			var referenceNull = ReferenceTransform == null || ReferenceSector == null || _intermediaryTransform.GetReferenceTransform() == null;
			var sectorManagerReady = QSBSectorManager.Instance.IsReady;

			if (!sectorManagerReady)
			{
				if (referenceNull && HasAuthority)
				{
					DebugLog.ToConsole($"Warning - Reference was null, but sector manager wasn't ready. " +
						$"Transform:{ReferenceTransform == null}, Sector:{ReferenceSector == null}, Intermediary:{_intermediaryTransform.GetReferenceTransform() == null}",
						OWML.Common.MessageType.Warning);
				}
				return base.UpdateTransform();
			}

			if (!HasAuthority)
			{
				return base.UpdateTransform();
			}

			if (referenceNull)
			{
				var closestSector = SectorSync.GetClosestSector(AttachedObject.transform);
				if (closestSector != null)
				{
					SetReferenceTransform(closestSector.Transform);
				}
				else
				{
					DebugLog.ToConsole($"Error - No closest sector found to {PlayerId}.{GetType().Name}!", OWML.Common.MessageType.Error);
					return false;
				}
			}

			return base.UpdateTransform();
		}

		public void SetReferenceSector(QSBSector sector)
		{
			ReferenceSector = sector;
			SetReferenceTransform(sector?.Transform);
		}
	}
}
