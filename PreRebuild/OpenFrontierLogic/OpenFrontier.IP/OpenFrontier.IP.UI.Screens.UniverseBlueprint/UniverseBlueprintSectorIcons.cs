using OpenFrontier.IP.Engine.WorldGeneration.Models;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.UniverseBlueprint
{
	public class UniverseBlueprintSectorIcons : MonoBehaviour
	{
		public Image PlanetImage;

		public Image AsteroidIconImage;

		public Image GasCloudImage;

		public SectorBlueprint SectorBlueprint;

		public void Refresh()
		{
			RefreshAsteroidImage();
			RefreshPlanetImage();
		}

		private void RefreshPlanetImage()
		{
			PlanetImage.gameObject.SetActive(SectorBlueprint != null && SectorBlueprint.HasPlanets);
		}

		private void RefreshAsteroidImage()
		{
			if (SectorBlueprint != null)
			{
				bool flag = SectorBlueprint.HasAsteroids && SectorBlueprint.AsteroidType != null;
				AsteroidIconImage.gameObject.SetActive(flag);
				if (flag)
				{
					AsteroidIconImage.sprite = SectorBlueprint.AsteroidType.UniverseMapSectorTypeSprite;
				}
				bool active = !flag && SectorBlueprint.HasGasClouds;
				GasCloudImage.gameObject.SetActive(active);
			}
			else
			{
				AsteroidIconImage.gameObject.SetActive(value: false);
			}
		}
	}
}
