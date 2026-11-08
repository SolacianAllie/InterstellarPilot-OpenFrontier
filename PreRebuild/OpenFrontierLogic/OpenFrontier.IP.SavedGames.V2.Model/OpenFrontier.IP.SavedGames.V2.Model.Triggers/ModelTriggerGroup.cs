using System.Collections.Generic;
using OpenFrontier.IP.Common.Triggers;
using OpenFrontier.IP.SavedGames.V2.Model.Actions;

namespace OpenFrontier.IP.SavedGames.V2.Model.Triggers
{
	public class ModelTriggerGroup
	{
		public int Id { get; set; }

		public float EvaluateFrequency { get; set; } = 1f;

		public bool FireAndDisable { get; set; } = true;

		public bool IsActive { get; set; } = true;

		public double NextEvaluationTime { get; set; }

		public int FireCount { get; set; }

		public int MaxFireCount { get; set; }

		public TriggerMaxFiredAction MaxFiredAction { get; set; } = TriggerMaxFiredAction.Destroy;

		public List<ModelTrigger> Triggers { get; set; } = new List<ModelTrigger>();

		public List<ModelAction> Actions { get; set; } = new List<ModelAction>();
	}
}
