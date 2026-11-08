using System.Collections.Generic;
using OpenFrontier.IP.Common;

namespace OpenFrontier.IP.SavedGames.V2.Model
{
	public class ModelComponentUnitData
	{
		public double? CaptureCooldownTime { get; set; }

		public string CustomShipName { get; set; }

		public float? CargoCapacity { get; set; }

		public int? ScanRange { get; set; }

		public ModelComponentUnitFactoryData FactoryData { get; set; }

		public ConstructionState ConstructionState { get; set; }

		public float ConstructionProgress { get; set; } = 1f;

		public ModelComponentUnitModData ModData { get; set; }

		public float? CapacitorCharge { get; set; }

		public bool IsCloaked { get; set; }

		public List<int> PoweredDownBayIds { get; set; } = new List<int>();

		public List<int> AutoFireBayIds { get; set; } = new List<int>();

		public float? EngineThrottle { get; set; }

		public ModelComponentUnitCargoData CargoData { get; set; }

		public ModelComponentUnitShieldHealthData ShieldData { get; set; }

		public ModelComponentUnitComponentHealthData ComponentHealthData { get; set; }

		public ModelComponentUnitDockData DockData { get; set; }

		public ModelPerson Pilot { get; set; }

		public List<ModelPerson> People { get; set; } = new List<ModelPerson>();

		public int ShipNameIndex { get; set; }

		public AutoTurretFireMode? AutoTurretFireMode { get; set; }
	}
}
