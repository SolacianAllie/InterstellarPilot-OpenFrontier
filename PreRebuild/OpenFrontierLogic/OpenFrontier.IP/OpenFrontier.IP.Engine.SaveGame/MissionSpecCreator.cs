using OpenFrontier.IP.Common;
using OpenFrontier.IP.Engine.MissionSpecs;

namespace OpenFrontier.IP.Engine.SaveGame
{
	public static class MissionSpecCreator
	{
		public static MissionSpec CreateMissionSpecFromType(JobType specType, Unit unit)
		{
			return specType switch
			{
				JobType.DestroyGroup => (MissionSpec)UnityObjectHelper.NewGameObject<DestroyGroupMissionSpec>(unit.transform), 
				JobType.Courier => UnityObjectHelper.NewGameObject<CourierMissionSpec>(unit.transform), 
				JobType.DeliverShip => UnityObjectHelper.NewGameObject<DeliverShipMissionSpec>(unit.transform), 
				JobType.Breakdown => UnityObjectHelper.NewGameObject<BreakdownMissionSpec>(unit.transform), 
				_ => null, 
			};
		}
	}
}
