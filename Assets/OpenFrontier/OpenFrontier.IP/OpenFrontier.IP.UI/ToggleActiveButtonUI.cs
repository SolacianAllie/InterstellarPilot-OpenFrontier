using UnityEngine;

namespace OpenFrontier.IP.UI
{
	public class ToggleActiveButtonUI : MonoBehaviour
	{
		public GameObject Target;

		private void OnClick()
		{
			if (Target != null)
			{
				Target.SetActive(!Target.activeSelf);
			}
		}
	}
}
