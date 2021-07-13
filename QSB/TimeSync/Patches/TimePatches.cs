using QSB.Patches;

namespace QSB.TimeSync.Patches
{
	internal class TimePatches : QSBPatch
	{
		public override QSBPatchTypes Type => QSBPatchTypes.OnClientConnect;

		public override void DoPatches()
			=> Prefix(nameof(PlayerCameraEffectController_OnStartOfTimeLoop));

		public static bool PlayerCameraEffectController_OnStartOfTimeLoop()
			=> false;
	}
}
