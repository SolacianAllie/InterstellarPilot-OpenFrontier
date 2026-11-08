using System;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.FactionTransactions
{
	public class FactionTransactionsItem : ScrollListItem<FactionTransaction>
	{
		public Text ValueText;

		public Text BalanceText;

		public TextMeshProUGUI DescriptionText;

		public Text LocationText;

		public Text FactionText;

		public override void Refresh()
		{
			base.Refresh();
			ValueText.text = TextFormattingHelper.FormatCredits(Item.Value);
			ValueText.color = GetItemColor(Item.Value, EngineASX.Instance);
			BalanceText.text = TextFormattingHelper.FormatCredits(Item.CurrentBalance);
			LocationText.text = ((Item.Location != null) ? Item.Location.GetFriendlyName() : "-");
			DescriptionText.text = GetTransactionDescription(Item, Item.Value < 0);
			FactionText.text = ((Item.OtherFaction != null) ? Item.OtherFaction.ShortName : "-");
		}

		public Color GetItemColor(int value, EngineASX engine)
		{
			if (value < 0)
			{
				return engine.CreditsDownColor;
			}
			if (value > 0)
			{
				return engine.CreditsUpColor;
			}
			return engine.CreditsNeutralColor;
		}

		public string GetTransactionDescription(FactionTransaction transaction, bool isPurchase)
		{
			switch (transaction.TransactionType)
			{
			case FactionTransactionType.FleetTransfer:
				return "Fleet transfer";
			case FactionTransactionType.EquipmentPurchase:
				if (!isPurchase)
				{
					return "Equipment Sale";
				}
				return "Equipment Purchase";
			case FactionTransactionType.MiscPurchase:
				return "Miscellaneous";
			case FactionTransactionType.Mission:
				return "Mission Reward";
			case FactionTransactionType.PassengerFare:
				return GetTransactionCountText(transaction) + "Fares";
			case FactionTransactionType.ShipPurchase:
				if (transaction.RelatedUnitClass != null)
				{
					return GetTransactionCountText(transaction) + transaction.RelatedUnitClass.GetClassAndSeriesName() + " Purchase";
				}
				if (!isPurchase)
				{
					return "Ship Sale";
				}
				return "Ship Purchase";
			case FactionTransactionType.ShipRepairs:
				return "Repairs";
			case FactionTransactionType.StationBuild:
				if (transaction.RelatedUnitClass != null)
				{
					return "Build " + transaction.RelatedUnitClass.GetClassAndSeriesName(shortName: true);
				}
				return "Build Station";
			case FactionTransactionType.Trade:
			{
				string text = (isPurchase ? "Purchase" : "Sale");
				string transactionCountText = GetTransactionCountText(transaction.RelatedCount.HasValue ? new int?(Mathf.Abs(transaction.RelatedCount.Value)) : ((int?)null));
				if (transaction.RelatedCargoClass != null)
				{
					return transactionCountText + transaction.RelatedCargoClass.ShortNameIfAssigned + " " + text;
				}
				return "Trade " + text;
			}
			case FactionTransactionType.Scratchcard:
				return "Scratchcard";
			case FactionTransactionType.Tax:
				switch (transaction.TaxType)
				{
				case FactionTransactionTaxType.PassengerFare:
					return "Tax on " + GetTransactionCountText(transaction) + "passenger fares";
				case FactionTransactionTaxType.ShipSale:
					if (transaction.RelatedUnitClass != null)
					{
						return "Tax on sale of " + GetTransactionCountText(transaction) + transaction.RelatedUnitClass.GetClassAndSeriesName(shortName: true);
					}
					return "Tax on ship sale";
				case FactionTransactionTaxType.CargoSale:
					if (transaction.RelatedCargoClass != null)
					{
						return "Tax on sale of " + GetTransactionCountText(transaction) + transaction.RelatedCargoClass.ClassName;
					}
					return "Tax on goods";
				default:
					return "Tax";
				}
			case FactionTransactionType.SectorIncome:
				return "Sector income";
			case FactionTransactionType.Unknown:
			case FactionTransactionType.Gift:
			case FactionTransactionType.Salvage:
			case FactionTransactionType.Bounty:
			case FactionTransactionType.Tribute:
				return Enum.GetName(typeof(FactionTransactionType), transaction.TransactionType);
			case FactionTransactionType.UnitDismantled:
				if (transaction.RelatedUnitClass != null)
				{
					return "Dismantled " + transaction.RelatedUnitClass.GetClassAndSeriesName();
				}
				return "Dismantled unit";
			default:
				return "Unknown";
			}
		}

		private static string GetTransactionCountText(FactionTransaction transaction)
		{
			return GetTransactionCountText(transaction.RelatedCount);
		}

		private static string GetTransactionCountText(int? count)
		{
			if (!count.HasValue)
			{
				return string.Empty;
			}
			return count.Value.ToString("N0") + "x ";
		}
	}
}
