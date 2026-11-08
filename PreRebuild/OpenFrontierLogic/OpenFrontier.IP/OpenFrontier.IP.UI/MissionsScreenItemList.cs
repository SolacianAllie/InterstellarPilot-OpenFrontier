using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP.UI
{
	public class MissionsScreenItemList : ScrollList<Mission>
	{
		public Color CompletedTextColor = Color.green;

		public Color DefaultTextColor = Color.white;

		public Color FailedTextColor = Color.grey;

		private int lastActiveMissionCount = -1;

		public MissionsScreen MissionsUI;

		public bool ShowCompletedMissions;

		protected override void OnRefreshing()
		{
			base.OnRefreshing();
			List<Mission> list = new List<Mission>();
			Engine.AbortInvalidMissions();
			foreach (Mission item in from e in Engine.Missions
				orderby e.IsFinished, 0.0 - e.StartTime
				select e)
			{
				if ((!item.IsFinished || ShowCompletedMissions) && IsItemFiltered(item) && item.ShowInJournal)
				{
					list.Add(item);
				}
			}
			SetItems(list);
		}

		protected override bool DetermineIsStale()
		{
			if (Engine.World != null && lastActiveMissionCount != Engine.World.ActiveMissionCount)
			{
				lastActiveMissionCount = Engine.World.ActiveMissionCount;
				return true;
			}
			return false;
		}
	}
}
