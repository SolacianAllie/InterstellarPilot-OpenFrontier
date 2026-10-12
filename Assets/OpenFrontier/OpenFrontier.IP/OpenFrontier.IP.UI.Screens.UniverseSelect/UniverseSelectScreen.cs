using System.Collections.Generic;
using OpenFrontier.IP.Engine;
using OpenFrontier.Unity.Utils;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.UniverseSelect
{
	public class UniverseSelectScreen : ScreenBase
	{
		public UniverseSelectList ScenarioList;

		public Button NextButton;

		public List<ScenarioInfo> AvailableScenarios = new List<ScenarioInfo>();

		public void Next()
		{
			// Open Frontier: the alpha gate ("temporarily unavailable"
			// for anything but Unchartered Space) is removed - every
			// scenario in AvailableScenarios is playable, including
			// A Wormhole Too Far.
			if (ScenarioList.FirstSelectedItem != null)
			{
				UIController.Instance.ScreenNavigator.ShowUniverseGameTypeScreen(ScenarioList.FirstSelectedItem);
			}
		}

		protected override void awake()
		{
			base.awake();
			NextButton.onClick.AddListener(Next);
			ScenarioList.SelectedItemChanged += GameTypeList_SelectedItemChanged;
		}

		protected override void start()
		{
			base.start();
			AvailableScenarios.TrimNulls();
			ScenarioList.SetItems(AvailableScenarios);
		}

		private void GameTypeList_SelectedItemChanged(ScrollList<ScenarioInfo> sender, ScenarioInfo oldItem, ScenarioInfo newItem)
		{
		}
	}
}
