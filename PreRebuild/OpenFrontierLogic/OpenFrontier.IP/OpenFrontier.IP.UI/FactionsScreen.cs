using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Comms;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.UI.Controls;
using OpenFrontier.IP.UI.Screens.Factions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class FactionsScreen : EngineScreen
	{
		public int MaxFactionsInFriendsList = 15;

		public Button CommsButton;

		public GameObject BountyPlacedOnPlayerRoot;

		public Text BountyPlacedOnPlayerLabel;

		public Text BountyPlacedOnFactionLabel;

		public FactionsScreenSorter Sorter;

		public Toggle ShowMinorFactionsToggle;

		public Toggle ShowBanditsToggle;

		public Toggle ShowFreelancersToggle;

		public TextMeshProUGUI FriendsLabel;

		public TextMeshProUGUI EnemiesLabel;

		public Text FactionInfoDescriptionLabel;

		public Text FactionWealthLabel;

		public Text FactionPowerLabel;

		public Text FactionReputationLabel;

		public Text FactionInfoNameLabel;

		public Text FactionTypeLabel;

		public Text FactionHomeSectorLabel;

		public Text FactionLeaderLabel;

		public GameObject FactionReputationRoot;

		public GameObject FactionTypeRoot;

		public GameObject FactionInfoRoot;

		public GameObject FactionHomeSectorRoot;

		public FactionList ItemList;

		public float MinOpinionBarScale = 6f;

		public float EnemiesOpinionTheshold = -0.25f;

		public float FriendsOpinionTheshold = 0.25f;

		public string FactionsTextListWarText = "<sprite index= 0>";

		public string FactionsTextListAlliedText = "(Allied)";

		public bool ShowBandits
		{
			get
			{
				return ShowBanditsToggle.isOn;
			}
			set
			{
				ShowBanditsToggle.isOn = value;
			}
		}

		public bool ShowFreelancers
		{
			get
			{
				return ShowFreelancersToggle.isOn;
			}
			set
			{
				ShowFreelancersToggle.isOn = value;
			}
		}

		public bool ShowMinorFactions
		{
			get
			{
				return ShowMinorFactionsToggle.isOn;
			}
			set
			{
				ShowMinorFactionsToggle.isOn = value;
			}
		}

		public Faction CurrentItem => ItemList.FirstSelectedItem;

		public FactionsSortMode SortMode
		{
			get
			{
				return Sorter.SortMode;
			}
			set
			{
				Sorter.SetSortMode((int)value);
			}
		}

		protected override void awake()
		{
			base.awake();
			ItemList.SelectedItemChanged += ItemList_SelectedItemChanged;
			Sorter.Applying += Sorter_Applying;
		}

		private void Sorter_Applying(ListSorter sender)
		{
			Refresh();
		}

		protected override void start()
		{
			base.start();
			CommsButton.onClick.AddListener(CommsButtonClick);
		}

		private void CommsButtonClick()
		{
			ICommsHandler handler = null;
			if (CanOpenCommsWithFaction(CurrentItem, out handler))
			{
				CommsHelper.OpenCommsWithHandler(handler, EngineASX.Instance.LocalFaction);
			}
		}

		public void Init()
		{
			ShowMinorFactionsToggle.onValueChanged.AddListener(OnFilterChanged);
			ShowFreelancersToggle.onValueChanged.AddListener(OnFilterChanged);
			ShowBanditsToggle.onValueChanged.AddListener(OnFilterChanged);
		}

		private void OnFilterChanged(bool value)
		{
			Refresh();
			ItemList.ResetScrollPosition();
		}

		protected override void refresh()
		{
			base.refresh();
			RefreshItemList();
			RefreshCurrentItemData();
		}

		private void RefreshTotalFactionBounty()
		{
			int totalBountyPlacedOnFaction = BountyHelper.GetTotalBountyPlacedOnFaction(CurrentItem);
			BountyPlacedOnFactionLabel.text = TextFormattingHelper.FormatCreditsWithDashForZero(totalBountyPlacedOnFaction, includeSuffix: true);
		}

		private void RefreshBountyPlacedOnPlayer()
		{
			bool flag = CurrentItem != null && ShouldShowBountyPlacedOnPlayerFaction(CurrentItem);
			BountyPlacedOnPlayerRoot.gameObject.SetActive(flag);
			if (flag)
			{
				int totalBountyPlacedOnFactionByFaction = BountyHelper.GetTotalBountyPlacedOnFactionByFaction(Eng.LocalFaction, CurrentItem);
				BountyPlacedOnPlayerLabel.text = TextFormattingHelper.FormatCredits(totalBountyPlacedOnFactionByFaction, includeSuffix: true);
			}
		}

		private void RefreshItemList()
		{
			List<Faction> list = Eng.Factions.Where((Faction e) => ShouldShowFactionInList(e)).ToList();
			list.Sort(Sorter);
			ItemList.SetItems(list);
		}

		private bool ShouldShowFactionInList(Faction faction)
		{
			if (faction.IsPlayerFaction)
			{
				return true;
			}
			if (!Eng.LocalPlayer.Faction.HasAttitudeToFaction(faction))
			{
				return false;
			}
			if (faction.IsFreelancer)
			{
				return ShowFreelancers;
			}
			if (faction.FactionType == FactionType.Bandit)
			{
				return ShowBandits;
			}
			if (faction.IsMinor)
			{
				return ShowMinorFactions;
			}
			return true;
		}

		private bool ShouldShowFactionInEnemiesOrAlliesList(Faction faction, Faction otherFaction, bool includeLocalFaction)
		{
			if (otherFaction.ShortName == "Bandits")
			{
				return false;
			}
			if (otherFaction.GetNeutralityWith(faction) == Neutrality.Allied)
			{
				return true;
			}
			if (includeLocalFaction || !otherFaction.IsPlayerFaction)
			{
				if (otherFaction.IsFreelancer)
				{
					return !otherFaction.IsMinor;
				}
				return true;
			}
			return false;
		}

		private void ItemList_SelectedItemChanged(ScrollList<Faction> sender, Faction oldItem, Faction newItem)
		{
			RefreshCurrentItemData();
		}

		private void RefreshCurrentItemData()
		{
			FactionInfoRoot.SetActive(CurrentItem != null);
			if (CurrentItem != null)
			{
				RefreshTotalFactionBounty();
				RefreshBountyPlacedOnPlayer();
				RefreshCommsButtonEnabled();
				RefreshFriendsLabel();
				RefreshEnemiesLabel();
				bool playedBySomebody = SomebodyElseIsPlayingToo()
					&& CurrentItem.IsAPlayersFaction;

				FactionReputationRoot.SetActive(value: true);
				FactionTypeRoot.SetActive(!playedBySomebody);
				FactionHomeSectorRoot.SetActive(!playedBySomebody);
				FactionInfoNameLabel.text = CurrentItem.Name;
				FactionInfoDescriptionLabel.text = CurrentItem.Description;

				// A number for a faction a person is playing, a description for
				// one nobody is. And the number is the point: "Destitute" is a
				// word, and a word cannot be wrong in a way you can see, so two
				// copies of a faction that both said Destitute while holding
				//// different amounts of money looked identical.
				//
				// Worth knowing when reading it: this is one-sided. The host
				// pushes each player their OWN faction's credits, so the host
				// sees a live number for the joiner and the joiner sees
				// whatever the world snapshot carried for the host. That
				// asymmetry is the display working, not failing.
				FactionWealthLabel.text = playedBySomebody
					? TextFormattingHelper.FormatCredits(
						CurrentItem.Credits, includeSuffix: true)
					: CurrentItem.GetWealthLevelDescription();
				FactionPowerLabel.text = CurrentItem.GetPowerLevelDescription();
				FactionReputationLabel.text = CurrentItem.GetReputationDescription();
				FactionHomeSectorLabel.text = CurrentItem.GetHomeSectorNameForPlayer();
				FactionLeaderLabel.text = ((CurrentItem.LeaderPerson != null) ? CurrentItem.LeaderPerson.GetNameAndTitleOrFullRank(shortName: false) : "-");
				FactionTypeLabel.text = CurrentItem.GetFactionTypeDescription();
			}
		}

		/// <summary>Whether this world has more than one player in it.</summary>
		/// <remarks>
		/// The second half of a condition that has to have two halves.
		/// IsAPlayersFaction is true for your own faction in a singleplayer
		/// game - it falls back to "is this mine" - so testing that alone would
		/// change the panel for somebody who never opens the multiplayer menu,
		/// which is the one thing this whole project is not allowed to do.
		///
		/// Asking whether there is anybody else here first makes singleplayer
		/// unchanged by construction rather than by remembering to check.
		/// </remarks>
		private bool SomebodyElseIsPlayingToo()
		{
			EngineASX engine = EngineASX.Instance;

			if (engine == null || engine.Factions == null)
			{
				return false;
			}

			int playerFactions = 0;

			for (int i = 0; i < engine.Factions.Count; i++)
			{
				if (engine.Factions[i] != null && engine.Factions[i].IsAPlayersFaction)
				{
					playerFactions++;

					if (playerFactions > 1)
					{
						return true;
					}
				}
			}

			return false;
		}

		private string GetFactionList(List<Faction> factions)
		{
			if (factions.Count() == 0)
			{
				return "None";
			}
			IEnumerable<Faction> source = factions.Take(MaxFactionsInFriendsList);
			string text = string.Join(", ", source.Select((Faction e) => GetColoredFactionListItemName(CurrentItem, e)).ToArray());
			if (source.Count() < factions.Count)
			{
				text += $" + {factions.Count - source.Count()} others";
			}
			return text;
		}

		private string GetColoredFactionListItemName(Faction currentFaction, Faction otherFaction)
		{
			string factionListItemName = GetFactionListItemName(currentFaction, otherFaction);
			Color factionHostilityColor = EngineASX.Instance.GetFactionHostilityColor(otherFaction, EngineASX.Instance.LocalFaction);
			return UnityRichTextHelper.Color(factionListItemName, factionHostilityColor);
		}

		private string GetFactionListItemName(Faction currentFaction, Faction otherFaction)
		{
			if (currentFaction.IsHostileToOrAlwaysHostileTo(otherFaction))
			{
				return otherFaction.GetShortNameElseLong() + " " + FactionsTextListWarText;
			}
			if (currentFaction.IsAlliedTo(otherFaction))
			{
				return otherFaction.GetShortNameElseLong() + " " + FactionsTextListAlliedText;
			}
			return otherFaction.GetShortNameElseLong();
		}

		protected override void update()
		{
			base.update();
			RefreshBountyPlacedOnPlayer();
			RefreshCommsButtonEnabled();
		}

		private void RefreshCommsButtonEnabled()
		{
			ICommsHandler handler = null;
			CommsButton.interactable = CurrentItem != null && CanOpenCommsWithFaction(CurrentItem, out handler);
		}

		private bool CanOpenCommsWithFaction(Faction currentItem, out ICommsHandler handler)
		{
			handler = null;
			if (!currentItem.IsPlayerFaction && currentItem.LeaderPerson != null)
			{
				return CommsHelper.TryGetCommsHandlerFromPerson(currentItem.LeaderPerson, out handler);
			}
			return false;
		}

		private bool ShouldShowBountyPlacedOnPlayerFaction(Faction selectedFaction)
		{
			if (selectedFaction != null && Eng.LocalFaction != null && selectedFaction != Eng.LocalFaction)
			{
				return BountyHelper.GetTotalBountyPlacedOnFactionByFaction(Eng.LocalFaction, selectedFaction) > 0;
			}
			return false;
		}

		private void RefreshEnemiesLabel()
		{
			TextMeshProUGUI enemiesLabel = EnemiesLabel;
			if (CurrentItem != null)
			{
				List<Faction> factions = (from e in CurrentItem.GetKnownFactionsWithOpinionLessThan(EnemiesOpinionTheshold)
					where ShouldShowFactionInEnemiesOrAlliesList(CurrentItem, e, includeLocalFaction: true)
					orderby CurrentItem.IsAlliedTo(e) ? 1 : 0 descending, e.GetCachedNetWorth()
					select e).ToList();
				enemiesLabel.text = GetFactionList(factions);
			}
			else
			{
				enemiesLabel.text = "-";
			}
		}

		private void RefreshFriendsLabel()
		{
			TextMeshProUGUI friendsLabel = FriendsLabel;
			if (CurrentItem != null)
			{
				List<Faction> factions = (from e in CurrentItem.GetKnownFactionsWithOpinionGreaterThan(FriendsOpinionTheshold)
					where ShouldShowFactionInEnemiesOrAlliesList(CurrentItem, e, includeLocalFaction: true)
					orderby CurrentItem.IsHostileToOrAlwaysHostileTo(e) ? 1 : 0 descending, e.GetCachedNetWorth()
					select e).ToList();
				friendsLabel.text = GetFactionList(factions);
			}
			else
			{
				friendsLabel.text = "-";
			}
		}
	}
}
