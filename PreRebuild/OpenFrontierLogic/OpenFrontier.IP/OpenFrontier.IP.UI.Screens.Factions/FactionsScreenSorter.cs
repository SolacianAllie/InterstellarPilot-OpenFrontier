using System;
using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.UI.Controls;

namespace OpenFrontier.IP.UI.Screens.Factions
{
	public class FactionsScreenSorter : ListSorter, IComparer<Faction>
	{
		public FactionsSortMode SortMode;

		public override int GetSortMode()
		{
			return (int)SortMode;
		}

		public override void SetSortMode(int sortType)
		{
			SortMode = (FactionsSortMode)sortType;
		}

		public override List<string> GetSortOptionNames()
		{
			return Enum.GetNames(typeof(FactionsSortMode)).ToList();
		}

		public int Compare(Faction x, Faction y)
		{
			switch (SortMode)
			{
			case FactionsSortMode.Wealth:
				return y.Credits.CompareTo(x.Credits) * (int)SortOrder;
			case FactionsSortMode.Power:
			{
				long cachedNetWorth = x.GetCachedNetWorth();
				return y.GetCachedNetWorth().CompareTo(cachedNetWorth) * (int)SortOrder;
			}
			case FactionsSortMode.Opinion:
			{
				Faction localFaction = x.Engine.LocalFaction;
				if (localFaction != null)
				{
					float factionOpinionSortValue = GetFactionOpinionSortValue(x, localFaction);
					float factionOpinionSortValue2 = GetFactionOpinionSortValue(y, localFaction);
					return factionOpinionSortValue.CompareTo(factionOpinionSortValue2) * (int)SortOrder;
				}
				return x.GetLongNameElseShort().CompareTo(y.GetLongNameElseShort()) * (int)SortOrder;
			}
			default:
				return x.GetLongNameElseShort().CompareTo(y.GetLongNameElseShort()) * (int)SortOrder;
			}
		}

		private float GetFactionOpinionSortValue(Faction faction, Faction targetFaction)
		{
			Neutrality neutralityWithInternal = faction.GetNeutralityWithInternal(targetFaction);
			float num = 0f;
			switch (neutralityWithInternal)
			{
			case Neutrality.Hostile:
				num = -10f;
				break;
			case Neutrality.Allied:
				num = 10f;
				break;
			}
			float opinion = faction.GetOpinion(targetFaction);
			return num + opinion;
		}
	}
}
