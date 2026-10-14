using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.Factions;

namespace OpenFrontier.IP.Engine.EngineMusic
{
	/// <summary>
	/// Which family of music a place or a faction belongs to.
	/// </summary>
	/// <remarks>
	/// Four families for factions, and one that is not a faction at all.
	///
	/// Neutral is the odd one. It is what a system under nobody's control
	/// sounds like, and nothing else - not a faction that happens to be
	/// peaceful, and never a faction that failed to classify. Every one of the
	/// fourteen faction types maps to one of the other four, so there is no
	/// such thing as silence here, and no faction quietly falls through to
	/// ambience that means something else.
	/// </remarks>
	public enum MusicFamily
	{
		/// <summary>Bandits and outlaws.</summary>
		Anarchic = 0,

		/// <summary>Empires, mercenaries and bounty hunters - the military powers.</summary>
		Federal = 1,

		/// <summary>Traders.</summary>
		Imperial = 2,

		/// <summary>Everything else: the ordinary inhabited space.</summary>
		Allied = 3,

		/// <summary>System under nobody's control. Not a faction.</summary>
		Neutral = 4
	}

	/// <summary>
	/// Decides which family a faction's music comes from.
	/// </summary>
	/// <remarks>
	/// FactionType is a flags enum - a faction can be several things at once -
	/// so this is a priority walk rather than a lookup. Bandit and Outlaw come
	/// first because they are always Anarchic, even for a faction that also
	/// trades or builds stations: the rule is that pirates sound like pirates
	/// everywhere they hold space, and a pirate with a shop is still a pirate.
	///
	/// Empire, Mercenary and BountyHunter are Federal - the military and
	/// mercenary powers. Trader is Imperial. Everything else is Allied, which
	/// is the ordinary inhabited space rather than a diplomatic claim: it
	/// means "native space", not "friendly to you", and it does not change
	/// when you go to war with somebody.
	///
	/// That last part is a decision rather than an omission. A music family
	/// that tracked your diplomatic standing would mean a faction sounded
	/// different depending on what you thought of it, and the map would stop
	/// being a description of places. The families describe what a place IS.
	///
	/// The table is a static array rather than a dictionary because it is
	/// walked in order on every territory lookup, and sixteen entries is
	/// small enough that the lookup table would cost more than the walk.
	/// </remarks>
	public static class MusicFactionClassifier
	{
		/// <summary>Faction types in the order they win.</summary>
		private static readonly FactionType[] Priority =
		{
			// Always Anarchic, whatever else they are.
			FactionType.Outlaw,
			FactionType.Bandit,

			// The military and mercenary powers.
			FactionType.Empire,
			FactionType.Mercenary,
			FactionType.BountyHunter,

			// Commerce.
			FactionType.Trader
		};

		/// <summary>
		/// The family a faction's music comes from.
		/// </summary>
		/// <remarks>
		/// Never returns Neutral. A faction with no recognised type is Allied,
		/// because Neutral means a system nobody owns, and a faction always
		/// owns something - itself. Returning Neutral for an unrecognised
		/// faction would make a bug sound like empty space.
		/// </remarks>
		public static MusicFamily FamilyOf(FactionType type)
		{
			for (int i = 0; i < Priority.Length; i++)
			{
				if ((type & Priority[i]) == 0)
				{
					continue;
				}

				switch (Priority[i])
				{
					case FactionType.Outlaw:
					case FactionType.Bandit:
						return MusicFamily.Anarchic;

					case FactionType.Empire:
					case FactionType.Mercenary:
					case FactionType.BountyHunter:
						return MusicFamily.Federal;

					case FactionType.Trader:
						return MusicFamily.Imperial;
				}
			}

			return MusicFamily.Allied;
		}

		/// <summary>
		/// The family for a faction, or Neutral when there is no faction.
		/// </summary>
		/// <remarks>
		/// The one place Neutral comes from a faction: a null faction. That is
		/// a system under nobody's control, which is exactly what Neutral is
		/// for.
		/// </remarks>
		public static MusicFamily FamilyOf(Faction faction)
		{
			if (faction == null)
			{
				return MusicFamily.Neutral;
			}

			return FamilyOf(faction.FactionType);
		}

		/// <summary>In words, for logs and the diagnostic overlay.</summary>
		public static string NameOf(MusicFamily family)
		{
			switch (family)
			{
				case MusicFamily.Anarchic:
					return "Anarchic";

				case MusicFamily.Federal:
					return "Federal";

				case MusicFamily.Imperial:
					return "Imperial";

				case MusicFamily.Allied:
					return "Allied";

				case MusicFamily.Neutral:
					return "Neutral";

				default:
					return "Unknown";
			}
		}
	}
}