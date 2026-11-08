using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP.UI.Screens.Orders.PatrolOrderCreator
{
	public class PatrolOrderCreatorList : ScrollList<SectorTarget>
	{
		public delegate void DeleteSectorTargetHandler(PatrolOrderCreatorListItem sender, SectorTarget sectorTarget);

		public Sector CurrentSector;

		public Color InactiveSectorListItemColor = new Color(0.5f, 0.5f, 0.5f);

		public event DeleteSectorTargetHandler DeletePatrolNodeEvent;

		internal void RequestDelete(PatrolOrderCreatorListItem patrolOrderCreatorListItem, SectorTarget item)
		{
			if (DeletePatrolNodeEvent != null)
			{
				DeletePatrolNodeEvent(patrolOrderCreatorListItem, item);
			}
		}
	}
}
