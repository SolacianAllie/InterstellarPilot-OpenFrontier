using OpenFrontier.IP.Common;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Factions
{
	public class FactionRecentDamageController
	{
		public static float CalculateDamageFromLostUnit(Unit unit, UnitClass unitClass, DamageDirectType damageDirectType)
		{
			FactionRecentDamageSettings factionRecentDamageSettings = EngineASX.Instance.GameSettings.FactionSettings.FactionRecentDamageSettings;
			return Mathf.Lerp(factionRecentDamageSettings.MinDamageReceivedPerDestroyedUnit, factionRecentDamageSettings.MaxDamageReceivedPerDestroyedUnit, Mathf.Clamp01((float)unitClass.SaleCost / factionRecentDamageSettings.DestroyedUnitReferenceCreditsValue));
		}

		public static float CalculateDamageFromLostPerson(Person person, DamageDirectType damageDirectType)
		{
			return EngineASX.Instance.GameSettings.FactionSettings.FactionRecentDamageSettings.DamageReceivedPerKilledPerson;
		}

		public static void HandleDamageFromSource(Faction damagedFaction, float damage, Faction sourceFaction, Unit sourceUnit, DamageDirectType damageDirectionType)
		{
			FactionRecentDamage orCreateRecentDamageItem = damagedFaction.GetOrCreateRecentDamageItem(sourceFaction);
			FactionRecentDamageSettings factionRecentDamageSettings = EngineASX.Instance.GameSettings.FactionSettings.FactionRecentDamageSettings;
			damage *= factionRecentDamageSettings.RecentAttacksDamageScale;
			damage *= 1f + orCreateRecentDamageItem.RecentDamageReceived * factionRecentDamageSettings.RecentAttacksExistingDamageFactor;
			if (damageDirectionType == DamageDirectType.Indirect)
			{
				damage *= factionRecentDamageSettings.IndirectDamageMultiplier;
				if (damagedFaction.IsAIFactionType && sourceFaction.IsAIFactionType)
				{
					damage *= damagedFaction.FactionAI.GetDamageMultiplierForIndirectFireFromNpcFaction(sourceFaction);
				}
			}
			orCreateRecentDamageItem.AddToRecentDamageReceived(damage);
			orCreateRecentDamageItem.TimeOfLastDamageReceived = Time.time;
		}
	}
}
