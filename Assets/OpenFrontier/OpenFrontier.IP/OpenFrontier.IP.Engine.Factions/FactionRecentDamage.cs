using System;

namespace OpenFrontier.IP.Engine.Factions
{
	[Serializable]
	public class FactionRecentDamage
	{
		internal float lastAttackOpinionChangeRecentAttacks;

		private float recentDamageReceived;

		private float timeOfLastDamageReceived;

		public Faction TargetFaction;

		public float TimeOfLastDamageReceived
		{
			get
			{
				return timeOfLastDamageReceived;
			}
			set
			{
				timeOfLastDamageReceived = value;
			}
		}

		public float RecentDamageReceived
		{
			get
			{
				return recentDamageReceived;
			}
			set
			{
				recentDamageReceived = value;
				if (recentDamageReceived < 0f)
				{
					recentDamageReceived = 0f;
				}
			}
		}

		public void AddToRecentDamageReceived(float damage)
		{
			RecentDamageReceived += damage;
		}
	}
}
