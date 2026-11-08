using System;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	[Obsolete]
	public class DockUIMenuButton : MonoBehaviour
	{
		private Button hudButton;

		public string TargetScene;

		private void Awake()
		{
			hudButton = GetComponent<Button>();
			hudButton.onClick.AddListener(hudButton_Activated);
		}

		private void hudButton_Activated()
		{
		}
	}
}
