using OpenFrontier.IP.Common;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine.MissionSpecs
{
	public abstract class MissionSpec : MonoBehaviour
	{
		private Faction faction;

		private EngineASX engine;

		public double ExpiryTime;

		private bool hasInit;

		public Mission MissionPrefab;

		private int profitCredits;

		private int rewardCredits;

		private int uniqueId = -1;

		private Unit unit;

		private bool destroyed;

		public int UniqueId
		{
			get
			{
				return uniqueId;
			}
			set
			{
				uniqueId = value;
			}
		}

		public EngineASX Engine
		{
			get
			{
				return engine;
			}
			private set
			{
				if (!(engine != value))
				{
					return;
				}
				EngineASX engineASX = engine;
				engine = value;
				if (engineASX != null)
				{
					engineASX.DeregisterMissionSpec(this);
				}
				if (engine != null)
				{
					if (UniqueId < 0)
					{
						UniqueId = engine.GetUniqueJobId();
					}
					engine.RegisterMissionSpec(this);
				}
			}
		}

		public bool WaitTimeExpired => EngineASX.Instance.ScenarioElapsedTime > ExpiryTime;

		public Faction Faction => faction;

		public Unit Unit => unit;

		public int RewardCredits
		{
			get
			{
				return rewardCredits;
			}
			set
			{
				rewardCredits = value;
			}
		}

		public int ProfitCredits
		{
			get
			{
				return profitCredits;
			}
			set
			{
				profitCredits = value;
			}
		}

		public abstract JobType JobType { get; }

		public void SetFaction(Faction value, bool updateParent)
		{
			if (this.faction != value)
			{
				Faction faction = this.faction;
				this.faction = value;
				if (faction != null)
				{
					faction.MissionSpecs.Remove(this);
				}
				if (this.faction != null)
				{
					this.faction.MissionSpecs.Add(this);
				}
				if (updateParent)
				{
					SetParent();
				}
			}
		}

		public void SetUnit(Unit value, bool updateParent)
		{
			if (this.unit != value)
			{
				Unit unit = this.unit;
				this.unit = value;
				if (unit != null)
				{
					EngineASX.Instance.DeregisterMissionSpecFromUnit(unit, this);
				}
				if (this.unit != null)
				{
					EngineASX.Instance.RegisterMissionSpecAtUnit(this.unit, this);
				}
				if (updateParent)
				{
					SetParent();
				}
			}
		}

		public void Init()
		{
			if (!hasInit)
			{
				Engine = EngineASX.Instance;
				SetUnit(FindParentUnit(), updateParent: false);
				SetFaction((unit != null) ? unit.Faction : null, updateParent: false);
				hasInit = true;
			}
		}

		public void SetParent()
		{
			if ((bool)gameObject)
			{
				if (unit != null)
				{
					gameObject.transform.SetParent(unit.gameObject.transform);
					gameObject.transform.localPosition = Vector3.zero;
				}
				else
				{
					gameObject.transform.SetParent(null);
				}
			}
		}

		public Unit FindParentUnit()
		{
			return UnityObjectHelper.FindInParentsOrSelf<Unit>(gameObject);
		}

		public virtual bool IsValid()
		{
			if (unit != null)
			{
				return Faction != null;
			}
			return false;
		}

		public Mission CreateMission(Faction factionTakingOnMission)
		{
			Mission mission = createMission();
			if (mission != null)
			{
				mission.MissionGiverFaction = Faction;
				mission.transform.SetParent(engine.World.transform);
				mission.transform.localPosition = Vector3.zero;
				mission.MissionRewardCredits = RewardCredits;
				mission.FailureOpinionChange = CalculateFailureOpinionChange();
				mission.CompletionOpinionChange = CalculateCompletionOpinionChange();
				mission.OwnerFaction = factionTakingOnMission;
				mission.Init();
				mission.InitObjectives();
			}
			return mission;
		}

		public virtual int CalculateProfitCredits(float profitability)
		{
			return 0;
		}

		public virtual int CalculateRewardCredits(float profitability)
		{
			return 0;
		}

		public virtual bool CanAcceptMission()
		{
			return true;
		}

		public abstract string CalculateBrief();

		public abstract string CalculateMissionSpecTitle();

		public virtual float CalculateCompletionOpinionChange()
		{
			return engine.GameSettings.MissionSettings.CompletionMinOpinionChange + (float)profitCredits / engine.GameSettings.MissionSettings.CompletionOpinionChangeReferenceValue;
		}

		public virtual float CalculateFailureOpinionChange()
		{
			return 0f;
		}

		public virtual void OnDisplayedToPlayer()
		{
		}

		protected virtual Mission createMission()
		{
			return null;
		}

		public void SafeDestroy()
		{
			if (!destroyed)
			{
				Cleanup();
				Object.Destroy(gameObject);
				destroyed = true;
			}
		}

		private void Cleanup()
		{
			SetUnit(null, updateParent: false);
			SetFaction(null, updateParent: false);
			Engine = null;
		}

		public virtual void AutoNameGameObject()
		{
			gameObject.name = $"{GetType().Name}: {uniqueId}";
		}
	}
}
