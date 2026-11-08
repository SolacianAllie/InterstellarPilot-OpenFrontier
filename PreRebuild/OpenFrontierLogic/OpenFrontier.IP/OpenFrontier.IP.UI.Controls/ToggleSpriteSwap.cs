using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Controls
{
	[RequireComponent(typeof(Toggle))]
	public class ToggleSpriteSwap : MonoBehaviour
	{
		public Sprite IsOnSprite;

		public Sprite IsOffSprite;

		public Image Image;

		private void Awake()
		{
			Toggle toggle = GetComponent<Toggle>();
			toggle.onValueChanged.AddListener((bool value) =>
			{
				Refresh(toggle.isOn);
			});
			Refresh(toggle.isOn);
		}

		public void Refresh(bool isOn)
		{
			Image.sprite = (isOn ? IsOnSprite : IsOffSprite);
		}
	}
}
