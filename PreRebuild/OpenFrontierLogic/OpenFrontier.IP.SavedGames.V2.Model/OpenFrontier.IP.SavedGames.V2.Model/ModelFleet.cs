using System.Collections.Generic;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.SavedGames.V2.Model.FleetOrders;

namespace OpenFrontier.IP.SavedGames.V2.Model
{
	public class ModelFleet
	{
		public int Id { get; set; }

		public string Name { get; set; }

		public Vec3 Position { get; set; }

		public Vec4 Rotation { get; set; }

		public ModelSector Sector { get; set; }

		public ModelFaction Faction { get; set; }

		public ModelSectorTarget HomeBase { get; set; }

		public bool ExcludeFromFactionAI { get; set; }

		public ModelFleetSettings FleetSettings { get; set; }

		public ModelFleetOrderCollection OrdersCollection { get; set; } = new ModelFleetOrderCollection();

		public bool IsActive { get; set; } = true;

		public List<ModelNpcPilot> Npcs { get; set; } = new List<ModelNpcPilot>();

		public int Seed { get; set; } = -1;

		public FactionStrategy Strategy { get; set; }

		public int FormationId { get; set; } = -1;
	}
}
