using OpenFrontier.IP.Engine.Factions.Intel;
using OpenFrontier.IP.Engine.TraderHeatmap;

namespace OpenFrontier.IP.Engine.Factions
{
	public class OutlawTraderHeatmapSearch
	{
		private TraderHeatmapTarget? bestTarget;

		private float bestScore;

		public Sector HomeSector { get; set; }

		public Fleet Fleet { get; set; }

		public int CurrentSearchIndex { get; set; } = -1;

		public TraderHeatmapTarget? BestTarget => bestTarget;

		public bool IsSearching => CurrentSearchIndex < EngineASX.Instance.TraderHeatmapModule.Targets.Count;

		public static OutlawTraderHeatmapSearch Init(Fleet fleet)
		{
			return new OutlawTraderHeatmapSearch
			{
				Fleet = fleet,
				HomeSector = fleet.GetHomeSectorOrCurrent(),
				CurrentSearchIndex = -1
			};
		}

		public void ProcessSearch()
		{
			CurrentSearchIndex++;
			if (CurrentSearchIndex >= EngineASX.Instance.TraderHeatmapModule.Targets.Count)
			{
				return;
			}
			TraderHeatmapTarget value = EngineASX.Instance.TraderHeatmapModule.Targets[CurrentSearchIndex];
			if (!(value.TargetFaction != null) || !(value.TargetFaction != Fleet.Faction) || !(Fleet.Faction.GetEffectiveOpinionOrNull(value.TargetFaction).GetValueOrDefault() < 0.1f))
			{
				return;
			}
			UniversePath universePath = Fleet.Faction.Intel.GetUniversePath(HomeSector, value.Sector);
			if (universePath != null && universePath.Jumps >= 0 && universePath.Jumps <= Fleet.Settings.MaxJumpDistance)
			{
				float score = value.Score;
				score -= (float)universePath.Jumps * 0.2f;
				if (!bestTarget.HasValue || score > bestScore)
				{
					bestScore = score;
					bestTarget = value;
				}
			}
		}
	}
}
