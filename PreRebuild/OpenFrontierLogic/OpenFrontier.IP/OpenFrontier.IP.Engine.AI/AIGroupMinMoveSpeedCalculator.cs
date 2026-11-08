using System;
using OpenFrontier.IP.Engine.UnitComponents;

namespace OpenFrontier.IP.Engine.AI
{
	public static class AIGroupMinMoveSpeedCalculator
	{
		public static float Calculate(Fleet fleet)
		{
			float num = 0f;
			bool flag = false;
			if (fleet.NpcPilots.Count > 0)
			{
				foreach (NpcPilot npcPilot in fleet.NpcPilots)
				{
					UnitComponentHolder currentUnitComponents = npcPilot.CurrentUnitComponents;
					if (!(currentUnitComponents != null))
					{
						continue;
					}
					float num2 = 0f;
					if (currentUnitComponents.IsMobile)
					{
						UnitEngineComponent engineComponent = currentUnitComponents.EngineComponent;
						if (engineComponent != null)
						{
							num2 = engineComponent.GetCurrentMaxSpeedConsideringDamage(npcPilot.CurrentUnit);
						}
					}
					if (!flag)
					{
						num = num2;
						flag = true;
					}
					else
					{
						num = Math.Min(num, num2);
					}
				}
			}
			return num;
		}
	}
}
