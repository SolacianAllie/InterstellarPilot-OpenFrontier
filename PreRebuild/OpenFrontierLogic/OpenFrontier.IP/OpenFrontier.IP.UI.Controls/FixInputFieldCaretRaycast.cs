using TMPro;
using UnityEngine;

namespace OpenFrontier.IP.UI.Controls
{
	public class FixInputFieldCaretRaycast : MonoBehaviour
	{
		private void Start()
		{
			TMP_SelectionCaret componentInChildren = GetComponentInChildren<TMP_SelectionCaret>();
			if (componentInChildren != null)
			{
				componentInChildren.raycastTarget = false;
			}
		}
	}
}
