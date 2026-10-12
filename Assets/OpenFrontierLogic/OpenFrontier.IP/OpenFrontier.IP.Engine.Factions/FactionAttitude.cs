using System;
using OpenFrontier.IP.Common.Factions;
using UnityEngine;
using UnityEngine.Serialization;

namespace OpenFrontier.IP.Engine.Factions
{
	[Serializable]
	public class FactionAttitude
	{
		public double HostilityEndTime;

		public Neutrality Neutrality;

		[SerializeField]
		private float opinion;

		public bool PermanentPeace;

		[FormerlySerializedAs("PermanentWar")]
		public bool RestrictHostilityTimeout;

		public Faction TargetFaction;

		private double createdTime = -1.0;

		private float timeOfLastOpinionChange;

		public double CreatedTime
		{
			get
			{
				return createdTime;
			}
			set
			{
				createdTime = value;
			}
		}

		public float TimeOfLastOpinionChange
		{
			get
			{
				return timeOfLastOpinionChange;
			}
			set
			{
				timeOfLastOpinionChange = value;
			}
		}

		public bool IsHostile => Neutrality == Neutrality.Hostile;

		public float Opinion
		{
			get
			{
				return opinion;
			}
			set
			{
				opinion = Faction.ClampOpinion(value);
			}
		}

		public double Editor_LastTimeMadePeace => -1.0;

		public double GetAge()
		{
			if (createdTime <= 0.0)
			{
				return 10000.0;
			}
			return EngineASX.Instance.ScenarioElapsedTime - createdTime;
		}

		public void SetOpinionAndRecordTimeOfChange(float newOpinion, Faction ourFaction = null)
		{
			float num = Faction.ClampOpinion(newOpinion);
			if (num != opinion)
			{
				opinion = num;
				timeOfLastOpinionChange = Time.time;
			}
		}
	}
}
