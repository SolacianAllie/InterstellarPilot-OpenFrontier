using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class QuitGameConfirmPanel : MonoBehaviour
	{
		public GameObject ButtonsRoot;

		public Button CancelButton;

		public Button ConfirmButton;

		public GameObject ConfirmPanelRoot;

		private float expireTime;

		public Button SourceButton;

		private void Awake()
		{
			ConfirmButton.onClick.AddListener(Cancel);
			CancelButton.onClick.AddListener(Cancel);
			if (SourceButton != null)
			{
				SourceButton.onClick.AddListener(Show);
			}
		}

		private void Update()
		{
			if (ConfirmPanelRoot.gameObject.activeSelf && Time.realtimeSinceStartup > expireTime)
			{
				Cancel();
			}
		}

		private void Show()
		{
			ButtonsRoot.gameObject.SetActive(value: false);
			ConfirmPanelRoot.gameObject.SetActive(value: true);
			expireTime = Time.realtimeSinceStartup + 4f;
		}

		private void OnDisable()
		{
			Cancel();
		}

		private void Cancel()
		{
			ButtonsRoot.gameObject.SetActive(value: true);
			ConfirmPanelRoot.gameObject.SetActive(value: false);
		}
	}
}
