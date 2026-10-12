using OpenFrontier.IP.Common.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Factions
{
	public static class FactionOpinionSetter
	{
		public static float GetOpinionBasedOnDifferences(Faction f1, Faction f2)
		{
			float num = 1f + f1.Aggression * 0.05f;
			float baseOpinionTo = GetBaseOpinionTo(f1, f2);
			float num2 = 0f;
			num2 += Mathf.Abs(f2.Virtue - f1.Virtue) * num;
			float num3 = baseOpinionTo + Mathf.Lerp(0.6f, -0.6f, num2 / 1f);
			return num3 * GetOpinionMultiplier(f1, f2, num3);
		}

		private static float GetBaseOpinionTo(Faction f1, Faction f2)
		{
			float num = Mathf.Lerp(-0.1f, 0.1f, Random.value);
			num -= f1.Aggression * 0.2f;
			num += (f1.Cooperation * 2f - 1f) * 0.2f;
			switch (f2.FactionType)
			{
			case FactionType.BountyHunter:
				num -= Mathf.Lerp(-0.2f, 0f, Random.value);
				break;
			case FactionType.Outlaw:
				num -= Random.value * 0.25f;
				if (f1.IsCivilianFromFactionType)
				{
					num -= Maths.RandomFloatWithPower(0.25f, 1f, 0.5f);
				}
				break;
			}
			return num;
		}

		private static float GetOpinionMultiplier(Faction f1, Faction f2, float opinion)
		{
			if (f1.FactionType == FactionType.BountyHunter || f2.FactionType == FactionType.BountyHunter)
			{
				return 0.1f;
			}
			if (f2.FactionType == FactionType.Mercenary || f2.FactionType == FactionType.Mercenary)
			{
				return 0.1f;
			}
			if (f2.FactionType == FactionType.Bar)
			{
				return 0.1f;
			}
			if (f1.FactionType == FactionType.Empire && f2.FactionType == FactionType.Empire)
			{
				return 1f;
			}
			if (f1.FactionType == FactionType.Empire && f2.HomeSector != null && f2.HomeSector.ControllingFaction == f1 && f2.IsCivilianFromFactionType && opinion < 0f)
			{
				return 0.15f;
			}
			return 0.5f;
		}
	}
}
