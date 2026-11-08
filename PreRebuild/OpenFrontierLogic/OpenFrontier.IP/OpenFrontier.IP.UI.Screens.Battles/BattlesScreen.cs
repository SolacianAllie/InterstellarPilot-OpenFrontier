using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenFrontier.IP.Engine;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.Battles
{
	public class BattlesScreen : ScenarioUI
	{
		private static StringBuilder stringBuilder = new StringBuilder();

		public GameObject FriendlyShipsContainer;

		public Text FriendlyShipsLabel;

		public GameObject HostileShipsContainer;

		public Text HostileShipsLabel;

		public GameObject ShipIconPrefab;

		protected override void awake()
		{
			base.awake();
		}

		protected override ScenarioLoadData GetLoadData(ScenarioInfo scenario)
		{
			SkirmishScenarioParams component = scenario.GetComponent<SkirmishScenarioParams>();
			return new SkirmishScenarioData
			{
				ScenarioInfo = scenario,
				FullSaveGamePath = null,
				Teams = component.Teams.ToList()
			};
		}

		protected override void OnSelectedItemChanged(ScenarioInfo oldItem)
		{
			base.OnSelectedItemChanged(oldItem);
			if (SelectedScenario != null)
			{
				SkirmishScenarioParams component = SelectedScenario.GetComponent<SkirmishScenarioParams>();
				UnityObjectHelper.DestroyChildren(FriendlyShipsContainer, destroyImmediate: true);
				UnityObjectHelper.DestroyChildren(HostileShipsContainer, destroyImmediate: true);
				PopulateShip(component.Teams[0].ShipItems[0], 1, FriendlyShipsContainer, isPlayer: true);
				PopulateShips(FriendlyShipsContainer, component.Teams[0].ShipItems.Skip(1));
				PopulateShips(HostileShipsContainer, component.Teams[1].ShipItems);
			}
		}

		private void PopulateShips(GameObject container, IEnumerable<SkirmishTeamParamsItem> items)
		{
			foreach (IGrouping<UnitClass, SkirmishTeamParamsItem> item in from e in items
				group e by e.UnitClass into e
				orderby e.Key.CombatRating
				select e)
			{
				PopulateShip(item.First(), item.Count(), container, isPlayer: false);
			}
		}

		private void PopulateShip(SkirmishTeamParamsItem item, int count, GameObject container, bool isPlayer)
		{
			GameObject gameObject = UnityObjectHelper.NewGameObject(ShipIconPrefab, container.transform);
			gameObject.transform.localScale = Vector3.one;
			Text componentInChildren = gameObject.GetComponentInChildren<Text>();
			componentInChildren.text = $"{count}x {item.UnitClass.GetClassAndSeriesName()}";
			gameObject.GetComponentInChildren<Image>().sprite = EngineASX.Instance.EngineResources.GetUnitClassIconSpriteOrDefault(item.UnitClass);
			if (isPlayer)
			{
				componentInChildren.text += " (P1)";
			}
		}

		private string GetShipsStr(UnitClass[] ships, int startIndex)
		{
			stringBuilder.Length = 0;
			UnitClassListFormatter.FormatList(stringBuilder, ships, startIndex);
			return stringBuilder.ToString();
		}
	}
}
