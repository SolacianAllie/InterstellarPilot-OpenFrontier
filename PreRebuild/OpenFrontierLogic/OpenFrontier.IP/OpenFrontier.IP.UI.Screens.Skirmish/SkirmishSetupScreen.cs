using System;
using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.CustomUnitVariants;
using OpenFrontier.IP.UI.Screens.Skirmish.SkirmishTeamSetup;
using OpenFrontier.Unity.Utils;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace OpenFrontier.IP.UI.Screens.Skirmish
{
	public class SkirmishSetupScreen : ScreenBase
	{
		public Button ManageVariantsButton;

		public Button RandomizeAllButton;

		public Button RandomizeSelectedTeamButton;

		public const int DefaultShipSelectionSetCount = 9;

		public Button CopyTeamButton;

		public Button PasteTeamButton;

		public Button PasteToAllButton;

		public Button ClearAllButton;

		public Button ClearSelectedTeamButton;

		public SkirmishTeam EditedTeam;

		public Button EditTeamButton;

		private int lastDefaultShipSelectionSet;

		public Button PlayButton;

		public Text SelectedTeamLabel;

		public SkirmishTeamShipLabelList SelectedTeamShipList;

		public Button SetDefaultButton;

		public Sector SpecificSceneToLoad;

		public SkirmishTeamList TeamList;

		public Toggle RandomShipComponentsToggle;

		public Toggle TeamColoursToggle;

		public Toggle IncludeCustomVariantsToggle;

		private SkirmishTeam copiedTeam;

		private List<CustomUnitVariant> loadedCustomVariants = new List<CustomUnitVariant>();

		private bool variantsInvalidated;

		public float MinRandomCombatRating = 1f;

		public float MaxRandomCombatRating = 40f;

		public float RandomCombatRatingPower = 1.5f;

		public float RandomTeamCountPower = 1.5f;

		public bool HasSelectedAnyShips => CountFriendlyShips() + CountHostileShips() > 0;

		public bool RandomShipComponents
		{
			get
			{
				return RandomShipComponentsToggle.isOn;
			}
			set
			{
				RandomShipComponentsToggle.isOn = value;
			}
		}

		public bool IncludeCustomVariants => IncludeCustomVariantsToggle.isOn;

		private int NumTeamsWithShips => TeamList.ActiveItems.Count((SkirmishTeam e) => e.Ships.Any());

		public static void PlayRandomSkirmish()
		{
			PlaySkirmish(GameController.Instance.GetInstantActionsShipSets().GetRandom().Teams, null);
		}

		public static void PlaySkirmish(IEnumerable<SkirmishTeamParams> teams, Sector scene, bool randomShipComponents = false)
		{
			PlaySkirmish(new SkirmishScenarioData
			{
				FullSaveGamePath = null,
				Teams = teams.ToList(),
				ScenarioInfo = GameController.Instance.SkirmishScenarioInfo,
				Sector = (scene ?? GameController.Instance.AllSectors.GetRandom()),
				RandomShipComponents = randomShipComponents,
				CustomSeed = UnityEngine.Random.Range(0, int.MaxValue)
			});
		}

		public static void PlaySkirmish(SkirmishScenarioData scenarioData)
		{
			GameController.Instance.ScenarioLoader.TryLoadScenario(scenarioData);
		}

		public void SetDefaultShipSelection()
		{
			SetDefaultShipSelection(0);
		}

		public SkirmishTeam GetTeamFromSkirmishParams(SkirmishTeamParams s, int teamIndex)
		{
			SkirmishTeam skirmishTeam = CreateSkirmishTeam(teamIndex);
			skirmishTeam.Ships = (from e in s.ShipItems
				where e != null
				select SkirmishShipItem.FromUnitClass(e.UnitClass)).ToList();
			return skirmishTeam;
		}

		public void SetTeamShips(int teamIndex, IEnumerable<SkirmishShipItem> ships)
		{
			TeamList.ActiveItems[teamIndex].Ships = ships.ToList();
			SetPlayerShipToFirstShip();
		}

		public List<SkirmishShipItem> GetTeamShips(int teamIndex)
		{
			return TeamList.ActiveItems[teamIndex].Ships;
		}

		public void Play()
		{
			SaveShipSelectionToPrefs();
			PlaySkirmish();
		}

		public void PlaySkirmish()
		{
			List<SkirmishTeamParams> teamParams = GetTeamParams();
			GameController.Instance.LaunchOrigin = GameController.EngineLaunchSource.Skirmish;
			for (int i = 0; i < teamParams.Count; i++)
			{
				if (TeamColoursToggle.isOn)
				{
					teamParams[i].TeamColor = GameController.Instance.GameSettings.SkirmishSettings.DefaultTeamColours[i];
				}
				else
				{
					teamParams[i].TeamColor = GameController.Instance.GameSettings.TeamColourSettings.DefaultTeamColour;
				}
			}
			PlaySkirmish(teamParams, SpecificSceneToLoad, RandomShipComponents);
		}

		public List<SkirmishTeamParams> GetTeamParams()
		{
			return TeamList.ActiveItems.Select((SkirmishTeam e) => new SkirmishTeamParams
			{
				ShipItems = e.Ships.Select((SkirmishShipItem f) => GetTeamsParamItem(f)).ToList()
			}).ToList();
		}

		public SkirmishTeamParamsItem GetTeamsParamItem(SkirmishShipItem skirmishShipItem)
		{
			return new SkirmishTeamParamsItem
			{
				CustomUnitVariant = skirmishShipItem.CustomVariant,
				UnitClass = skirmishShipItem.UnitClass
			};
		}

		public int CountFriendlyShips()
		{
			return (from e in TeamList.ActiveItems
				where e.TeamIndex == 0
				select e.Ships.Count).Sum();
		}

		public int CountHostileShips()
		{
			return TeamList.ActiveItems.Select((SkirmishTeam e) => e.Ships.Count).Sum() - CountFriendlyShips();
		}

		public UnitClass GetShipUnitClass(int teamIndex, int shipIndex)
		{
			if (teamIndex < TeamList.ActiveItems.Count)
			{
				SkirmishTeam skirmishTeam = TeamList.ActiveItems[teamIndex];
				if (shipIndex < skirmishTeam.Ships.Count)
				{
					return skirmishTeam.Ships[shipIndex].UnitClass;
				}
			}
			return null;
		}

		public string GetShipCustomVariantFullName(int teamIndex, int shipIndex)
		{
			if (teamIndex < TeamList.ActiveItems.Count)
			{
				SkirmishTeam skirmishTeam = TeamList.ActiveItems[teamIndex];
				if (shipIndex < skirmishTeam.Ships.Count && skirmishTeam.Ships[shipIndex].CustomVariant != null)
				{
					return skirmishTeam.Ships[shipIndex].CustomVariant.FullName;
				}
			}
			return null;
		}

		public void SaveShipSelectionToPrefs()
		{
			for (int i = 0; i < GameController.Instance.GameSettings.SkirmishSettings.MaxSkirmishTeams; i++)
			{
				for (int j = 0; j < GameController.Instance.GameSettings.SkirmishSettings.MaxSkirmishTeamShips; j++)
				{
					string shipKey = GetShipKey(i, j);
					UnitClass shipUnitClass = GetShipUnitClass(i, j);
					string arg = GetShipCustomVariantFullName(i, j) ?? string.Empty;
					PlayerPrefs.SetString(shipKey, (shipUnitClass != null) ? $"{shipUnitClass.UniqueID}_{arg}" : string.Empty);
				}
			}
			PlayerPrefs.Save();
		}

		public void LoadShipSelection()
		{
			TeamList.ClearActiveItems();
			for (int i = 0; i < GameController.Instance.GameSettings.SkirmishSettings.MaxSkirmishTeams; i++)
			{
				SkirmishTeam skirmishTeam = CreateSkirmishTeam(TeamList.ActiveItems.Count);
				for (int j = 0; j < GameController.Instance.GameSettings.SkirmishSettings.MaxSkirmishTeamShips; j++)
				{
					string text = PlayerPrefs.GetString(GetShipKey(i, j), "");
					if (string.IsNullOrWhiteSpace(text))
					{
						continue;
					}
					string[] array = text.Split("_");
					if (array.Length < 0)
					{
						continue;
					}
					string text2 = array[0];
					if (string.IsNullOrWhiteSpace(text2))
					{
						continue;
					}
					if (!int.TryParse(text2, out var classId))
					{
						continue;
					}
					UnitClass unitClass = GameController.Instance.LoadedUnitClasses.FirstOrDefault((UnitClass e) => e.UniqueID == classId);
					if (unitClass != null)
					{
						SkirmishShipItem skirmishShipItem = SkirmishShipItem.FromUnitClass(unitClass);
						if (array.Length > 1)
						{
							string customVariantFullName = array[1];
							if (!string.IsNullOrWhiteSpace(customVariantFullName))
							{
								CustomUnitVariant customUnitVariant = loadedCustomVariants.FirstOrDefault((CustomUnitVariant e) => e.FullName == customVariantFullName);
								if (customUnitVariant != null)
								{
									skirmishShipItem.CustomVariant = customUnitVariant;
								}
							}
						}
						skirmishTeam.Ships.Add(skirmishShipItem);
					}
					else
					{
						Debug.LogWarningFormat(this, "PlayerPrefs references unit with id \"{0}\" which is unknown", classId);
					}
				}
				TeamList.Add(skirmishTeam);
			}
			FillTeamsToCapacity();
			SetPlayerShipToFirstShip();
		}

		private void FillTeamsToCapacity()
		{
			int num = 0;
			while (TeamList.ActiveItems.Count < GameController.Instance.GameSettings.SkirmishSettings.MaxSkirmishTeams && num < 20)
			{
				SkirmishTeam item = CreateSkirmishTeam(TeamList.ActiveItems.Count);
				TeamList.Add(item);
				num++;
			}
		}

		public void SetPlayerShipToFirstShip()
		{
			foreach (SkirmishTeam activeItem in TeamList.ActiveItems)
			{
				foreach (SkirmishShipItem ship in activeItem.Ships)
				{
					ship.IsPlayer = false;
				}
			}
			if (TeamList.ActiveItems.Count > 0 && TeamList.ActiveItems[0].Ships.Count > 0)
			{
				Debug.Log("Setting player ship to true");
				TeamList.ActiveItems[0].Ships[0].IsPlayer = true;
			}
		}

		protected override void awake()
		{
			base.awake();
			SetDefaultButton.onClick.AddListener(SetDefaults);
			EditTeamButton.onClick.AddListener(EditTeam);
			ClearAllButton.onClick.AddListener(ClearAllTeams);
			ClearSelectedTeamButton.onClick.AddListener(ClearSelectedTeamClick);
			RandomShipComponentsToggle.isOn = RandomShipComponents;
			CopyTeamButton.onClick.AddListener(CopyTeam);
			PasteTeamButton.onClick.AddListener(PasteTeam);
			PasteToAllButton.onClick.AddListener(PasteTeamToAll);
			RandomizeAllButton.onClick.AddListener(RandomizeAllButtonClick);
			RandomizeSelectedTeamButton.onClick.AddListener(RandomizeSelectedTeamButtonClick);
			ManageVariantsButton.onClick.AddListener(() =>
			{
				UIController.Instance.ScreenNavigator.ShowManageCustomUnitVariantsScreen();
				variantsInvalidated = true;
			});
		}

		protected override void start()
		{
			base.start();
			TryLoadCustomShipVariants();
			PlayButton.onClick.AddListener(TryPlay);
			PopulateInitialTeams();
			Refresh();
			TeamList.SelectedItemChanged += TeamList_SelectedItemChanged;
		}

		protected override void onMadeCurrentPanel(bool navigatedForward)
		{
			base.onMadeCurrentPanel(navigatedForward);
			if (!navigatedForward && variantsInvalidated)
			{
				TryLoadCustomShipVariants();
			}
		}

		private void TryLoadCustomShipVariants()
		{
			try
			{
				loadedCustomVariants.Clear();
				loadedCustomVariants.AddRange(CustomUnitVariantIO.LoadCustomVariantShips());
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}

		protected override bool onNavigatingBack()
		{
			SaveShipSelectionToPrefs();
			return base.onNavigatingBack();
		}

		protected override void update()
		{
			base.update();
			PlayButton.interactable = ShouldNextButtonBeEnabled();
			CopyTeamButton.gameObject.SetActive(TeamList.FirstSelectedItem != null && TeamList.FirstSelectedItem.Ships.Count > 0);
			PasteTeamButton.gameObject.SetActive(copiedTeam != null && copiedTeam != TeamList.FirstSelectedItem && copiedTeam.Ships.Count > 0);
			PasteToAllButton.gameObject.SetActive(copiedTeam != null && copiedTeam.Ships.Count > 0);
		}

		protected override void onDestroy()
		{
			base.onDestroy();
			SaveShipSelectionToPrefs();
		}

		protected override void refresh()
		{
			base.refresh();
			TeamList.Refresh();
			SelectedTeamShipList.Refresh();
			RefreshSelectedTeamShipItems();
		}

		private void ClearSelectedTeamClick()
		{
			if (TeamList.FirstSelectedItem != null)
			{
				TeamList.FirstSelectedItem.Ships.Clear();
				Refresh();
			}
		}

		private void ClearAllTeams()
		{
			ClearAllTeamsNoRefresh();
			Refresh();
		}

		private void ClearAllTeamsNoRefresh()
		{
			TeamList.ClearActiveItems();
			for (int i = 0; i < GameController.Instance.GameSettings.SkirmishSettings.MaxSkirmishTeams; i++)
			{
				TeamList.Add(CreateSkirmishTeam(i));
			}
			SetPlayerShipToFirstShip();
		}

		private void TeamList_SelectedItemChanged(ScrollList<SkirmishTeam> sender, SkirmishTeam oldItem, SkirmishTeam newItem)
		{
			RefreshSelectedTeamShipItems();
		}

		private void PopulateInitialTeams()
		{
			if (PlayerPrefs.GetInt("visited_skirmish_setup") == 0)
			{
				SetDefaultShipSelection();
				PlayerPrefs.SetInt("visited_skirmish_setup", 1);
				PlayerPrefs.Save();
			}
			else
			{
				LoadShipSelection();
				if (!HasSelectedAnyShips)
				{
					SetDefaultShipSelection(0);
				}
			}
			SetPlayerShipToFirstShip();
		}

		private void EditTeam()
		{
			EditedTeam = TeamList.FirstSelectedItem;
			UIController.Instance.ScreenNavigator.ShowSkirmishTeamSetupScreen(EditedTeam, (SkirmishTeamSetupScreen screen) =>
			{
				screen.SkirmishSetupUI = this;
				screen.LoadedCustomUnitVariants = loadedCustomVariants;
				screen.IncludeCustomVariants = IncludeCustomVariants;
			});
		}

		private void CopyTeam()
		{
			copiedTeam = TeamList.FirstSelectedItem;
		}

		private void PasteTeam()
		{
			if (TeamList.FirstSelectedItem != null && copiedTeam != null)
			{
				TeamList.FirstSelectedItem.Ships.Clear();
				TeamList.FirstSelectedItem.Ships.AddRange(copiedTeam.Ships);
				Refresh();
			}
		}

		private void PasteTeamToAll()
		{
			foreach (SkirmishTeam activeItem in TeamList.ActiveItems)
			{
				if (activeItem != copiedTeam)
				{
					activeItem.Ships.Clear();
					activeItem.Ships.AddRange(copiedTeam.Ships);
				}
			}
			Refresh();
		}

		private void SetDefaults()
		{
			lastDefaultShipSelectionSet = Maths.WrapValue(lastDefaultShipSelectionSet + 1, 0, 9);
			SetDefaultShipSelection(lastDefaultShipSelectionSet);
			SetPlayerShipToFirstShip();
			Refresh();
		}

		private void SetDefaultShipSelection(int index)
		{
			SkirmishScenarioParams s = GameController.Instance.GetInstantActionsShipSets().GetRandom();
			IEnumerable<SkirmishTeam> items = s.Teams.Select((SkirmishTeamParams e) => GetTeamFromSkirmishParams(e, s.Teams.IndexOf(e)));
			TeamList.SetItems(items);
			FillTeamsToCapacity();
		}

		private void TryPlay()
		{
			if (ShouldNextButtonBeEnabled())
			{
				Play();
			}
		}

		private void RefreshSelectedTeamShipItems()
		{
			SelectedTeamLabel.gameObject.SetActive(TeamList.FirstSelectedItem != null);
			if (TeamList.FirstSelectedItem != null)
			{
				SelectedTeamLabel.text = TeamList.FirstSelectedItem.TeamName;
				SelectedTeamShipList.SetItems(TeamList.FirstSelectedItem.Ships.Select((SkirmishShipItem e) => e.Clone()));
			}
			else
			{
				SelectedTeamShipList.SetItems(null);
			}
		}

		private bool ShouldNextButtonBeEnabled()
		{
			if (CountFriendlyShips() > 0)
			{
				return CountHostileShips() > 0;
			}
			return false;
		}

		private SkirmishTeam CreateSkirmishTeam(int teamIndex = 0)
		{
			return new SkirmishTeam
			{
				TeamName = "Team " + (teamIndex + 1),
				TeamIndex = teamIndex
			};
		}

		private string GetShipKey(int teamIndex, int shipIndex)
		{
			return $"skirmish_team{teamIndex.ToString().PadLeft(2, '0')}_{shipIndex.ToString().PadLeft(2, '0')}";
		}

		private void RandomizeAllButtonClick()
		{
			ClearAllTeams();
			float combatRating = UnityEngine.Random.Range(MinRandomCombatRating, MaxRandomCombatRating);
			int num = Maths.RandomIntWithPower(2, GameController.Instance.GameSettings.SkirmishSettings.MaxSkirmishTeams, RandomTeamCountPower);
			List<SkirmishTeamParamsItem> availableShips = SkirmishHelper.GetAvailableShips(IncludeCustomVariants, loadedCustomVariants).ToList();
			for (int i = 0; i < num; i++)
			{
				List<SkirmishShipItem> teamShips = GetTeamShips(i);
				RandomizeTeam(teamShips, availableShips, combatRating);
			}
			SetPlayerShipToFirstShip();
			Refresh();
		}

		private void RandomizeSelectedTeamButtonClick()
		{
			float combatRating = Maths.RandomFloatWithPower(MinRandomCombatRating, MaxRandomCombatRating, RandomCombatRatingPower);
			if (NumTeamsWithShips > 1)
			{
				combatRating = (from e in TeamList.ActiveItems
					where e.Ships.Any()
					select e.Ships.Sum((SkirmishShipItem ships) => ships.GetCombatRating())).Average();
			}
			TeamList.FirstSelectedItem.Ships.Clear();
			List<SkirmishTeamParamsItem> availableShips = SkirmishHelper.GetAvailableShips(IncludeCustomVariants, loadedCustomVariants).ToList();
			RandomizeTeam(TeamList.FirstSelectedItem.Ships, availableShips, combatRating);
			SetPlayerShipToFirstShip();
			Refresh();
		}

		private void RandomizeTeam(List<SkirmishShipItem> skirmishShipItems, List<SkirmishTeamParamsItem> availableShips, float combatRating)
		{
			skirmishShipItems.Clear();
			List<SkirmishTeamParamsItem> list = new List<SkirmishTeamParamsItem>(8);
			float num = availableShips.Min((SkirmishTeamParamsItem e) => e.GetCombatRating());
			float remainingCombatRating = combatRating;
			while (list.Count < GameController.Instance.GameSettings.SkirmishSettings.MaxSkirmishTeamShips && remainingCombatRating > num * 1.05f)
			{
				SkirmishTeamParamsItem random = availableShips.Where((SkirmishTeamParamsItem e) => e.GetCombatRating() <= remainingCombatRating).GetRandom();
				if (random == null)
				{
					break;
				}
				list.Add(random);
				remainingCombatRating -= random.GetCombatRating();
			}
			skirmishShipItems.AddRange(from e in list
				orderby e.GetCombatRating() descending
				select SkirmishShipItem.FromSkirmishTeamParamsItem(e));
		}
	}
}
