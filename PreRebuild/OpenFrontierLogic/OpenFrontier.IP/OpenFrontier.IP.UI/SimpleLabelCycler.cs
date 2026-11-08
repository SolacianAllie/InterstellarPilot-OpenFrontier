using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class SimpleLabelCycler : MonoBehaviour
	{
		public int CurIndex;

		public Text[] Labels;

		public Text CurrentLabel
		{
			get
			{
				if (CurIndex >= 0 && CurIndex < Labels.Length)
				{
					return Labels[CurIndex];
				}
				return null;
			}
		}

		public void CycleLabel(int step)
		{
			Text currentLabel = CurrentLabel;
			if (Labels.Length != 0)
			{
				CurIndex = Maths.WrapValue(CurIndex + step, 0, Labels.Length);
			}
			else
			{
				CurIndex = 0;
			}
			Text currentLabel2 = CurrentLabel;
			if (currentLabel2 != currentLabel)
			{
				if (currentLabel != null)
				{
					currentLabel.gameObject.SetActive(value: false);
				}
				if (currentLabel2 != null)
				{
					currentLabel2.gameObject.SetActive(value: true);
				}
			}
		}

		private void Awake()
		{
			Text currentLabel = CurrentLabel;
			for (int i = 0; i < Labels.Length; i++)
			{
				if (Labels[i] != currentLabel)
				{
					Labels[i].gameObject.SetActive(value: false);
				}
			}
		}
	}
}
