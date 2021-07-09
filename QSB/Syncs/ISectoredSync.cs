using QSB.SectorSync;
using QSB.SectorSync.WorldObjects;

namespace QSB.Syncs
{
	public interface ISectoredSync<T>
	{
		SectorSync.SectorSync SectorSync { get; }
		QSBSector ReferenceSector { get; }
		TargetType Type { get; }

		void SetReferenceSector(QSBSector sector);
	}
}
