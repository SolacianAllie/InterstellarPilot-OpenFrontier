using System.Collections.Generic;
using OpenFrontier.IP.SavedGames.V2.Model.Jobs;

namespace OpenFrontier.IP.SavedGames.V2.Model
{
	public class ModelUnit
	{
		public int Id { get; set; }

		public int Seed { get; set; } = -1;

		public ModelUnitClass Class { get; set; }

		public ModelSector Sector { get; set; }

		public Vec3 Position { get; set; }

		public Vec3 Rotation { get; set; }

		public ModelFaction Faction { get; set; }

		public int RpProvision { get; set; }

		public ModelUnitCargoData CargoData { get; set; }

		public ModelUnitDebrisData DebrisData { get; set; }

		public ModelUnitShipTraderData ShipTraderData { get; set; }

		public ModelUnitProjectileData ProjectileData { get; set; }

		public ModelUnitAsteroidData AsteroidData { get; set; }

		public string Name { get; set; }

		public string ShortName { get; set; }

		public ModelComponentUnitData ComponentUnitData { get; set; }

		public ModelUnitHealthData HealthData { get; set; }

		public bool IsInvulnerable { get; set; }

		public bool AvoidDestruction { get; set; }

		public float TotalDamagedReceived { get; set; }

		public List<ModelPassengerGroup> PassengerGroups { get; set; } = new List<ModelPassengerGroup>(10);

		public ModelUnitWormholeData WormholeData { get; set; }

		public List<ModelJob> Jobs { get; set; } = new List<ModelJob>();

		public ModelUnitActiveData ActiveData { get; set; }

		public float? Radius { get; set; }

		public float? Mass { get; set; }

		public string CustomClassName { get; set; }
	}
}
