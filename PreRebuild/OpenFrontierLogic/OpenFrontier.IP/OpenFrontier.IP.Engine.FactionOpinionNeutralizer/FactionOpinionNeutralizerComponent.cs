using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine.FactionOpinionNeutralizer
{
	public class FactionOpinionNeutralizerComponent : MonoBehaviour
	{
		private int currentFactionIndex;

		private Faction currentFaction;

		private int currentFactionAttitudeIndex;

		public FactionOpinionNeutralizerSettings Settings;

		private void Update()
		{
			if (!EngineASX.LoadedAndReady || !(Settings != null) || EngineASX.Instance.Factions.Count <= 1)
			{
				return;
			}
			int num = Mathf.CeilToInt((float)Settings.ProcessedAttitudesPerSecond * Time.deltaTime * Time.timeScale);
			Faction faction = currentFaction;
			for (int i = 0; i < num; i++)
			{
				if (currentFaction != null && currentFaction.FactionType != FactionType.Player)
				{
					if (currentFaction.DynamicFactionAttitudes)
					{
						currentFactionAttitudeIndex++;
						if (currentFactionAttitudeIndex < currentFaction.Relations.Count)
						{
							FactionAttitude factionAttitude = currentFaction.Relations[currentFactionAttitudeIndex];
							if (!(factionAttitude.TargetFaction != null))
							{
								continue;
							}
							float num2 = 0f;
							float changeMultiplier = 1f;
							switch (factionAttitude.Neutrality)
							{
							case Neutrality.Hostile:
								num2 = -1f;
								if (factionAttitude.Opinion > 0f)
								{
									changeMultiplier *= 2f;
								}
								break;
							case Neutrality.Neutral:
								num2 = GetBaselineOpinion(currentFaction, factionAttitude.TargetFaction, out changeMultiplier);
								break;
							}
							if (factionAttitude.Opinion != num2)
							{
								float num3 = Time.time - factionAttitude.TimeOfLastOpinionChange;
								if (num3 > Settings.TimeBeforeOpinionNeutralization)
								{
									float num4 = num3 / 60f / 60f * Settings.OpinionChangePerHour;
									float newOpinion = Mathf.MoveTowards(factionAttitude.Opinion, num2, num4 * changeMultiplier);
									factionAttitude.SetOpinionAndRecordTimeOfChange(newOpinion, currentFaction);
								}
							}
						}
						else
						{
							currentFaction = null;
						}
					}
					else
					{
						currentFaction = null;
					}
				}
				else
				{
					NextFaction();
					if (currentFaction == faction)
					{
						break;
					}
				}
			}
		}

		public static float GetBaselineOpinion(Faction currentFaction, Faction targetFaction, out float changeMultiplier)
		{
			changeMultiplier = 1f;
			if (currentFaction.IsHostileToOrAlwaysHostileTo(targetFaction))
			{
				return -1f;
			}
			float num = 0f;
			if (currentFaction.Virtue > 0.65f || targetFaction.Virtue > 0.65f || currentFaction.Virtue < 0.35000002f || targetFaction.Virtue < 0.35000002f)
			{
				float num2 = Mathf.Abs(currentFaction.Virtue - targetFaction.Virtue);
				if (num2 > 0.5f)
				{
					num = (num2 - 0.5f) / 0.5f * -1f;
				}
				else if (num2 < 0.3f && (!currentFaction.IsCivilianFromFactionType || targetFaction.Virtue >= 0.5f))
				{
					num = (0.3f - num2) / 0.3f * 0.5f;
				}
			}
			if (num > -1f)
			{
				int controlledSectorCount = targetFaction.ControlledSectorCount;
				if (controlledSectorCount > 0)
				{
					if (currentFaction.HomeSector != null && currentFaction.HomeSector.ControllingFaction == targetFaction)
					{
						switch (currentFaction.FactionType)
						{
						case FactionType.Bandit:
						case FactionType.Outlaw:
							num -= Mathf.Lerp(0.6f, 0f, currentFaction.Cooperation) + targetFaction.Virtue;
							changeMultiplier = 3f;
							break;
						case FactionType.Empire:
							num -= 0.5f;
							break;
						}
					}
					if (controlledSectorCount > 1)
					{
						int count = EngineASX.Instance.Sectors.Count;
						if (currentFaction.FactionType == FactionType.Empire)
						{
							float num3 = (float)controlledSectorCount / (float)count;
							if (num3 > 0.25f && currentFaction.Greed > 0.35f)
							{
								num -= Mathf.Pow((num3 - 0.25f) / 0.75f, 0.6f);
							}
						}
					}
				}
			}
			if (num < -1f)
			{
				changeMultiplier += Mathf.Abs(num - -1f) * 4f;
			}
			return Mathf.Clamp(num, -1f, 1f);
		}

		private void NextFaction()
		{
			if (EngineASX.Instance.Factions.Count > 0)
			{
				currentFactionIndex++;
				if (currentFactionIndex >= EngineASX.Instance.Factions.Count)
				{
					currentFactionIndex = 0;
				}
				currentFaction = EngineASX.Instance.Factions[currentFactionIndex];
				currentFactionAttitudeIndex = -1;
			}
			else
			{
				currentFaction = null;
			}
		}
	}
}
