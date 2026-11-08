using System.Linq;
using UnityEngine;

namespace OpenFrontier.IP.Scratchcard.Model
{
	public class ScratchcardSetup : MonoBehaviour
	{
		public ScratchcardPrize[] Prizes;

		public int CostPerCard = 1000;

		public int NumTickets => Prizes.Sum((ScratchcardPrize e) => e.TicketCount);

		public int TotalFund => NumTickets * CostPerCard;

		public int TotalWinnings => Prizes.Sum((ScratchcardPrize e) => e.PrizeValue * e.TicketCount);

		public int Profit => TotalFund - TotalWinnings;
	}
}
