using OpenFrontier.IP.Common;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Controls
{
	[RequireComponent(typeof(Toggle))]
	public class SortOrderButton : MonoBehaviour
	{
		public Image Image;

		public Sprite SortAZSprite;

		public Sprite SortZASprite;

		private Toggle toggle;

		public Sort SortMode
		{
			get
			{
				if (toggle == null)
				{
					Awake();
				}
				if (!toggle.isOn)
				{
					return Sort.Descending;
				}
				return Sort.Ascending;
			}
			set
			{
				toggle.isOn = value == Sort.Ascending;
			}
		}

		public Toggle Toggle
		{
			get
			{
				if (toggle == null)
				{
					Awake();
				}
				return toggle;
			}
		}

		private void Awake()
		{
			toggle = GetComponent<Toggle>();
			toggle.onValueChanged.AddListener((bool value) =>
			{
				RefreshImage();
			});
			RefreshImage();
		}

		private void RefreshImage()
		{
			Image.sprite = ((SortMode == Sort.Ascending) ? SortAZSprite : SortZASprite);
		}
	}
}
