using System.Collections.Generic;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.SavedGames.V2.Model.Factions;
using OpenFrontier.IP.SavedGames.V2.Model.Factions.Bounty;

namespace OpenFrontier.IP.SavedGames.V2.Model
{
	public class ModelFaction
	{
		public int Id { get; set; }

		public ModelSector HomeSector { get; set; }

		public Vec3? HomeSectorPosition { get; set; }

		public int GeneratedNameId { get; set; } = -1;

		public int GeneratedSuffixId { get; set; } = -1;

		public string CustomName { get; set; }

		public string CustomShortName { get; set; }

		public int Credits { get; set; }

		public string Description { get; set; }

		public bool IsCivilian { get; set; }

		public FactionType FactionType { get; set; }

		public float Aggression { get; set; }

		public float Virtue { get; set; }

		public float Greed { get; set; }

		public float Cooperation { get; set; }

		public float TradeEfficiency { get; set; }

		public bool DynamicRelations { get; set; }

		public bool ShowJobBoards { get; set; }

		public bool CreateJobs { get; set; }

		public float RequisitionPointMultiplier { get; set; }

		public bool DestroyWhenNoUnits { get; set; }

		public float MinNpcCombatEfficiency { get; set; } = 0.25f;

		public float MaxNpcCombatEfficiency { get; set; } = 1f;

		public int AdditionalRpProvision { get; set; }

		public bool TradeIllegalGoods { get; set; }

		public double SpawnTime { get; set; }

		public long HighestEverNetWorth { get; set; }

		public ModelFactionCustomSettings CustomSettings { get; set; }

		public ModelFactionStats Stats { get; set; }

		public List<ModelSector> AutopilotExcludedSectors { get; set; } = new List<ModelSector>();

		public ModelFactionIntel Intel { get; set; } = new ModelFactionIntel();

		public ModelFactionRelationData Relations { get; set; } = new ModelFactionRelationData();

		public List<ModelFactionRecentDamageItem> RecentDamageItems { get; set; } = new List<ModelFactionRecentDamageItem>();

		public ModelFactionOpinionData Opinions { get; set; } = new ModelFactionOpinionData();

		public ModelPerson Leader { get; set; }

		public ModelFactionAI FactionAI { get; set; }

		public ModelFactionBountyBoard BountyBoard { get; set; }

		public List<ModelFactionTransaction> Transactions { get; set; } = new List<ModelFactionTransaction>();

		public int RankingSystemId { get; set; } = -1;

		public int PreferredFormationId { get; set; } = -1;

		public List<byte> AvatarProfileIds { get; set; }
	}
}
