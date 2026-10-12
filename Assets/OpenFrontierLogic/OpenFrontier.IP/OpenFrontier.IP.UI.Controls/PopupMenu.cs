using System.Collections;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Controls
{
	public class PopupMenu : MonoBehaviour, IDeselectHandler, IEventSystemHandler
	{
		public delegate bool PopupMenuOpeningHandler(PopupMenu sender);

		private Button button;

		public Transform ToggleTarget;

		private ScreenBase ownerScreen;

		public bool MenuActive => ToggleTarget.gameObject.activeSelf;

		public event PopupMenuOpeningHandler Opening;

		private void Awake()
		{
			button = GetComponent<Button>();
			button.onClick.AddListener(OnButtonClick);
			ToggleMenu(active: false);
		}

		private void OnDisable()
		{
			ToggleMenu(active: false);
		}

		private bool HasAnyActiveItems()
		{
			for (int i = 0; i < ToggleTarget.transform.childCount; i++)
			{
				Selectable component = ToggleTarget.transform.GetChild(i).GetComponent<Selectable>();
				if (component != null && component.gameObject.activeSelf)
				{
					return true;
				}
			}
			return false;
		}

		private void OnButtonClick()
		{
			if (!MenuActive && Opening != null && !Opening(this))
			{
				return;
			}
			if (!HasAnyActiveItems())
			{
				UIController.Instance.ShowMessageBox("No options are available", MessageBoxButtons.Ok);
				return;
			}
			ToggleMenu();
			if (!MenuActive)
			{
				EventSystem.current.SetSelectedGameObject(null);
			}
		}

		public void ToggleMenu()
		{
			ToggleMenu(!MenuActive);
		}

		public void ToggleMenu(bool active)
		{
			ToggleTarget.gameObject.SetActive(active);
			if (active)
			{
				ownerScreen = UIController.Instance.ScreenNavigator.CurrentScreen;
				EventSystem.current.SetSelectedGameObject(gameObject);
			}
		}

		private void Update()
		{
			if (EngineASX.LoadedAndReady && MenuActive && UIController.Instance.ScreenNavigator.CurrentScreen != ownerScreen)
			{
				ToggleMenu(active: false);
			}
		}

		public void OnDeselect(BaseEventData eventData)
		{
			if (!(eventData is PointerEventData { pointerCurrentRaycast: var pointerCurrentRaycast } pointerEventData))
			{
				ToggleMenu(active: false);
			}
			else if (pointerCurrentRaycast.gameObject == null || !pointerEventData.pointerCurrentRaycast.gameObject.transform.IsChildOf(ToggleTarget))
			{
				ToggleMenu(active: false);
			}
			else
			{
				StartCoroutine(StartCloseCorourtine());
			}
		}

		private IEnumerator StartCloseCorourtine()
		{
			yield return new WaitForSecondsRealtime(0.3f);
			ToggleMenu(active: false);
		}
	}
}
