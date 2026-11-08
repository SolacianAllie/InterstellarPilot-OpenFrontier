using OpenFrontier.IP.Common.Triggers;

namespace OpenFrontier.IP.SavedGames.V2.Model.Triggers
{
	public class ModelTrigger_Unit_HasCargo : ModelTrigger
	{
		public ComparisonOp Operator = ComparisonOp.LessThan;

		public override TriggerType Type => TriggerType.Unit_HasCargo;

		public ModelCargoClass CargoTypeId { get; set; }

		public int Quantity { get; set; } = 1;

		public ModelUnit TargetUnit { get; set; }
	}
}
