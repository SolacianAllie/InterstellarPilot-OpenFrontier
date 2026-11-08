using UnityEngine;

namespace OpenFrontier.IP.UI
{
	public class TwoStateCheckBox : MonoBehaviour
	{
		public GameObject OffGameObject;

		public GameObject OnGameObject;

		[SerializeField]
		private bool state = true;

		public bool State
		{
			get
			{
				return state;
			}
			set
			{
				if (state != value)
				{
					state = value;
					OnButtonActivatedChanged();
				}
			}
		}

		private void Start()
		{
			OnButtonActivatedChanged();
		}

		private void OnButtonActivatedChanged()
		{
			OnGameObject.SetActive(state);
			OffGameObject.SetActive(!state);
		}
	}
}
