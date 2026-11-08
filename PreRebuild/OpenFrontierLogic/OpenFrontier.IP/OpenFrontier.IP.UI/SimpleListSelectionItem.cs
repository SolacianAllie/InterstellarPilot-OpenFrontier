using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class SimpleListSelectionItem : MonoBehaviour
	{
		private Button button;

		private SimpleListSelection list;

		public SimpleListSelection List
		{
			get
			{
				return list;
			}
			private set
			{
				if (list != value)
				{
					SimpleListSelection simpleListSelection = list;
					list = value;
					if (simpleListSelection != null)
					{
						simpleListSelection.DeregisterItem(this);
					}
					if (list != null)
					{
						list.RegisterItem(this);
					}
				}
			}
		}

		public Button Button => button;

		public void Awake()
		{
			FindList();
			button = GetComponent<Button>();
		}

		public void FindList()
		{
			List = UnityObjectHelper.FindInParentsOrSelf<SimpleListSelection>(gameObject);
		}

		private void Start()
		{
			button.onClick.AddListener(Button_Activated);
		}

		private void Button_Activated()
		{
			if (list != null)
			{
				list.SelectedItem = this;
			}
		}

		private void OnDestroy()
		{
			List = null;
		}
	}
}
