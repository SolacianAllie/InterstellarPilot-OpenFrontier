using System.Collections.Generic;

namespace OpenFrontier.IP.Engine.DitchShip
{
	public class DitchUnitCleanupModule
	{
		public struct DitchedUnitCleanupItem
		{
			public Unit Unit;

			public double TimeOfExpiry;

			public DitchedUnitCleanupItem(Unit unit, double timeOfExpiry)
			{
				this = default;
				Unit = unit;
				TimeOfExpiry = timeOfExpiry;
			}
		}

		private List<DitchedUnitCleanupItem> cleanupItems = new List<DitchedUnitCleanupItem>(32);

		private int currentCleanupIndex;

		public List<DitchedUnitCleanupItem> CleanupItems => cleanupItems;

		public void QueueForCleanup(Unit unit)
		{
			cleanupItems.Add(new DitchedUnitCleanupItem(unit, EngineASX.Instance.ScenarioElapsedTime + GameController.Instance.GameSettings.DitchUnitSettings.TimeOfPersistingDitchedShip));
		}

		public void Update()
		{
			if (cleanupItems.Count <= 0)
			{
				return;
			}
			currentCleanupIndex++;
			if (currentCleanupIndex >= cleanupItems.Count)
			{
				currentCleanupIndex = 0;
			}
			DitchedUnitCleanupItem ditchedUnitCleanupItem = cleanupItems[currentCleanupIndex];
			if (ditchedUnitCleanupItem.Unit == null || ditchedUnitCleanupItem.Unit.Faction != null)
			{
				cleanupItems.RemoveAt(currentCleanupIndex);
			}
			else if (EngineASX.Instance.ScenarioElapsedTime > ditchedUnitCleanupItem.TimeOfExpiry)
			{
				if (ditchedUnitCleanupItem.Unit.Destructable != null)
				{
					ditchedUnitCleanupItem.Unit.Destructable.KillAnonymously();
				}
				else
				{
					ditchedUnitCleanupItem.Unit.SafeDestroy();
				}
				EngineASX.Instance.DebugInfo.NumTimesDitchedShipCleanedUp++;
				cleanupItems.RemoveAt(currentCleanupIndex);
			}
		}
	}
}
