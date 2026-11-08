using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public static class VirtueHandler
	{
		public static void HandleSectorCapturedFromFaction(Faction capturedFaction, Faction oldSectorOwner)
		{
		}

		public static void HandleIllegalGoodsTraded(Faction tradingFaction, float cargoValue)
		{
			float num = cargoValue / 1000f;
			ChangeVirtue(tradingFaction, num * (0f - GameController.Instance.GameSettings.VirtueChangeSettings.SignificanceOfIllegalCargoTradedPer1kValue));
		}

		public static void HandleIllegalStationConstruction(Faction constructorFaction)
		{
			ChangeVirtue(constructorFaction, 0f - GameController.Instance.GameSettings.VirtueChangeSettings.SignificanceOfIllegalStationConstructed);
		}

		public static void HandleCargoStolen(Faction thievingFaction, Faction factionLostOut, int cargoValue)
		{
		}

		public static void HandleUnitCaptured(Faction capturingFaction, Faction capturedUnitFaction, UnitClass unitClass)
		{
			HandleUnitKilled(capturingFaction, capturedUnitFaction, unitClass, 0.4f);
		}

		public static float GetSignificanceOfKilledUnit(UnitClass unitClass, Faction killedFaction, bool killedFactionIsCivilian)
		{
			float num = Mathf.Lerp(0.25f, 4f, (float)unitClass.SaleCost / 4000000f);
			if (killedFactionIsCivilian)
			{
				num++;
				if (unitClass.UnitType == UnitType.Ship && unitClass.PurposePassengerTransport)
				{
					num += 4f;
				}
			}
			if ((killedFactionIsCivilian || killedFaction.Virtue > 0.5f) && unitClass.UnitType == UnitType.Station && unitClass.StationPurpose == StationPurpose.TradeStation)
			{
				num *= 2f;
			}
			return num;
		}

		public static void HandleUnitKilled(Faction killerFaction, Faction killedFaction, UnitClass unitClass, float multiplier = 1f)
		{
			if (!(killerFaction == null) && !(killedFaction == null) && !(unitClass == null))
			{
				bool isCivilianFromFactionType = killedFaction.IsCivilianFromFactionType;
				float significanceOfKilledUnit = GetSignificanceOfKilledUnit(unitClass, killedFaction, isCivilianFromFactionType);
				float num = 0f;
				if (killedFaction.Virtue > 0.39999998f)
				{
					num = (0f - (killedFaction.Virtue - 0.6f)) / 0.39999998f;
				}
				else if (killedFaction.Virtue < 0.6f && !isCivilianFromFactionType)
				{
					num = (0.6f - killedFaction.Virtue) / 0.6f;
				}
				if (isCivilianFromFactionType)
				{
					num -= 0.2f;
				}
				if (num != 0f)
				{
					ChangeVirtue(killerFaction, significanceOfKilledUnit * GameController.Instance.GameSettings.VirtueChangeSettings.KilledUnitSignificanceMultiplier * multiplier * num);
				}
			}
		}

		private static void ChangeVirtue(Faction faction, float significance)
		{
			significance *= Mathf.Lerp(1f, 0.2f, Mathf.Clamp01((float)faction.People.Count / 50f));
			float num = Mathf.Abs(faction.Virtue - 0.5f);
			significance *= Mathf.Lerp(1f, 0.1f, num / 0.5f);
			float value = significance * GameController.Instance.GameSettings.GameplaySettings.VirtueChangeRateMultiplier;
			value = Mathf.Clamp(value, -0.1f, 0.1f);
			faction.ChangeVirtue(faction.Virtue + value);
		}
	}
}
