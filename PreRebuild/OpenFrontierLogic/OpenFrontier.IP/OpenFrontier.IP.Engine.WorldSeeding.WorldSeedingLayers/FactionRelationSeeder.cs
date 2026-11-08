using System.Collections.Generic;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers
{
	[RequireComponent(typeof(WorldSeederLayer))]
	public class FactionRelationSeeder : MonoBehaviour
	{
		public void SeedLayer(WorldBase world)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Seeding faction relations...", this, 1);
			}
			HashSet<ulong> hashSet = new HashSet<ulong>(EngineASX.Instance.Factions.Count * 4);
			foreach (Faction faction in EngineASX.Instance.Factions)
			{
				if (!faction.IsAIFactionType)
				{
					continue;
				}
				foreach (FactionAttitude relation in faction.Relations)
				{
					if (relation.TargetFaction.IsAIFactionType)
					{
						ulong item = Helper.PairId(faction.UniqueId, relation.TargetFaction.UniqueId);
						if (!hashSet.Contains(item))
						{
							hashSet.Add(item);
							if (relation.TargetFaction.FactionType != FactionType.Player)
							{
								float? num = SeedOpinionForFaction(faction, relation.TargetFaction);
								float? num2 = SeedOpinionForFaction(relation.TargetFaction, faction);
								if (num.HasValue || num2.HasValue)
								{
									float newOpinion = Mathf.Min(num ?? 100f, num2 ?? 100f);
									FactionAttitude orCreateAttitude = relation.TargetFaction.GetOrCreateAttitude(faction);
									relation.SetOpinionAndRecordTimeOfChange(newOpinion, faction);
									orCreateAttitude.SetOpinionAndRecordTimeOfChange(newOpinion, relation.TargetFaction);
								}
							}
						}
					}
					if (faction.FactionAI.IsAlwaysAtWarWithFaction(relation.TargetFaction))
					{
						faction.SetAsHostileToTwoWay(relation.TargetFaction, permanentWar: true);
					}
				}
			}
		}

		public static void ValidateUnusualFactionRelations()
		{
			foreach (Faction faction in EngineASX.Instance.Factions)
			{
				if (faction.IsAIFactionType)
				{
					continue;
				}
				foreach (FactionAttitude relation in faction.Relations)
				{
					if (relation.TargetFaction.IsAIFactionType && relation.Opinion > 0.6f && ((faction.FactionType == FactionType.Outlaw && relation.TargetFaction.FactionType == FactionType.Trader) || (faction.FactionType == FactionType.Trader && relation.TargetFaction.FactionType == FactionType.Outlaw)))
					{
						Debug.LogWarning($"Seeding high opinion from trader to outlaw. Faction {faction} TargetFaction {relation.TargetFaction}");
					}
				}
			}
		}

		public float? SeedOpinionForFaction(Faction faction, Faction otherFaction)
		{
			int jumpDistanceTo = faction.HomeSector.GetJumpDistanceTo(otherFaction.HomeSector);
			float chanceOfSeedingOpinion = GetChanceOfSeedingOpinion(faction, otherFaction, jumpDistanceTo);
			if (Random.value > chanceOfSeedingOpinion)
			{
				return null;
			}
			float num = FactionOpinionSetter.GetOpinionBasedOnDifferences(faction, otherFaction);
			if (jumpDistanceTo > 2 && (faction.FactionType != FactionType.Empire || otherFaction.FactionType != FactionType.Empire))
			{
				num *= Mathf.Lerp(1f, 0f, Mathf.Clamp01((float)(jumpDistanceTo - 2) / 5f));
			}
			if (faction.IsCivilianFromFactionType && !otherFaction.IsBanditOrOutlaw() && faction.HomeSector != null && faction.HomeSector.ControllingFaction == otherFaction && faction.HomeSector.ControllingFaction.Virtue > 0.5f)
			{
				num += 0.05f + Random.value * 0.3f;
			}
			if (Random.value < 0.1f)
			{
				num *= 2f;
			}
			if (faction.IsHostileToOrAlwaysHostileTo(otherFaction) || otherFaction.IsHostileToOrAlwaysHostileTo(faction))
			{
				num--;
			}
			return num;
		}

		public static float GetChanceOfSeedingOpinion(Faction faction, Faction targetFaction, int distFromHomeSector)
		{
			float num = 0.75f;
			switch (targetFaction.FactionType)
			{
			case FactionType.Empire:
				num = 1f;
				break;
			case FactionType.BountyHunter:
				num = 1f;
				break;
			case FactionType.Mercenary:
				num = 0.5f;
				break;
			case FactionType.Bandit:
			case FactionType.Outlaw:
				if (faction.IsCivilianFromFactionType)
				{
					num = 1.25f;
				}
				break;
			}
			if (targetFaction.IsFreelancer)
			{
				num *= 0.25f;
			}
			else if (targetFaction.IsMinor)
			{
				num *= 0.5f;
			}
			if (faction.FactionType != FactionType.Empire && targetFaction.FactionType != FactionType.Empire && distFromHomeSector > 2)
			{
				num *= Mathf.Lerp(1f, 0f, Mathf.Clamp01((float)(distFromHomeSector - 2) / 6f));
			}
			return num;
		}
	}
}
