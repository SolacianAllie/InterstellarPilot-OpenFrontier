using DigitalRubyShared;
using OpenFrontier.IP.Engine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Controls
{
	public class ItemAutoMultiSelect : MonoBehaviour, IPointerDownHandler, IEventSystemHandler, IPointerUpHandler
	{
		private double? timePointerDown;

		public Toggle ToggleTarget;

		public ScrollListItemBase Item;

		private void Awake()
		{
			if (ToggleTarget == null)
			{
				ToggleTarget = GetComponent<Toggle>();
			}
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			timePointerDown = Time.realtimeSinceStartupAsDouble;
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			timePointerDown = null;
		}

		private void OnDisable()
		{
			timePointerDown = null;
		}

		private void Update()
		{
			if (UI.TouchInputEnabled && timePointerDown.HasValue && ToggleTarget != null && Item.ParentListBase.CanEnableMultiSelect && Time.realtimeSinceStartupAsDouble > timePointerDown + (double)GameController.Instance.GameSettings.UIAutoMultiSelectTime)
			{
				_ = Item.ParentListBase.IsMultiSelectEnabled;
				Item.ParentListBase.IsMultiSelectEnabled = !Item.ParentListBase.IsMultiSelectEnabled;
				ToggleTarget.isOn = true;
				timePointerDown = null;
				ToggleTarget.OnPointerUp(new PointerEventData(EventSystem.current));
				EventSystem.current.SetSelectedGameObject(null);
				GameController.Instance.DebouceEventSystem(0.2f);
				FingersScript.Instance.ResetState(clearGestures: true);
				if (!Item.ParentListBase.IsMultiSelectEnabled && Item.ParentListBase.SelectedItemCount > 1)
				{
					Item.ParentListBase.DeselectAllExcept(Item);
				}
			}
		}
	}
}
