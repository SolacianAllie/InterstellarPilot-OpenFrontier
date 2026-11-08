using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.MissionGenerators;
using OpenFrontier.IP.Engine.MissionSpecs;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class MissionManager : MonoBehaviour
	{
		private Faction currentCheckFaction;

		private Dictionary<MissionManagerMissionType, MissionGenerator> missionSpecGenerators = new Dictionary<MissionManagerMissionType, MissionGenerator>();

		public MissionManagerWeighting[] MissionTypes;

		private float nextUpdateTime;

		private int numMissionsGenerated;

		public int UnitMinPreferredMissionCount = 5;

		public int UnitMaxPreferredMissionCount = 9;

		[ContextMenu("Regenerate all mission specs")]
		public void RegenerateAllMissionSpecs()
		{
			List<Unit> list = new List<Unit>();
			foreach (MissionSpec item in EngineASX.Instance.Jobs.ToList())
			{
				if (item != null)
				{
					if (item.Unit != null && !list.Contains(item.Unit))
					{
						list.Add(item.Unit);
					}
					item.SafeDestroy();
				}
			}
			foreach (Unit item2 in list)
			{
				ReplenishUnitMissions(item2);
			}
		}

		public void ReplenishUnitMissions(Unit missionLocationUnit)
		{
			if (missionLocationUnit.Faction != null && missionLocationUnit.Faction.IsValidInGame)
			{
				if (EngineASX.Instance.LocalFaction != null)
				{
					EngineASX.Instance.LocalFaction.UpdateNetWorth();
				}
				EngineASX.Instance.DestroyInvalidJobsAtUnit(missionLocationUnit);
				if (MissionTypes.Length != 0)
				{
					int preferredMissionCount = GetPreferredMissionCount(missionLocationUnit);
					int num = 0;
					while (GetUnitMissionSpecCount(missionLocationUnit) < preferredMissionCount)
					{
						GenerateRandomMissionSpec(missionLocationUnit);
						num++;
						if (num > 20)
						{
							Debug.LogWarning("Giving up trying to create missions at unit: " + missionLocationUnit, this);
							break;
						}
					}
				}
				else
				{
					Debug.LogWarning("MissionManager doesn't have any mission types defined", this);
				}
			}
			else if (LogWrapper.LogMsgs)
			{
				Debug.LogWarningFormat(this, "Cannot replenish missions at {0}. Unit faction is null or invalid", missionLocationUnit);
			}
		}

		private void Awake()
		{
			MissionGenerator[] componentsInChildren = GetComponentsInChildren<MissionGenerator>();
			foreach (MissionGenerator missionGenerator in componentsInChildren)
			{
				missionSpecGenerators[missionGenerator.MissionType] = missionGenerator;
			}
		}

		private int GetPreferredMissionCount(Unit unit)
		{
			if (unit.UnitClass.GenerateMissions)
			{
				return Random.Range(UnitMinPreferredMissionCount, UnitMaxPreferredMissionCount);
			}
			return 0;
		}

		private MissionSpec GenerateRandomMissionSpec(Unit unit)
		{
			if (unit.Faction == null)
			{
				Debug.LogError("Unit faction is null", this);
			}
			else if (MissionTypes.Length != 0)
			{
				MissionManagerWeighting randomWeighted = MissionTypes.GetRandomWeighted();
				if (randomWeighted != null)
				{
					MissionSpec missionSpec = GenerateMissionSpecFromType(randomWeighted.MissionType, unit, unit.Faction);
					if (missionSpec != null)
					{
						missionSpec.Init();
						missionSpec.SetUnit(unit, updateParent: false);
						missionSpec.SetFaction(unit.Faction, updateParent: false);
						float profitability = Mathf.Pow(Random.value, GameController.Instance.GameSettings.MissionSettings.MissionProfitabilityPower);
						missionSpec.RewardCredits = missionSpec.CalculateRewardCredits(profitability);
						missionSpec.ProfitCredits = missionSpec.CalculateProfitCredits(profitability);
						numMissionsGenerated++;
						return missionSpec;
					}
				}
				else
				{
					Debug.LogWarning("MissionManager has mission types but couldn't find one", this);
				}
			}
			return null;
		}

		private MissionSpec GenerateMissionSpecFromType(MissionManagerMissionType missionType, Unit missionLocationUnit, Faction missionLocationFaction)
		{
			MissionSpec missionSpec = null;
			MissionGenerator value = null;
			if (missionSpecGenerators.TryGetValue(missionType, out value))
			{
				if (value.CanGenerateMission(missionLocationUnit, missionLocationFaction))
				{
					missionSpec = value.GenerateMissionSpec(missionLocationUnit, missionLocationFaction, EngineASX.Instance);
					if (missionSpec == null)
					{
						Debug.LogWarning($"MissionManager has a generator for {missionType} but no mission was generated. Faction={missionLocationFaction}", this);
					}
				}
			}
			else
			{
				Debug.LogWarning("MissionManager doesn't have a generator capable of creating a " + missionType, this);
			}
			return missionSpec;
		}

		private bool UnitNeedsNewMissions(Unit unit)
		{
			return GetUnitMissionSpecCount(unit) < UnitMaxPreferredMissionCount;
		}

		private int GetUnitMissionSpecCount(Unit unit)
		{
			return EngineASX.Instance.GetJobsAtUnit(unit)?.Count ?? 0;
		}

		private void Update()
		{
			if (EngineASX.LoadedAndReady && EngineASX.Instance.World.ObjectiveState == WorldBase.ScenarioState.Playing && Time.time > nextUpdateTime)
			{
				nextUpdateTime = Time.time + 1f;
				currentCheckFaction = EngineASX.Instance.Factions.GetNextItem(1, currentCheckFaction);
				if (currentCheckFaction != null)
				{
					currentCheckFaction.DestroyInvalidMissionSpecs();
				}
			}
		}
	}
}
