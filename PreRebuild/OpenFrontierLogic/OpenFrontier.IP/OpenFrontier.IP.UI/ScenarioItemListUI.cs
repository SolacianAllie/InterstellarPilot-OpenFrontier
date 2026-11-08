using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;

namespace OpenFrontier.IP.UI
{
	public class ScenarioItemListUI : ScrollList<ScenarioInfo>
	{
		public List<ScenarioInfo> VisibleScenarios = new List<ScenarioInfo>();

		public bool ShowTutorials;

		public bool ShowMissions = true;

		protected override void OnRefreshing()
		{
			base.OnRefreshing();
			List<ScenarioInfo> list = new List<ScenarioInfo>();
			foreach (ScenarioInfo item in from e in VisibleScenarios
				where e != null && ((ShowTutorials && e.IsTutorial) || (ShowMissions && !e.IsTutorial))
				orderby e.ComingSoon, !e.IsTutorial, e.DateYear, e.DateMonth, e.DateDay
				select e)
			{
				if (item.ShowInScenarioUI && (item.ShowInListWhenLocked || item.GetIsUnlocked()))
				{
					list.Add(item);
				}
			}
			SetItems(list);
		}
	}
}
