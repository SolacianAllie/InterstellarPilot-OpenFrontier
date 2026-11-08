using System.Collections.Generic;
using OpenFrontier.IP.Common;
using UnityEngine;

namespace OpenFrontier.IP.UI.Controls
{
	public class ListSorter : MonoBehaviour
	{
		public delegate void ListSorterApplyingHandler(ListSorter sender);

		public Sort SortOrder = Sort.Ascending;

		public event ListSorterApplyingHandler Applying;

		public virtual int GetSortMode()
		{
			return 0;
		}

		public virtual void SetSortMode(int sortType)
		{
		}

		public virtual List<string> GetSortOptionNames()
		{
			return new List<string>();
		}

		public virtual void Apply()
		{
			if (Applying != null)
			{
				Applying(this);
			}
		}
	}
}
