using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class EngineDebugInfo : MonoBehaviour
	{
		public int SafePositionFinderCalls;

		public int NumDistressCallsSent;

		public int NumTimesEmpireRespondedToDistressCall;

		public int NumTimesScavengerOrderedToDistressCall;

		public int NumTimesScavengerFactionOrderedToCargo;

		public int NumTimesNonScavengerFactionOrderedToCargo;

		public int NumTimesScavengerOrderedToDestroyedShip;

		public int NumTimesEmpireLaunchedAttacks;

		public int NumTimesBanditsLaunchedAttacks;

		public int NumTimesOtherFactionLaunchedAttacks;

		public int NumTimesOutlawLaunchedRaid;

		public int NumTimesOutlawStartedSearchForTrader;

		public int NumTimesOutlawOrderToMoveToTraderTarget;

		public int NumTimesOutlawFailedToFindTraderTarget;

		public int NumTimesSectorControlChangedFaction;

		public int NumTimesSectorControlLostDueToDestruction;

		public int NumTimesSectorControlGainedDueToConstruction;

		public int NumTimesSectorControlGainedDueToConstructionByNonEmpire;

		public int NumTimesConstructedStationBroadcastedToNpc;

		public int NumTimesConstructedStationBroadcastedToPlayer;

		public int NumTimesBountyHunterPissedOffByStolenKill;

		public int NumTimesBountyHunterFleetEngagedTargetWithBounty;

		public int NumTimesFleetsOrderedToFlee;

		public int NumTimesFleetsOrderedToFightBack;

		public int NumTimesFleetsOrderedToEscortFleet;

		public int NumTimesFleetOrderedToDefensivePatrol;

		public int NumTimesFleetsRegrouped;

		public long NpcBountyHunterReward;

		public int NumTimesFleetSendForRepairs;

		public int NumTimesFleetSendForWaitForRepair;

		public int NumTimesFleetSendForRepairsTimeoutAndReissued;

		public int NumTimesFleetSendForRearming;

		public int NumFleetRearmedOrdersCompletedWithSpend;

		public int NumFleetRearmedOrdersCompletedWithNoSpend;

		public float InactiveUnitUpdateAverageUpdateTime;

		public int NumTimesTraderTargetBlacklistedDueToBeingClogged;

		public int NumShipsCaptured;

		public int NumStationsCaptured;

		public int NumAsteroidsDepleted;

		public int NumTimesFleetOrderedToDefendAttackedUnit;

		public int NumAsteroidsRespawned;

		public int CargoTradersTrimmed;

		public int NumCargoExpired;

		public int NumUnstableWormholesChangedTarget;

		public int PassengerGroups_NumDestinationSearchesStarted;

		public int PassengerGroups_NumGroupsCreated;

		public int PassengerGroups_NumPassengersCreated;

		public int PassengerGroups_NumGroupsSeeded;

		public int PassengerGroups_NumPassengersSeeded;

		public int PassengerGroups_NumGroupsExpired;

		public int NumTimesTradableCargoDumped;

		public int NumTimesFleetDumpedIncompatibleAmmo;

		public int NumTimesFleetRearmFailedDueToInsufficientCredits;

		public int NumTimesFleetRearmFailedDueToTimeout;

		public long CargoValueCollectedByNonScavengerFaction_CompatibleEquipment;

		public long CargoValueCollectedByNonScavengerFaction_IncompatibleEquipment;

		public long CargoValueCollectedByNonScavengerFaction_TradableCargo;

		public long CargoValueCollectedByScavengerFaction_CompatibleEquipment;

		public long CargoValueCollectedByScavengerFaction_IncompatibleEquipment;

		public long CargoValueCollectedByScavengerFaction_TradableCargo;

		public int NumTimesNpcPilotSuccessfullyUsesPointDefenceInInactiveSector;

		public int NumTimesNpcPilotFailsToUsePointDefenceInInactiveSector;

		public int NumTimesNpcPilotEvadesMissilesInInactiveSector;

		public int NumTimesNpcPilotFailsToEvadeMissilesInInactiveSector;

		public int Seeding_NumOutlawPilotsSeededWithBounty;

		public int Seeding_NumFactionsSeededDiscoveryOfOtherFaction;

		public int NumTimesFailedToGiveFactionUniqueName;

		public int NumNpcPeoplePromoted;

		public int NumNpcPeopleDemoted;

		public int NumFactionAIRequestsToBuildStations;

		public int NumFactionAIRequestsToBuildStationsApproved;

		public int NumFactionAIOrdersToBuildStation;

		public int NumFactionAIRequestsToBuildStationsInvalidated;

		public int NumFactionAIRequestsToBuildStationsTimedOut;

		public int NumFactionAIBuildStationRequestsDeclined;

		public int NumFactionAIRequestsToJoinWar;

		public int NumFactionAIRequestsToJoinWarAgreed;

		public int NumFactionAIMajorStationsStartedConstruction;

		public int NumBountyHunterFleetsSeedWithRandomPosition;

		public int NumTimesPilotDitchedShipAndKilled;

		public int NumTimesPilotDitchedShipAndLived;

		public int NumTimesDitchedShipCleanedUp;

		public int NumFactionAISurplusShipsStartedDismantling;

		public int NumTimesShipClaimedByAIFaction;

		public int NumTimesStationClaimedByAIFaction;

		public int NumTimesFleetOrderedToClaimShip;

		public int NumTimesFleetOrderedToClaimStation;

		public int NumTimesNpcPilotJumpedIntoClaimedShip;

		public int AsteroidDepleterSeeder_NumAsteroidsDestroyed;

		public int AsteroidDepleterSeeder_NumAsteroidsDepleted;

		public int AsteroidSprinklerSeeder_NumAsteroidsCreated;

		public int AsteroidSprinklerSeeder_NumAsteroidClustersCreated;

		public int AsteroidSprinklerSeeder_NumSectorsSprinkled;

		public int NumFactionAIFleetsDisbanded;

		public int NumTimesFactionAIPeaceTreatiesSigned;

		public int AdvPathfinder_HighestNavpointCountEnteredIntoMethod;

		public int AdvPathfinder_HighestNavpointReturnedFromMethod;

		public int NumTimesMercenaryTriedToAvoidOtherMercenaries;

		public int NumTimesMercenaryStartedWorkForOtherFaction;

		public int NumTimesFactionAIUpgradedStations;

		public int NumTimesFactionRequestedHelpFromSectorController;

		public int NumTimesFactionRespondedToHelpFromControlledFaction;

		public double FactionRecentDamageFromRequestsForHelp;

		public int NumTimesPlayerHasTargetThatIsOutOfDetectionRange;

		public int NumTimesNpcForcedToLoseTargetOutOfDetectionRange;

		public int NumTimesPlayerForcedToLoseTargetOutOfDetectionRange;

		public int NumTimesNpcForcedToLoseTargetNotInIntel;
	}
}
