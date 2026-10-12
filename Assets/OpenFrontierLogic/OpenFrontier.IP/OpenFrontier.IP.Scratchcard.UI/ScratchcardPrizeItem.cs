using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Scratchcard.Model;
using OpenFrontier.IP.UI;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.Scratchcard.UI
{
	public class ScratchcardPrizeItem : MonoBehaviour
	{
		public Text Label;

		public SpriteSineColor ActivatedGameObject;

		private ScratchcardPrize prize;

		public Image Image;

		public ScratchcardPrize Prize
		{
			get
			{
				return prize;
			}
			set
			{
				prize = value;
			}
		}

		public void Refresh()
		{
			ActivatedGameObject.Reset();
			ActivatedGameObject.enabled = false;
			Label.text = TextFormattingHelper.FormatCredits(prize.PrizeValue);
		}

		public void HighlightWin()
		{
			ActivatedGameObject.Reset();
			ActivatedGameObject.enabled = true;
		}
	}
}
