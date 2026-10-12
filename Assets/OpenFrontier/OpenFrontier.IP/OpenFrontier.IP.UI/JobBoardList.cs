using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.MissionSpecs;

namespace OpenFrontier.IP.UI
{
	public class JobBoardList : ScrollList<MissionSpec>
	{
		protected override void OnRefreshing()
		{
			List<MissionSpec> list = new List<MissionSpec>();
			Unit localRootUnit = GetLocalRootUnit();
			if (localRootUnit != null)
			{
				IEnumerable<MissionSpec> missionSpecs = GetMissionSpecs(localRootUnit);
				if (missionSpecs != null)
				{
					foreach (MissionSpec item in missionSpecs)
					{
						if (item.gameObject.activeSelf && item.IsValid())
						{
							list.Add(item);
						}
					}
				}
			}
			SetItems(list);
			base.OnRefreshing();
		}

		private IEnumerable<MissionSpec> GetMissionSpecs(Unit unit)
		{
			return EngineASX.Instance.GetJobsAtUnit(unit)?.Where((MissionSpec e) => e != null).OrderBy((MissionSpec e) => e.ProfitCredits);
		}

		private Unit GetLocalRootUnit()
		{
			if (Engine != null && Engine.PlayerRootUnit != null)
			{
				return Engine.PlayerRootUnit;
			}
			return null;
		}
	}
}
