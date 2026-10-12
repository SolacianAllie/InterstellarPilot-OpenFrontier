using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

namespace OpenFrontier.IP.Scratchcard.Model
{
	public class ScratchcardDrawer : MonoBehaviour
	{
		public ScratchcardPrize Draw(ScratchcardSetup setup, System.Random random)
		{
			float[] array = new float[setup.Prizes.Length];
			List<ScratchcardPrize> list = setup.Prizes.OrderBy((ScratchcardPrize e) => e.PrizeValue).ToList();
			int numTickets = setup.NumTickets;
			for (int num = 0; num < list.Count; num++)
			{
				array[num] = (float)list[num].TicketCount / (float)numTickets;
			}
			float num2 = (float)random.NextDouble();
			int num3 = 0;
			for (float num4 = array[num3]; num2 >= num4; num4 += array[num3])
			{
				if (num3 >= array.Length - 1)
				{
					break;
				}
				num3++;
			}
			return list[num3];
		}
	}
}
