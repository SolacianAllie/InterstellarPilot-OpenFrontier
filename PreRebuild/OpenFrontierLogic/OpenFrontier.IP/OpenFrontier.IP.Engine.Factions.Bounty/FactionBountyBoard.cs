using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Factions.Bounty
{
	public class FactionBountyBoard : MonoBehaviour
	{
		private float lastChangedTime;

		private Faction faction;

		private List<FactionBountyItem> bountyItems = new List<FactionBountyItem>();

		public List<FactionBountyItem> BountyItems
		{
			get
			{
				return bountyItems;
			}
			set
			{
				bountyItems = value;
			}
		}

		public Faction Faction
		{
			get
			{
				return faction;
			}
			set
			{
				faction = value;
			}
		}

		public float LastChangedTime => lastChangedTime;

		public void RemoveBountyPlacedByFaction(Faction faction)
		{
			for (int i = 0; i < bountyItems.Count; i++)
			{
				if (bountyItems[i].Source == faction)
				{
					bountyItems.RemoveAt(i);
					i--;
				}
			}
		}

		internal FactionBountyItem AddBounty(Faction source, Person pilot, int bounty, double? timeOfSighting)
		{
			if (pilot != null)
			{
				if (bounty > 0)
				{
					FactionBountyItem factionBountyItem = GetExistingBounty(pilot, source);
					if (factionBountyItem == null)
					{
						factionBountyItem = new FactionBountyItem
						{
							Person = pilot,
							Bounty = bounty,
							Source = source
						};
						bountyItems.Add(factionBountyItem);
					}
					else
					{
						factionBountyItem.Bounty += bounty;
					}
					factionBountyItem.TimeOfLastSighting = timeOfSighting;
					lastChangedTime = Time.time;
					return factionBountyItem;
				}
			}
			else
			{
				Debug.LogError("AddBounty: required valid pilot", this);
			}
			return null;
		}

		public bool RemoveBounty(FactionBountyItem item)
		{
			bool result = bountyItems.Remove(item);
			lastChangedTime = Time.time;
			return result;
		}

		public bool HasAnyBounty()
		{
			return bountyItems.Count > 0;
		}

		public FactionBountyItem GetExistingBounty(Person pilot, Faction sourceFaction)
		{
			foreach (FactionBountyItem bountyItem in bountyItems)
			{
				if (bountyItem.Person == pilot && bountyItem.Source == sourceFaction)
				{
					return bountyItem;
				}
			}
			return null;
		}

		public void TrimInvalids()
		{
			for (int i = 0; i < bountyItems.Count; i++)
			{
				FactionBountyItem factionBountyItem = bountyItems[i];
				if (factionBountyItem == null || !factionBountyItem.IsValid)
				{
					bountyItems.RemoveAt(i);
					i--;
				}
			}
		}
	}
}
