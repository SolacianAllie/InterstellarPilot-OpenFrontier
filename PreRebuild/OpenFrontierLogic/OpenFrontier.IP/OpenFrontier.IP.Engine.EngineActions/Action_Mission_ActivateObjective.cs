using System.Collections.Generic;
using OpenFrontier.IP.Common.Triggers;
using OpenFrontier.IP.Engine.MissionObjectives;
using UnityEngine;

namespace OpenFrontier.IP.Engine.EngineActions
{
	public class Action_Mission_ActivateObjective : EngineAction
	{
		public MissionObjective Objective;

		public List<MissionObjective> Objectives = new List<MissionObjective>();

		public override ActionType Type => ActionType.Mission_ActivateObjective;

		protected override void validate()
		{
			base.validate();
			if (Objective != null)
			{
				Debug.LogWarning("Using MissionObjective is obsolete. Use Objectives instead");
			}
		}

		public override void Execute()
		{
			base.Execute();
			if (Objective != null)
			{
				Objective.MakeActive();
			}
			for (int i = 0; i < Objectives.Count; i++)
			{
				if (Objectives[i] != null)
				{
					Objectives[i].MakeActive();
				}
			}
		}
	}
}
