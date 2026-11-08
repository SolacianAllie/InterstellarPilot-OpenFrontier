using System.Collections.Generic;
using OpenFrontier.IP.Engine;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Testing.MovePlayer
{
	public static class MovePlayerToRandomUnstableWormhole
	{
		public static void Move()
		{
			if (!EngineASX.LoadedAndReady || !(EngineASX.Instance.PlayerUnit != null))
			{
				return;
			}
			Wormhole wormhole = PickRandomWormhole();
			if (wormhole != null)
			{
				int num = 5;
				for (int i = 0; i < num; i++)
				{
					if (TryMoveToRandomWormhole(wormhole))
					{
						break;
					}
				}
			}
			else
			{
				Debug.Log("Could not find unstable wormhole");
			}
		}

		private static bool TryMoveToRandomWormhole(Wormhole wormhole)
		{
			Vector3 vector = Vector3.Normalize(wormhole.transform.localPosition);
			float num = Mathf.Clamp(wormhole.transform.localPosition.magnitude - 300f, 300f, 20000f);
			Unit playerUnit = EngineASX.Instance.PlayerUnit;
			Vector3 vector2 = wormhole.Unit.SectorPosition + vector * num;
			if (wormhole.Sector != EngineASX.Instance.ActiveSector)
			{
				EngineASX.Instance.TryStartSectorTransition(playerUnit, null, wormhole.Sector, vector2, Quaternion.LookRotation(vector, Vector3.up));
				return true;
			}
			MovePlayerTestUtils.MoveLocalUnitToSectorPosition(playerUnit.Sector, vector2);
			Debug.LogWarning("Todo - not implemented");
			return false;
		}

		private static Wormhole PickRandomWormhole()
		{
			List<Wormhole> list = new List<Wormhole>();
			foreach (Sector sector in EngineASX.Instance.Sectors)
			{
				List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Wormhole);
				if (unitsByType == null)
				{
					continue;
				}
				foreach (Unit item in unitsByType)
				{
					if (item.IsValidAndNotDestroyed && item.WormholeComponent.IsUnstable)
					{
						list.Add(item.WormholeComponent);
					}
				}
			}
			return list.GetRandom();
		}
	}
}
