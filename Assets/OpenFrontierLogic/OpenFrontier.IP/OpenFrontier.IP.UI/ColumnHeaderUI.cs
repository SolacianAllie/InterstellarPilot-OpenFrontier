using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	[RequireComponent(typeof(Text))]
	public class ColumnHeaderUI : MonoBehaviour
	{
		public ColumnHeaderGroupUI HeaderGroup;

		private Text label;

		public bool StartSelected;

		public Text Label => label;

		private void Awake()
		{
			label = GetComponent<Text>();
		}

		private void Start()
		{
			if (StartSelected)
			{
				HeaderGroup.SelectedHeader = this;
			}
		}

		private void OnClick()
		{
			HeaderGroup.SelectColumnHeader(this);
		}
	}
}
