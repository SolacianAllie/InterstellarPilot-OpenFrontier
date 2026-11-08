using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.EngineActions;
using OpenFrontier.IP.Testing.Spawning;
using UnityEngine;

namespace OpenFrontier.IP.Assets.Scripts.Engine.EngineActions
{
	public class Action_Spawn_BanditHorde : EngineAction
	{
		public Sector TargetSector;

		public Vector3 TargetSectorPosition = Vector3.zero;

		public override void Execute()
		{
			base.Execute();
			if (TargetSector != null)
			{
				SpawnUtils.SpawnBanditHordesAtSectorPosition(TargetSector, TargetSectorPosition);
			}
		}
	}
}
