using UnityEngine;
using UnityEngine.EventSystems;

namespace OpenFrontier.IP.UI
{
	public class RepeatableButton : MonoBehaviour, IPointerDownHandler, IEventSystemHandler, IPointerUpHandler
	{
		private bool pressed;

		public bool IsPressed => pressed;

		public void OnPointerDown(PointerEventData eventData)
		{
			pressed = true;
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			pressed = false;
		}

		private void OnDisable()
		{
			pressed = false;
		}
	}
}
