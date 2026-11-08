using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Controls
{
	public class ClearableInputFIeld : MonoBehaviour
	{
		public Button ClearButton;

		private TMP_InputField inputField;

		private void Awake()
		{
			inputField = GetComponent<TMP_InputField>();
			inputField.onValueChanged.AddListener((string _) =>
			{
				UpdateClearButton();
			});
			UpdateClearButton();
			ClearButton.onClick.AddListener(() =>
			{
				inputField.text = null;
			});
		}

		private void UpdateClearButton()
		{
			ClearButton.gameObject.SetActive(!string.IsNullOrEmpty(inputField.text));
		}
	}
}
