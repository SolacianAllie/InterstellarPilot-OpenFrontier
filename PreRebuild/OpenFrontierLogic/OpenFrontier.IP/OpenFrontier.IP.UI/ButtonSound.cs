using OpenFrontier.IP.Engine;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	[RequireComponent(typeof(Button))]
	public class ButtonSound : MonoBehaviour
	{
		public AudioClip AudioClip;

		private Button button;

		private void Awake()
		{
			button = GetComponent<Button>();
			button.onClick.AddListener(OnClick);
		}

		private void OnClick()
		{
			if (GameController.Instance.PlayButtonSounds && AudioClip != null)
			{
				AudioHelper.PlaySound(AudioClip);
			}
		}
	}
}
