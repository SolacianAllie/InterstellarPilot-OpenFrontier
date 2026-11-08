using OpenFrontier.IP.Engine.Factions;

namespace OpenFrontier.IP.Engine
{
	public static class GamePlayerExtensions
	{
		public static bool NotTheSameFactionAs(this GamePlayer player, Unit unit)
		{
			if (!(unit == null) && (!(player.Faction == null) || !(unit.Faction == null)))
			{
				return player.Faction != unit.Faction;
			}
			return true;
		}

		public static bool NotTheSameFactionAs(this GamePlayer player, Faction faction)
		{
			if (!(player.Faction == null) || !(faction == null))
			{
				return player.Faction != faction;
			}
			return true;
		}
	}
}
