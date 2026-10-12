using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Intel;
using OpenFrontier.Unity.Utils;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.BuySectorIntel
{
	public class BuySectorIntelScreen : EngineScreen
	{
		public Button BuyButton;

		public Text PromptText;

		public Unit CurrentLocation;

		public GamePlayer BuyingPlayer;

		private int cachedCost;

		private List<Unit> undiscoveredUnits = new List<Unit>();

		public int GetTotalCost()
		{
			undiscoveredUnits = (from e in BuyingPlayer.Faction.Intel.GetUndiscoveredUnitsInScene(CurrentLocation.Sector)
				where CanPlayerBuySectorIntelOnUnit(e)
				select e).ToList();
			return IntelHelper.GetCostToDiscoverUnits(undiscoveredUnits, BuyingPlayer.Faction);
		}

		public static bool CanPlayerBuySectorIntelOnUnit(Unit unit)
		{
			if (unit.IsDiscoverableType && unit.UnitType != UnitType.Ship && unit.UnitType != UnitType.Cargo && unit.UnitClass.StationPurpose != StationPurpose.Satellite)
			{
				return unit.UnitClass.StationPurpose != StationPurpose.Defence;
			}
			return false;
		}

		public void Buy()
		{
			int num = cachedCost;
			Eng.RegisterTaxedPlayerTrade(CurrentLocation.GetRootUnit(), -num, FactionTransactionType.MiscPurchase);
			undiscoveredUnits.TrimNulls();
			foreach (Unit undiscoveredUnit in undiscoveredUnits)
			{
				BuyingPlayer.Faction.Intel.DiscoverUnit(undiscoveredUnit);
			}
			IntelHelper.AddIntelPurchasedQuickMessage();
			ScreenNavigator.Instance.ShowBuySectorIntelConfirmScreen(undiscoveredUnits);
		}

		protected override void awake()
		{
			base.awake();
			BuyButton.onClick.AddListener(TryBuy);
		}

		protected override void update()
		{
			base.update();
			if (BuyButton != null)
			{
				BuyButton.interactable = BuyingPlayer != null && undiscoveredUnits.Count > 0 && CanAffordPurchase();
			}
		}

		protected override void refresh()
		{
			base.refresh();
			if (CurrentLocation != null && BuyingPlayer != null)
			{
				cachedCost = GetTotalCost();
				RefreshPrompt();
			}
		}

		private void TryBuy()
		{
			if (CanPurchase())
			{
				if (CanAffordPurchase())
				{
					Buy();
				}
				else
				{
					UIController.Instance.ShowInsufficientCreditsMessageBox();
				}
			}
		}

		private string GetPromptText()
		{
			if (cachedCost > 0)
			{
				string text = $"A traveller is offering to add the locations of stations in this sector to your database for {TextFormattingHelper.FormatCredits(cachedCost)} credits";
				if (CanAffordPurchase())
				{
					return text + "\n\nWould you like to buy this?";
				}
				return text + "\n\nYou cannot afford this";
			}
			return "No intel is available at this time.";
		}

		private void RefreshPrompt()
		{
			PromptText.text = GetPromptText();
		}

		private bool CanAffordPurchase()
		{
			return BuyingPlayer.Faction.Credits >= cachedCost;
		}

		private bool CanPurchase()
		{
			return cachedCost > 0;
		}
	}
}
