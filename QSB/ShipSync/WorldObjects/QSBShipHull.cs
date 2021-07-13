using OWML.Utils;
using QSB.Utility;
using QSB.WorldSync;

namespace QSB.ShipSync.WorldObjects
{
	internal class QSBShipHull : WorldObject<ShipHull>
	{
		public override void Init(ShipHull hull, int id)
		{
			ObjectId = id;
			AttachedObject = hull;
		}

		public void SetDamaged()
		{
			DebugLog.DebugWrite($"[HULL] {AttachedObject} Set damaged.");
			AttachedObject.SetValue("_damaged", true);
			AttachedObject.RaiseEvent("OnDamaged", AttachedObject);
		}

		public void SetRepaired()
		{
			DebugLog.DebugWrite($"[HULL] {AttachedObject} Set repaired.");
			AttachedObject.SetValue("_damaged", false);
			AttachedObject.RaiseEvent("OnRepaired", AttachedObject);
			var damageEffect = AttachedObject.GetValue<DamageEffect>("_damageEffect");
			damageEffect.SetEffectBlend(0f);
		}

		public void ChangeIntegrity(float newIntegrity)
		{
			DebugLog.DebugWrite($"[HULL] {AttachedObject} Change integrity to {newIntegrity}.");
			AttachedObject.SetValue("_integrity", newIntegrity);
			var damageEffect = AttachedObject.GetValue<DamageEffect>("_damageEffect");
			damageEffect.SetEffectBlend(1f - newIntegrity);
		}

		public void RepairTick(float integrity)
		{
			AttachedObject.SetValue("_integrity", integrity);
			var damageEffect = AttachedObject.GetValue<DamageEffect>("_damageEffect");
			damageEffect.SetEffectBlend(1f - integrity);
		}
	}
}
