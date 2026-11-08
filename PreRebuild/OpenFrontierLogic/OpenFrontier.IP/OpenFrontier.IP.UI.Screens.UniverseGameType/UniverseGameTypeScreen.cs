using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.billing;
using OpenFrontier.Unity.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.UniverseGameType
{
	public class UniverseGameTypeScreen : ScreenBase
	{
		public UniverseGameTypeList GameTypeList;

		public Button PlayButton;

		public Text StartingCreditsLabel;

		public Text StartingShipsLabel;

		public UniverseScenarioData UniverseScenarioData = new UniverseScenarioData();

		public GameObject PlayableGameTypesRoot;

		public List<UniverseGameTypeInfo> ShownGameTypes => (from e in PlayableGameTypesRoot.GetComponentsInChildren<UniverseGameTypeOption>()
			where e.GameTypeInfo != null && CanPlayGameType(e)
			select e.GameTypeInfo).ToList();

		public void Play()
		{
			if (GameTypeList.FirstSelectedItem != null)
			{
				if (HasAllRequiredProductsForSelectedItem())
				{
					UniverseScenarioData.GameTypeInfo = GameTypeList.FirstSelectedItem;
					UniverseScenarioData.PilotName = GameController.Instance.DefaultPilotName;
					UniverseScenarioData.FullSaveGamePath = null;
					ScenarioUI.PlayScenario(UniverseScenarioData, GameController.EngineLaunchSource.Unspecified);
				}
				else
				{
					IPProduct requestedProduct = GameTypeList.FirstSelectedItem.RequiredProducts.SingleOrDefault();
					NavigateToStoreScreen(requestedProduct);
				}
			}
		}

		private bool HasAllRequiredProductsForSelectedItem()
		{
			UniverseGameTypeInfo firstSelectedItem = GameTypeList.FirstSelectedItem;
			firstSelectedItem.RequiredProducts.TrimNulls();
			foreach (IPProduct requiredProduct in firstSelectedItem.RequiredProducts)
			{
				if (!requiredProduct.IsPurchased)
				{
					return false;
				}
			}
			return true;
		}

		private void NavigateToStoreScreen(IPProduct requestedProduct = null)
		{
			ScreenNavigator.Instance.ShowStoreScreen(requestedProduct);
		}

		protected override void awake()
		{
			base.awake();
			PlayButton.onClick.AddListener(Play);
			GameTypeList.SelectedItemChanged += GameTypeList_SelectedItemChanged;
		}

		protected override void refresh()
		{
			base.refresh();
			List<UniverseGameTypeInfo> shownGameTypes = ShownGameTypes;
			GameTypeList.SetItems(shownGameTypes);
		}

		protected override void onMadeCurrentPanel(bool navigatedForward)
		{
			base.onMadeCurrentPanel(navigatedForward);
			Refresh();
		}

		private bool CanPlayGameType(UniverseGameTypeOption e)
		{
			switch (e.GameTypeInfo.GameType)
			{
			case OpenFrontier.IP.Engine.UniverseGameType.Bandits:
				if (UniverseScenarioData.ScenarioInfo == GameController.Instance.SandboxScenarioInfo)
				{
					return GameController.Instance.SandboxPlayerSettings.FactionSeedingBanditFactions;
				}
				return true;
			case OpenFrontier.IP.Engine.UniverseGameType.TheFugitive:
			case OpenFrontier.IP.Engine.UniverseGameType.Empire:
				if (UniverseScenarioData.ScenarioInfo == GameController.Instance.SandboxScenarioInfo)
				{
					return GameController.Instance.IsEmpireSeedingInSandboxEnabled;
				}
				return true;
			default:
				return true;
			}
		}

		private void GameTypeList_SelectedItemChanged(ScrollList<UniverseGameTypeInfo> sender, UniverseGameTypeInfo oldItem, UniverseGameTypeInfo newItem)
		{
			if (newItem != null)
			{
				StartingCreditsLabel.text = TextFormattingHelper.FormatCreditsWithDashForZero(newItem.StartingCredits, includeSuffix: true);
				StartingShipsLabel.text = string.Join(", ", newItem.SpawnUnits.Select((UniverseGameTypeSpawnUnit e) => e.UnitClass.GetClassAndSeriesName()).ToArray());
			}
			else
			{
				Text startingCreditsLabel = StartingCreditsLabel;
				string text = (StartingShipsLabel.text = string.Empty);
				startingCreditsLabel.text = text;
			}
		}
	}
}
