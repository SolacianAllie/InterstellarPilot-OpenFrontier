using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.MissionObjectives;
using UnityEngine;

namespace OpenFrontier.IP.UI
{
	public class MissionObjectivesGrid : ScrollList<MissionObjective>
	{
		public Color CompletedTextColor = Color.green;

		public Color DefaultTextColor = Color.white;

		public Color FailedTextColor = Color.grey;

		public Mission Mission;

		protected override bool DetermineIsStale()
		{
			return false;
		}

		protected override void OnRefreshing()
		{
			base.OnRefreshing();
			if (Mission != null)
			{
				SetItems(Mission.Objectives.Where((MissionObjective e) => e.ShowInJournal));
			}
		}
	}
}
