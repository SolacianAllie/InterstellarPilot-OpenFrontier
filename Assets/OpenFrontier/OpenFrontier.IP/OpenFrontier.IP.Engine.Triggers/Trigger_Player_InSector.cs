using OpenFrontier.IP.Common.Triggers;
using UnityEngine.Serialization;

namespace OpenFrontier.IP.Engine.Triggers
{
	public class Trigger_Player_InSector : TriggerBase
	{
		[FormerlySerializedAs("Scene")]
		public Sector Sector;

		public override TriggerType Type => TriggerType.Player_InSector;

		protected override bool evaluate(EngineASX engine)
		{
			if (engine.LocalPlayer != null && Sector != null)
			{
				return engine.LocalPlayer.Sector == Sector;
			}
			return false;
		}
	}
}
