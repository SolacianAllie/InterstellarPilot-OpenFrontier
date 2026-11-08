using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Controls
{
	public class ModalWindow : MonoBehaviour
	{
		public delegate bool ModalWindowOpeningHandler(ModalWindow sender);

		public bool CloseOnChildClick;

		public Button DeselectionButton;

		public event ModalWindowOpeningHandler Opening;

		private void Awake()
		{
			gameObject.SetActive(value: false);
			DeselectionButton.onClick.AddListener(DeselectionButtonClick);
		}

		private void OnDisable()
		{
			ToggleActive(active: false);
		}

		private void DeselectionButtonClick()
		{
			ToggleActive(active: false);
		}

		public void ToggleActive()
		{
			ToggleActive(!gameObject.activeSelf);
		}

		public void ToggleActive(bool active)
		{
			if (!active || Opening == null || Opening(this))
			{
				gameObject.SetActive(active);
			}
		}
	}
}
