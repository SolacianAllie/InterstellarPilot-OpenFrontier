using System.Collections.Generic;

namespace OpenFrontier.IP.SavedGames.V2.Model
{
	public class ModelFleetSpawnParams
	{
		public List<ModelFleetSpawnParamsItem> Items = new List<ModelFleetSpawnParamsItem>();

		public ModelFaction Faction { get; set; }

		public string FleetResourceName { get; set; }

		public ModelUnit HomeBaseUnit { get; set; }

		public ModelSector HomeSector { get; set; }

		public string ShipDesignation { get; set; }

		public ModelUnit TargetDockUnit { get; set; }

		public Vec3 TargetPosition { get; set; }

		public ModelSector TargetSector { get; set; }
	}
}
