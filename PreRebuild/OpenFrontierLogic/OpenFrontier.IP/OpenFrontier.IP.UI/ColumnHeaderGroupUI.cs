using OpenFrontier.IP.UI.Extensions;
using UnityEngine;

namespace OpenFrontier.IP.UI
{
	public class ColumnHeaderGroupUI : MonoBehaviour
	{
		public GameObject ArrowWidget;

		private ColumnHeaderUI selectedHeader;

		public int SortDirection = 1;

		public ColumnHeaderUI SelectedHeader
		{
			get
			{
				return selectedHeader;
			}
			set
			{
				if (selectedHeader != value)
				{
					selectedHeader = value;
				}
			}
		}

		public void SelectColumnHeader(ColumnHeaderUI columnHeader)
		{
			if (columnHeader == selectedHeader)
			{
				SortDirection = -SortDirection;
			}
			else
			{
				SortDirection = 1;
				SelectedHeader = columnHeader;
			}
			RefreshArrow();
			applySort();
		}

		public void RefreshArrow()
		{
			ArrowWidget.gameObject.SetActive(selectedHeader != null);
			if (selectedHeader != null)
			{
				ArrowWidget.transform.SetParent(selectedHeader.transform);
				Vector3 zero = Vector3.zero;
				if (selectedHeader.Label != null)
				{
					zero += new Vector3(selectedHeader.Label.rectTransform.GetWidth() / 2f + 20f, 0f, 0f);
				}
				ArrowWidget.transform.localPosition = zero;
				Vector3 localScale = ArrowWidget.transform.localScale;
				localScale.x = Mathf.Abs(localScale.x) * (float)(-SortDirection);
				ArrowWidget.transform.localScale = localScale;
			}
		}

		protected virtual void applySort()
		{
		}
	}
}
