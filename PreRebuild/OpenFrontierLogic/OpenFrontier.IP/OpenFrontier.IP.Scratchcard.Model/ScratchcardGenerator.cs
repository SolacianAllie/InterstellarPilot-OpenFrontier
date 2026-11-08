using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

namespace OpenFrontier.IP.Scratchcard.Model
{
	public class ScratchcardGenerator : MonoBehaviour
	{
		public ScratchcardPrize[] Generate(System.Random random, ScratchcardPrize winningPrize, ScratchcardPrize[] allPrizes, int slotCount = 9, int matchingSlotsToWin = 3)
		{
			ScratchcardPrize[] array = new ScratchcardPrize[slotCount];
			List<int> list = new List<int>();
			for (int i = 0; i < slotCount; i++)
			{
				list.Add(i);
			}
			if (winningPrize != null && winningPrize.PrizeValue > 0)
			{
				for (int j = 0; j < matchingSlotsToWin; j++)
				{
					PopulateRandomSlotAndRemoveFromQueue(random, winningPrize, array, list);
				}
			}
			while (list.Count > 0)
			{
				ScratchcardPrize prize = allPrizes[random.Next(0, allPrizes.Length)];
				int num = matchingSlotsToWin - 1;
				if (prize != null && prize.IsWinner && winningPrize != prize && array.Count((ScratchcardPrize e) => e != null && e.PrizeValue == prize.PrizeValue) < num)
				{
					PopulateRandomSlotAndRemoveFromQueue(random, prize, array, list);
				}
			}
			return array;
		}

		private static void PopulateRandomSlotAndRemoveFromQueue(System.Random random, ScratchcardPrize winningPrize, ScratchcardPrize[] slots, List<int> queue)
		{
			int index = random.Next(0, queue.Count);
			int num = queue[index];
			slots[num] = winningPrize;
			queue.RemoveAt(index);
		}
	}
}
