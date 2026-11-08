using System;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.WorldGeneration.Models;
using OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers;
using OpenFrontier.IP.UI.Extensions;
using OpenFrontier.IP.UI.Screens.EnterNumber;
using OpenFrontier.IP.UI.Screens.UniverseBlueprint;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace OpenFrontier.IP.UI.Screens.UniverseBlueprintMap
{
	public class UniverseBlueprintMapScreen : ScreenBase
	{
		public UniverseBlueprintDrawer UniverseBlueprintDrawer;

		public CreateBlueprintSectorsSeederSettings CreateBlueprintSectorsSeederSettings;

		public WorldGeneratorSettings WorldGeneratorSettings;

		public WorldBlueprint WorldBlueprint;

		public Button RegenerateButton;

		public Func<WorldBlueprint> RegenerateCallback;

		public Button SetSeedValueButton;

		protected override void awake()
		{
			base.awake();
			RegenerateButton.onClick.AddListener(RegenerateButtonClick);
			SetSeedValueButton.onClick.AddListener(SetSeedValueButtonClick);
		}

		private void SetSeedValueButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowEnterNumberScreen(WorldGeneratorSettings.SeedValue, EnterSeedValueConfirmed, "Enter seed value");
		}

		private void UpdateSeedButtonText()
		{
			SetSeedValueButton.GetComponentInChildren<Text>().text = $"Seed: {WorldGeneratorSettings.SeedValue}";
		}

		private void EnterSeedValueConfirmed(EnterNumberScreen sender, bool confirmed, int? newValue)
		{
			if (confirmed)
			{
				WorldGeneratorSettings.SeedValue = newValue.Value;
				UpdateSeedButtonText();
				TryRegenerate();
			}
		}

		private void RegenerateButtonClick()
		{
			WorldGeneratorSettings.SeedValue = UnityEngine.Random.Range(1, 1000000);
			UniverseBlueprintDrawer.MapScrollView.velocity = Vector2.zero;
			UniverseBlueprintDrawer.MapScrollView.ResetScrollPosition();
			TryRegenerate();
		}

		private void TryRegenerate()
		{
			if (RegenerateCallback != null)
			{
				WorldBlueprint = RegenerateCallback();
				Refresh();
			}
		}

		protected override void refresh()
		{
			base.refresh();
			UniverseBlueprintDrawer.CreateBlueprintSectorsSeederSettings = CreateBlueprintSectorsSeederSettings;
			UniverseBlueprintDrawer.Refresh(WorldBlueprint);
			UpdateSeedButtonText();
		}
	}
}
