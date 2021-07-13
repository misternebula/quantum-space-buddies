using OWML.Utils;
using QSB.Utility;
using QSB.WorldSync;

namespace QSB.ShipSync.WorldObjects
{
	internal class QSBShipComponent : WorldObject<ShipComponent>
	{
		public override void Init(ShipComponent component, int id)
		{
			ObjectId = id;
			AttachedObject = component;
		}

		public void SetDamaged()
		{
			DebugLog.DebugWrite($"[S COMPONENT] {AttachedObject} Set damaged.");
			AttachedObject.SetValue("_damaged", true);
			AttachedObject.SetValue("_repairFraction", 0f);
			AttachedObject.GetType().GetAnyMethod("OnComponentDamaged").Invoke(AttachedObject, null);
			AttachedObject.RaiseEvent("OnDamaged", AttachedObject);
			AttachedObject.GetType().GetAnyMethod("UpdateColliderState").Invoke(AttachedObject, null);
			var damageEffect = AttachedObject.GetValue<DamageEffect>("_damageEffect");
			damageEffect.SetEffectBlend(1f);
		}

		public void SetRepaired()
		{
			DebugLog.DebugWrite($"[S COMPONENT] {AttachedObject} Set repaired.");
			AttachedObject.SetValue("_damaged", false);
			AttachedObject.SetValue("_repairFraction", 1f);
			AttachedObject.GetType().GetAnyMethod("OnComponentRepaired").Invoke(AttachedObject, null);
			AttachedObject.RaiseEvent("OnRepaired", AttachedObject);
			AttachedObject.GetType().GetAnyMethod("UpdateColliderState").Invoke(AttachedObject, null);
			var damageEffect = AttachedObject.GetValue<DamageEffect>("_damageEffect");
			damageEffect.SetEffectBlend(0f);
		}

		public void RepairTick(float repairFraction)
		{
			AttachedObject.SetValue("_repairFraction", repairFraction);
			var damageEffect = AttachedObject.GetValue<DamageEffect>("_damageEffect");
			damageEffect.SetEffectBlend(1f - repairFraction);
		}
	}
}
