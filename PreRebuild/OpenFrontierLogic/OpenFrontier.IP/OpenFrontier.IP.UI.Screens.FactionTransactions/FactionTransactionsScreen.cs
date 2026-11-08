using System.Collections.Generic;
using OpenFrontier.IP.Engine.Factions;

namespace OpenFrontier.IP.UI.Screens.FactionTransactions
{
	public class FactionTransactionsScreen : EngineScreen
	{
		private float lastTransactionTime;

		public FactionTransactionsItemList TransactionsItemList;

		public UnitContextButton UnitContextButton;

		public Faction Faction { get; set; }

		protected override void awake()
		{
			base.awake();
			TransactionsItemList.SelectedItemChanged += TransactionsItemList_SelectedItemChanged;
		}

		private void TransactionsItemList_SelectedItemChanged(ScrollList<FactionTransaction> sender, FactionTransaction oldItem, FactionTransaction newItem)
		{
			RefreshUnitContextMenu();
		}

		protected override void refresh()
		{
			base.refresh();
			RefreshTransactions();
			RefreshUnitContextMenu();
		}

		private void RefreshUnitContextMenu()
		{
			if (TransactionsItemList.FirstSelectedItem != null)
			{
				UnitContextButton.SetUnit(TransactionsItemList.FirstSelectedItem.Location);
			}
			else
			{
				UnitContextButton.SetUnit(null);
			}
		}

		private void RefreshTransactions()
		{
			if (Faction != null && (lastTransactionTime == 0f || Faction.LastTransactionTime > lastTransactionTime))
			{
				TransactionsItemList.SetItems(GetTransactions());
				lastTransactionTime = Faction.LastTransactionTime;
			}
		}

		protected override void update()
		{
			base.update();
			RefreshTransactions();
		}

		public IEnumerable<FactionTransaction> GetTransactions()
		{
			return Faction.RecentTransactions;
		}

		private void SetWaypointButtonClick()
		{
			if (Eng.LocalPlayer != null)
			{
				Eng.LocalPlayer.SetCustomWaypointToUnit(TransactionsItemList.FirstSelectedItem.Location);
			}
		}
	}
}
