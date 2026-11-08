using OpenFrontier.IP.Engine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	[RequireComponent(typeof(Toggle))]
	public class ToggleSound : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
	{
		public AudioClip AudioClip;

		private Toggle toggle;

		public bool PlayWhenOnOnly;

		private void Awake()
		{
			toggle = GetComponent<Toggle>();
		}

		public void OnPointerClick(PointerEventData pointerEventData)
		{
			if (!(GameController.Instance == null) && GameController.Instance.PlayButtonSounds && AudioClip != null && (toggle.isOn || !PlayWhenOnOnly))
			{
				AudioHelper.PlaySound(AudioClip);
			}
		}
	}
}
