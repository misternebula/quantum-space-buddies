using QSB.SectorSync;
using QSB.SectorSync.WorldObjects;
using QSB.Utility;
using QSB.WorldSync;
using QuantumUNET.Transport;

namespace QSB.Syncs.RigidbodySync
{
	public abstract class SectoredRigidbodySync : UnparentedBaseRigidbodySync, ISectoredSync<OWRigidbody>
	{
		public QSBSector ReferenceSector { get; set; }
		public SectorSync.SectorSync SectorSync { get; private set; }
		public abstract TargetType Type { get; }

		public override bool IgnoreDisabledAttachedObject => false;
		public override bool IgnoreNullReferenceTransform => true;

		public override void Start()
		{
			SectorSync = gameObject.AddComponent<SectorSync.SectorSync>();
			QSBSectorManager.Instance.SectoredRigidbodySyncs.Add(this);
			base.Start();
		}

		protected override void OnDestroy()
		{
			DebugLog.DebugWrite($"OnDestroy {_logName}");
			base.OnDestroy();
			QSBSectorManager.Instance.SectoredRigidbodySyncs.Remove(this);
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
				SetReferenceTransform(closestSector.Transform);
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
				SetReferenceSector(sector);
			}

			base.DeserializeTransform(reader);
		}

		protected override bool UpdateTransform()
		{
			if ((ReferenceTransform == null || ReferenceSector == null) && QSBSectorManager.Instance.IsReady && HasAuthority)
			{
				var closestSector = SectorSync.GetClosestSector(AttachedObject.transform);
				if (closestSector != null)
				{
					SetReferenceTransform(closestSector.Transform);
				}
				else
				{
					return false;
				}
			}

			return base.UpdateTransform();
		}

		public void SetReferenceSector(QSBSector sector)
		{
			DebugLog.DebugWrite($"{GetType().Name} set reference sector to {transform.name}");
			ReferenceSector = sector;
			SetReferenceTransform(sector?.Transform);
		}
	}
}
