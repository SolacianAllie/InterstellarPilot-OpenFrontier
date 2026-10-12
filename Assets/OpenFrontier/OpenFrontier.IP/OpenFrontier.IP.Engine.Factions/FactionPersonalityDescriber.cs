namespace OpenFrontier.IP.Engine.Factions
{
	public static class FactionPersonalityDescriber
	{
		public static string GetDescription(Faction faction)
		{
			if (faction.Virtue < 0.05f)
			{
				return "Evil";
			}
			if (faction.Virtue < 0.2f)
			{
				return "Immoral";
			}
			if (faction.Virtue < 0.4f)
			{
				return "Crooked";
			}
			if (faction.Virtue >= 1f)
			{
				return "Paragon";
			}
			if (faction.Virtue >= 0.9f)
			{
				return "Saintly";
			}
			if (faction.Virtue > 0.75f)
			{
				return "Virtuous";
			}
			if (faction.Virtue > 0.6f)
			{
				return "Honourable";
			}
			if (faction.Aggression > 0.75f)
			{
				return "Aggressive";
			}
			if (faction.Aggression < 0.25f)
			{
				return "Gentle";
			}
			return "Normal";
		}
	}
}
