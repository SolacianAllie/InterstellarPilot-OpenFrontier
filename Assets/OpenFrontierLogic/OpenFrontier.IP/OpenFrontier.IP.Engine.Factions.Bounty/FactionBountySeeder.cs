using OpenFrontier.IP.Common.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Factions.Bounty
{
	public static class FactionBountySeeder
	{
		private const int maxBountyItemsOnPilot = 2;

		public static void Seed(Faction faction, float probabilityMultiplier = 1f)
		{
			long num = faction.CalculateNetWorth();
			float num2 = (float)num * 0.25f;
			FactionBountyBoard nearestBountyBoardToHomeSector = faction.GetNearestBountyBoardToHomeSector();
			if (nearestBountyBoardToHomeSector == null)
			{
				return;
			}
			foreach (FactionAttitude relation in faction.Relations)
			{
				if (!(relation.TargetFaction != null) || !relation.TargetFaction.IsAIFactionType || !(relation.Opinion < -0.2f))
				{
					continue;
				}
				float num3 = Mathf.Abs(relation.Opinion) * Mathf.Clamp01(probabilityMultiplier * relation.TargetFaction.FactionTypeInfo.ProbabilityOfBountySeededOnPilots * faction.Engine.GameSettings.BountyProbabilityForEachPilot);
				if (faction.FactionType != FactionType.Empire && relation.TargetFaction.FactionType == FactionType.Empire)
				{
					num3 *= 0.5f;
				}
				foreach (Person person in relation.TargetFaction.People)
				{
					if (!person.IsPilot || faction.Engine.GetBountyCountOnPilot(person) >= 2 || !(Random.value < num3))
					{
						continue;
					}
					int saleCost = person.CurrentUnit.UnitClass.SaleCost;
					float randomPower = Mathf.Clamp(1f / num3, 0f, 8f);
					int bountyValue = GetBountyValue(saleCost, randomPower);
					bool updateLastKnownPosition = true;
					if (person.Sector != null && !faction.Intel.IsSectorDiscovered(person.Sector))
					{
						updateLastKnownPosition = false;
					}
					else if (Random.value < 0.3f)
					{
						updateLastKnownPosition = false;
					}
					if ((float)bountyValue < num2)
					{
						double timeOfSighting = 0.0 - (double)Random.Range(20f, 300f);
						BountyHelper.AddBountyWithRounding(faction, nearestBountyBoardToHomeSector, person, bountyValue, timeOfSighting, updateLastKnownPosition);
						num -= bountyValue;
						if (relation.TargetFaction.FactionType == FactionType.Outlaw)
						{
							EngineASX.Instance.DebugInfo.Seeding_NumOutlawPilotsSeededWithBounty++;
						}
					}
				}
			}
		}

		public static int GetBountyValueToPlaceOnUnit(Unit unit)
		{
			int saleCost = unit.UnitClass.SaleCost;
			float randomPower = Random.Range(6f, 8f);
			return GetBountyValue(saleCost, randomPower);
		}

		public static int GetBountyValue(int bountyRefVBalue, float randomPower)
		{
			return Mathf.RoundToInt(Mathf.Lerp(GameController.Instance.GameSettings.BountyGroupMinRewardMultiplier, GameController.Instance.GameSettings.BountyGroupMaxRewardMultiplier, Mathf.Pow(Random.value, randomPower)) * (float)bountyRefVBalue);
		}
	}
}
