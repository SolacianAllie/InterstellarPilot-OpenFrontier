using UnityEngine;

namespace OpenFrontier.IP.Engine.Settings
{
	public class GameplaySettings : MonoBehaviour
	{
		[Tooltip("How much a kill raises an NPC pilot's combat efficiency, on the 0-1 scale. Accumulates per kill and is clamped to 1, so a pilot that kills enough fights sharpens up permanently.")]
		public float CombatEfficiencyChangePerKill = 0.05f;

		[Tooltip("Scales how fast virtue (the good/evil axis) shifts per event. Below 1 damps every virtue change in the game - the reputation events still happen, they just land softer.")]
		public float VirtueChangeRateMultiplier = 0.25f;

		[Tooltip("Chance per AI tick that a faction with spare credits buys an upgrade for one of its stations. Also gated on having over 10000 spare credits, so this is the rate once that is satisfied, not an absolute chance.")]
		public float ProbabilityOfFactionAIUpgradingStation = 0.1f;

		[Tooltip("Scales the odds of any successful boarding action turning into a capture. Multiplies the target unit class's own CaptureLikelihood, so it bends the roll rather than replacing it.")]
		public float CaptureProbabilityMultiplier = 0.2f;

		[Tooltip("Flat sensor-range bonus, in metres, while the unit is NOT cloaked and has fired its weapons in the last 6 seconds. The 6 seconds is a hardcoded constant in UnitComponentHolder, not a setting - this only sizes the bonus it grants.")]
		public float WeaponsFireSensorRangeChange = 1500f;

		[Tooltip("Raises shields automatically while the unit is cloaked. Off means a cloaking unit keeps its shields down, which leaves it fragile if it is decloaked early.")]
		public bool ShieldsUpWhenCloaked = true;

		[Tooltip("Any incoming damage forces a decloak, revealing the unit. This is what stops cloaking from being a free defensive action.")]
		public bool DecloakWhenReceiveDamage = true;

		[Tooltip("Ramming damage also forces a decloak, the same as weapons fire.")]
		public bool DecloakUnitsWhenCollisionDamage = true;

		[Tooltip("Whether sensor packages can detect cloaked units at all. Off makes cloaking a hard hide; on, detection still depends on range and on the cloak's detection multiplier.")]
		public bool CanDetectCloakedUnits = true;

		[Tooltip("Whether weapons can lock a cloaked target once it has been detected. Detection and targeting are separate switches - a unit can be spotted but not shootable.")]
		public bool CanTargetCloakedUnits = true;

		[Tooltip("Raises a HUD warning when a cloaked ship targets the player.")]
		public bool NotifyPlayerTargettedByCloakedShip = true;

		[Tooltip("Distance in metres within which a cloaked unit targeting the player triggers that warning while the player is in combat.")]
		public float NotifyPlayerTargettedMaxDistanceToTriggerInCombat = 2500f;

		[Tooltip("Seconds before an unclaimed cargo pod despawns. Measured on ScenarioElapsedTime (the scenario clock), so it keeps running while the game is paused or the player is in menus. 1800 is 30 minutes.")]
		public float CargoLifetime = 1800f;

		[Tooltip("Seconds before unclaimed ore cargo despawns. Shorter than CargoLifetime because ore is meant to be picked up promptly. Also on ScenarioElapsedTime. 600 is 10 minutes.")]
		public float CargoOreLifetime = 600f;

		[Tooltip("UNUSED - nothing reads this setting. Left in place in case the ejected-cargo rule was meant to be wired up.")]
		public double CargoPlayerEjectedLifetime = 600.0;

		[Tooltip("Health of a cargo pod before its volume is counted. A full pod's health is this plus CargoHealthPerVolumeUnit per volume unit, clamped to CargoMaxHealth.")]
		public float CargoBaseHealth = 60f;

		[Tooltip("Extra cargo-pod health per unit of volume, so a bigger pod is tougher but not proportionally.")]
		public float CargoHealthPerVolumeUnit = 20f;

		[Tooltip("Hard ceiling on cargo-pod health, reached before the volume bonus would top out.")]
		public float CargoMaxHealth = 1000f;

		[Tooltip("Master damage scale for the whole game. 0.5 halves every point of damage everywhere - the single biggest lever on how long fights last.")]
		public float GlobalDamageMultiplier = 0.5f;

		[Tooltip("Second damage scale, applied on top of GlobalDamageMultiplier but only to hull damage in DestructableUnit. Lower than the global one so hulls outlast components.")]
		public float GlobalHullDamageMultiplier = 0.8f;

		[Tooltip("Chance per AI evaluation that a bandit faction picks an unconnected fleet to attack, rather than waiting for a target of opportunity.")]
		public float ProbabilityOfBanditsRandomAttack = 0.05f;

		[Tooltip("Chance per AI evaluation that the Empire attacks an unconnected fleet. Multiplied by the faction's OffensiveStance, so an aggressive stance can push the effective chance above this value.")]
		public float ProbabilityOfEmpireRandomAttack = 0.8f;

		[Tooltip("UNUSED - nothing reads this setting. Asteroid health is not randomised from these two values.")]
		public float MinAsteroidHealth = 200f;

		[Tooltip("UNUSED - nothing reads this setting. Paired with MinAsteroidHealth; neither is wired up.")]
		public float MaxAsteroidHealth = 1200f;

		[Tooltip("Seconds before a unit that has just come through a wormhole may be scanned for intel again. Measured on Time.time (real engine time) rather than ScenarioElapsedTime, so it runs even while the scenario is paused.")]
		public float WormholeEntryScanCooldownTime = 2f;

		[Tooltip("UNUSED - nothing reads this setting. Looks like it was meant to scale statically-seeded intel.")]
		public float SeededStaticIntelMultiplier = 1f;

		[Tooltip("Seconds a faction must run before its AI will expand into a new sector. On ScenarioElapsedTime. 400 is about 6 minutes 40 seconds.")]
		public float MinTimeBeforeFactionSectorExpansion = 400f;

		[Tooltip("Damage fraction, 0-1, an NPC ship must take before its faction bothers to repair it. The threshold is then raised further by aggression, offensive stance and fleet strength, so a strong fleet patches up earlier.")]
		public float MinDamageBeforeNpcShipNeedsRepair = 0.2f;

		[Tooltip("How strongly small ships dominate an NPC faction's build choices. Acts as a multiplier on total fleet size and on spare capacity when scoring what to build, so raising it biases hard toward escorts over capital ships.")]
		public float NpcFactionSmallShipRatio = 2f;

		[Tooltip("Lower bound in seconds on the wait before an NPC faction orders its next ship built. Each order randomises between this and MaxTimeBeforeFactionShipBuild. On ScenarioElapsedTime. 180 is 3 minutes.")]
		public float MinTimeBeforeFactionShipBuild = 180f;

		[Tooltip("Upper bound in seconds on the wait before an NPC faction orders its next ship built. Pairs with MinTimeBeforeFactionShipBuild, which is drawn from the same Random.Range. 360 is 6 minutes.")]
		public float MaxTimeBeforeFactionShipBuild = 360f;

		[Tooltip("Scales how far laser shots land from where they are aimed. 0 is perfectly accurate; the generated error grows with this value, so it is the main difficulty dial for accuracy.")]
		public float LaserInaccuracyMultiplier;

		[Tooltip("Master output scale for power generators, applied to a component class's own PowerGenRate. 1.25 makes every reactor 25% stronger without touching any component class.")]
		public float PowerGeneratorRateMultiplier = 1.25f;

		[Tooltip("Seconds before a captured unit can be recaptured by anyone. Also the delay before its auto-turrets start firing again, and it is set the moment the capture lands. On ScenarioElapsedTime. 120 is 2 minutes.")]
		public double TimeBeforeUnitRecapture = 120.0;

		[Tooltip("Seconds a recaptured unit's auto-turrets stay silent after the capture, starting at the same instant as TimeBeforeUnitRecapture. On Time.time, not ScenarioElapsedTime.")]
		public float CapturedUnitAutoTurretCooldownTime = 10f;

		[Tooltip("How far past its rated range a laser turret keeps firing, as a multiple of that range. 1.5 lets a turret shoot half again as far as its class claims - a quiet way to buff every laser in the game.")]
		public float LaserOvershootMultiplier = 1.5f;

		[Tooltip("UNUSED - nothing reads this setting. Looks like it was meant to make countermeasures disrupt missiles arriving from outside the active sector.")]
		public float ProbabilityCountermeasuresDisruptMissileInactiveSector = 1f;

		[Tooltip("Chance a generated NPC is male, 0-1. Used when randomising both generated crews and named personnel.")]
		public float ProbabilityOfMaleNpc = 0.65f;

		[Tooltip("Share of a station's value refunded to its owner when it is dismantled. Despite the name this is a FRACTION, not a percentage: it is multiplied straight into the unit's value, so 1 refunds everything and 0.5 refunds half.")]
		public float DismantledStationRefundPercent = 1f;

		[Tooltip("Share of a ship's value refunded when it is dismantled. Also a fraction despite the name, same as the station one. 0.5 refunds half.")]
		public float DismantledShipRefundPercent = 0.5f;

		[Tooltip("Lets bandit factions order attacks on OTHER bandit factions. Off keeps outlaws fighting only non-bandits, which stops the faction list from eating itself.")]
		public bool AllowBanditOnBanditAttack;

		[Tooltip("How much noise to add when scoring which strategy to assign a fleet. Higher values make fleet behaviour more erratic between decisions.")]
		public float FleetStrategyAssignment_ScoreFuzziness = 0.3f;

		[Tooltip("Weight given to how well a fleet's individual ships measure up when picking a strategy, against FactionStrategyWeight.")]
		public float FleetStrategyAssignment_ShipEffectivenessWeight = 0.8f;

		[Tooltip("Weight given to the faction's standing strategy when picking one for a fleet, against ShipEffectivenessWeight. The two are read together, so they are most meaningful as a ratio.")]
		public float FleetStrategyAssignment_FactionStrategyWeight = 0.2f;

		[Tooltip("Drops the player's current target the moment it stops being valid. Off leaves the target hanging until the player clears it by hand.")]
		public bool ClearPlayerTargetWhenInvalidInstantly;

		[Tooltip("Drops an NPC's current target the moment it stops being valid. Off lets NPC units hold a stale target.")]
		public bool ClearNpcTargetWhenInvalidInstantly;

		[Tooltip("Scales how much component damage an explosion deals, separately from hull damage. 0.25 makes explosions mostly a hull-damage event, which spares modules from being shredded.")]
		public float ExplosionComponentDamageMultiplier = 0.25f;
	}
}
