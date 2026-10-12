using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class TabControl : MonoBehaviour
	{
		public Transform Toggles;

		public Transform Panels;

		private void Awake()
		{
			List<Toggle> componentsInImmediateChildren = Toggles.GetComponentsInImmediateChildren<Toggle>();
			ToggleGroup component = Toggles.GetComponent<ToggleGroup>();
			if (componentsInImmediateChildren.Count > 0)
			{
				componentsInImmediateChildren[0].isOn = true;
			}
			for (int i = 0; i < componentsInImmediateChildren.Count; i++)
			{
				Toggle toggle = componentsInImmediateChildren[i];
				if (toggle.group == null)
				{
					toggle.group = component;
				}
				WireUpToggle(toggle, Panels.GetChild(i));
				Panels.GetChild(i).gameObject.SetActive(componentsInImmediateChildren[i].isOn);
			}
		}

		private void WireUpToggle(Toggle toggle, Transform target)
		{
			toggle.onValueChanged.AddListener((bool val) =>
			{
				target.gameObject.SetActive(val);
			});
		}
	}
}
