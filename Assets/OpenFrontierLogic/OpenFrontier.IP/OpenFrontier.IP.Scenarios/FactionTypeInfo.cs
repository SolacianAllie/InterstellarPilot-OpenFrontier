using System.Collections.Generic;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;
using UnityEngine.Serialization;

namespace OpenFrontier.IP.Scenarios
{
	public class FactionTypeInfo : MonoBehaviour
	{
		public FactionShipCapType ShipCapType;

		public bool GeneratePassengerGroups = true;

		public bool ReceivePassengerGroups = true;

		public float ProbabilityOfRandomGroupStartPositionMultiplier = 1f;

		public FactionType FactionType;

		public FactionAIType FactionAIType;

		public int BaseRpProvision = 500;

		public List<StationPurpose> AllowableStationPurposes;

		public float SecurityRatingMultiplier;

		public string CustomUnitDesignation;

		public string WarPartyUnitDesignation;

		public string ShortCustomUnitDesignation;

		public float ProbabilityOfBountySeededOnPilots = 1f;

		[FormerlySerializedAs("ProbabilityOfSeedingBountyOnPilots")]
		public float ProbabilityOfSeedingBountyPlacedOnOthers = 1f;

		public float MinShipCombatRating;

		public float MaxShipCombatRating = 10f;

		public bool BuildCloakedShips;

		public bool CanMakeHomeInHostileControlledSector;

		public bool BuildIllegalStations;

		public SecurityTypePreference SecurityTypePreference;

		public float SecurityTypePreferenceMultiplier = 0.2f;

		public float BorderSectorPreference;

		public float DistressCallProbabilityMultiplier = 1f;

		public float ProbabilityOfDistressCallGoingToPlayerMultiplier = 1f;

		public StationPurpose AllowableStationPurposesFlags
		{
			get
			{
				StationPurpose stationPurpose = StationPurpose.None;
				foreach (StationPurpose allowableStationPurpose in AllowableStationPurposes)
				{
					stationPurpose |= allowableStationPurpose;
				}
				return stationPurpose;
			}
		}
	}
}
