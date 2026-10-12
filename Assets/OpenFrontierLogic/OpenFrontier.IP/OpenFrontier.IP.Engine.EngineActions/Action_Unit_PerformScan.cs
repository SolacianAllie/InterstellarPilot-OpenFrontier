using OpenFrontier.IP.Common.Triggers;

namespace OpenFrontier.IP.Engine.EngineActions
{
	public class Action_Unit_PerformScan : EngineAction
	{
		public Unit Unit;

		public override ActionType Type => ActionType.Unit_PerformScan;

		public override void Execute()
		{
			base.Execute();
			if (Unit != null && Unit.Faction != null && Unit.Components != null)
			{
				Unit.Faction.Intel.PerformScanImmediate(Unit.Sector, Unit.SectorPosition, Unit.Components.ScanRange, Unit);
			}
		}
	}
}
