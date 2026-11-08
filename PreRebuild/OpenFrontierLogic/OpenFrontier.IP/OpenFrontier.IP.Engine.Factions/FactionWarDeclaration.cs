using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Common.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Factions
{
	public class FactionWarDeclaration
	{
		public double TimeOfDeclaration { get; set; }

		public Faction Aggressor { get; set; }

		public Faction Defender { get; set; }

		public FactionWarMotivation WarMotivation { get; set; } = FactionWarMotivation.NoneSpecified;

		public List<Faction> DefenderFactionsJoined { get; set; }

		public List<Faction> AggressorFactionsJoined { get; set; }

		public List<Faction> DefenderFactionsRequestedToJoin { get; set; }

		public List<Faction> AggressorFactionsRequestedToJoin { get; set; }

		public IEnumerable<Faction> GetDefendersDeclinedToJoin()
		{
			return DefenderFactionsRequestedToJoin.Where((Faction e) => !DefenderFactionsJoined.Contains(e));
		}

		public IEnumerable<Faction> GetAggressorsDeclinedToJoin()
		{
			return AggressorFactionsRequestedToJoin.Where((Faction e) => !AggressorFactionsJoined.Contains(e));
		}

		public void Validate()
		{
			if (Aggressor == null)
			{
				Debug.LogError("Aggressor faction is null");
			}
			if (Defender == null)
			{
				Debug.LogError("Defender faction is null");
			}
			foreach (Faction item in DefenderFactionsJoined)
			{
				if (item == Aggressor)
				{
					Debug.LogError($"Defender friend faction {item} is the same as aggressor");
				}
				if (item == Defender)
				{
					Debug.LogError($"Defender friend faction {item} is the same as defender");
				}
				if (AggressorFactionsJoined.Contains(item))
				{
					Debug.LogError($"Faction {item} has joined both sides of the war");
				}
			}
			foreach (Faction item2 in AggressorFactionsJoined)
			{
				if (item2 == Aggressor)
				{
					Debug.LogError($"Aggressor friend faction {item2} is the same as aggressor");
				}
				if (item2 == Defender)
				{
					Debug.LogError($"Aggressor friend faction {item2} is the same as defender");
				}
				if (DefenderFactionsJoined.Contains(item2))
				{
					Debug.LogError($"Faction {item2} has joined both sides of the war");
				}
			}
		}
	}
}
