using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class ToggleButton : MonoBehaviour
	{
		public Color ActiveColor = Color.green;

		public Graphic ColorTarget;

		private Color defaultColor = Color.white;

		[SerializeField]
		private bool isActive;

		public bool IsActive
		{
			get
			{
				return isActive;
			}
			set
			{
				if (isActive != value)
				{
					isActive = value;
					onActiveChanged();
				}
			}
		}

		protected virtual void onActiveChanged()
		{
			UpdateColor();
		}

		private void Awake()
		{
			if (ColorTarget != null)
			{
				defaultColor = ColorTarget.color;
			}
		}

		private void UpdateColor()
		{
			if (ColorTarget != null)
			{
				ColorTarget.color = (isActive ? ActiveColor : defaultColor);
			}
		}

		private void Start()
		{
			if (isActive)
			{
				onActiveChanged();
			}
		}
	}
}
