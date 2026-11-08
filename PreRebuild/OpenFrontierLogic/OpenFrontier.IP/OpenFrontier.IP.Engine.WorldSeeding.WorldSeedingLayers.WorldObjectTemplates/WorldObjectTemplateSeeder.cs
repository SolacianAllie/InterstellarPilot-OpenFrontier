using OpenFrontier.IP.Engine.WorldObjectTemplates;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers.WorldObjectTemplates
{
	[RequireComponent(typeof(WorldSeederLayer))]
	public class WorldObjectTemplateSeeder : MonoBehaviour
	{
		public GameObject Prefab;

		public WorldObjectTemplateSeedParams SeedParams;

		public WorldObjectTemplateSeederSectorParams SectorParams;

		public void SeedLayer(WorldBase world)
		{
			if ((SeedParams.DontSpawnWhenFactionSeedingDisabled && !world.Seeder.Settings.FactionSeederSettings.SeedNonBanditFactions) || !(Prefab != null))
			{
				return;
			}
			float templateRadius = GetRadius(Prefab) * 1.2f;
			if (SectorParams.GetSectorAndPosition(templateRadius, out var sector, out var sectorPosition))
			{
				WorldObjectTemplateInitializer.InitObjectsInSector(Prefab, sector, sectorPosition, Geometry.RandomYRotation());
				if (SeedParams.DiscoverOtherFactions)
				{
					Debug.LogError("Not implemented", this);
				}
				if (SeedParams.SeedOpinionsWithOtherFactions)
				{
					Debug.LogError("Not implemented", this);
				}
			}
			else
			{
				Debug.LogWarning($"The world object template will not be spawned as no safe position can be found: {this}", this);
			}
		}

		private float GetRadius(GameObject prefab)
		{
			Unit[] componentsInChildren = prefab.GetComponentsInChildren<Unit>();
			float num = 100f;
			Unit[] array = componentsInChildren;
			foreach (Unit unit in array)
			{
				float num2 = unit.transform.localPosition.magnitude + unit.Radius * 1.2f;
				if (unit.UnitClass.DisplayData != null)
				{
					num2 += unit.UnitClass.DisplayData.BuildBlockerRadius;
				}
				num = Mathf.Max(num, num2);
			}
			return num;
		}
	}
}
