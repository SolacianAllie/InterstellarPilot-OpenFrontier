using OpenFrontier.IP.Engine.Settings.Performance;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	/// <summary>
	/// Frame-time budgets. Nearly every field here caps how much simulation
	/// work the engine is allowed to do per second, or turns a feature off
	/// past a distance from the camera. The simulation runs on many more
	/// units and sectors than are on screen, so these are the dials that
	/// decide what stays accurate under load and what gets quietly skipped or
	/// switched off. Raising one buys fidelity and costs frame time.
	/// </summary>
	public class PerformanceSettings : MonoBehaviour
	{
		[Tooltip("Shortest gap in seconds between checks for cargo collectors picking up loose pods. The actual gap is randomised across the range from here up to MaxNpcCargoCollectorCheckInterval, so collectors do not all act on the same frame.")]
		public float MinNpcCargoCollectorCheckInterval = 1f;

		[Tooltip("Longest gap in seconds between cargo collector checks. The high end of the randomised interval above - the gap is never longer than this.")]
		public float MaxNpcCargoCollectorCheckInterval = 6f;

		[Tooltip("How far away thruster effects still get drawn. Grouped settings rather than a single number, because the visible length and the particle budget are tuned separately.")]
		public ThrusterDrawDistanceSettings ThrusterDrawDistanceSettings;

		[Tooltip("Budget for how many units an NPC scans for targets each second in the sector the player is in. Units over the budget are left for the next frame, so a busy sector resolves targets more slowly rather than all at once.")]
		public float NpcTargetScanActiveFrequency = 2f;

		[Tooltip("Budget for how many units an NPC scans for targets each second in sectors the player is NOT in. Much lower than the active figure - off-screen sectors are simulated cheaply because nobody is watching them.")]
		public float NpcTargetScanInactiveFrequency = 6f;

		[Tooltip("Hard cap on units actually processed per second by NPC target scans, across the whole game. The budget above decides how much of this is spent in the active sector; this stops the total running away.")]
		public float NpcTargetScanProcessedUnitsPerSecond = 20f;

		[Tooltip("Seconds between checks that top up a passenger group's morale and needs. Very frequent because passenger happiness drifts continuously and a visible slump looks like a bug.")]
		public float PassengerManagerTimeBetweenReplenish = 0.1f;

		[Tooltip("Budget for how many candidate trades an AI fleet weighs per second. The core economy budget - an AI's decisions get worse as this falls, so it is the first thing to raise if trading looks unintelligent.")]
		public float AITradeSearchSearchesPerSecond = 100f;

		[Tooltip("Budget for bounty hunter AI target searches per second. Separate from trade search because hunters chase movers rather than prices.")]
		public float AIBountyHunterSearchSearchesPerSecond = 100f;

		[Tooltip("Budget for AI trade searches per second when the trade concerns something the player is involved in. Raised well above the general figure so the AI never feels stupid about the player's own trade.")]
		public float AITradeSearchSearchesPerSecondPlayer = 300f;

		[Tooltip("Budget for how many cargo goods an AI weighs per second when looking for stock to buy.")]
		public float AICargoSearchSearchesPerSecond = 60f;

		[Tooltip("Budget for cargo searches per second when the goods involved are the player's. Like the trade budget, it is deliberately generous so the player is never outmanoeuvred on their own cargo.")]
		public float AICargoSearchSearchesPerSecondPlayer = 120f;

		[Tooltip("Seconds between simulation ticks for a unit, for units that are not the player's. The master step size for off-screen unit movement and behaviour - the single biggest lever on how heavy the simulation is.")]
		public float PreferredTimeBetweenUnitUpdates = 3f;

		[Tooltip("Seconds between simulation ticks for a whole group of units moving together. Groups are cheap to step as one unit, which is why this can match the per-unit figure.")]
		public float PreferredTimeBetweenGroupUpdates = 3f;

		[Tooltip("Spawns impact particles where weapons and projectiles land. The first thing to switch off on low-end hardware.")]
		public bool CreateImpactParticles = true;

		[Tooltip("Shows hull damage effects - scorch marks, sparks, deformation. Off removes a large amount of per-frame effect work in a firefight.")]
		public bool ShowDamageEffects = true;

		[Tooltip("Shows the shield hit flash and its renderer effects. Off leaves shields looking untouched while still working, which can read as a bug, so it is worth keeping on unless frames are genuinely tight.")]
		public bool ShowShieldHitEffects = true;

		[Tooltip("Distance in metres from the camera within which a unit visibly rolls and banks into its turns. Beyond this, units fly flat - nobody can tell at that range, and the rotation maths stops being worth the cost.")]
		public float UnitRollMaxDistance = 400f;

		[Tooltip("Seconds between recalculations of how much ore an active AI pilot's ship is worth mining. Short because the pilot needs to react to a field running out under the ship.")]
		public float AIActivePilotMineCalcInterval = 0.2f;

		[Tooltip("Distance in metres beyond which shield hit effects are skipped. A cheap distance cull - the effect is invisible at range but still costs a renderer and a light.")]
		public float MaxShieldHitDist = 200f;

		[Tooltip("Near edge of the distance band, in metres, where unit trails are drawn. Below this they are off entirely, to avoid the trails flickering as a unit jitters on the threshold.")]
		public float UnitTrailsDrawLowerDist = 75f;

		[Tooltip("Far edge of the distance band, in metres, where unit trails are drawn. Past this they are off again. Between the two values trails fade, so there is no visible pop at either boundary.")]
		public float UnitTrailsDrawUpperDist = 100f;

		[Tooltip("Seconds between faction AI decision passes. Each pass has every faction consider expanding, building and trading, so this is the cost of the whole background simulation happening at all.")]
		public float FactionAIUpdateInterval = 3f;

		[Tooltip("Hard cap on faction intel scans processed per second, across all factions. Stops one faction's sensors from consuming the whole budget during a large battle.")]
		public int FactionIntelMaxScansProcessedPerSecond = 120;

		[Tooltip("Seconds between a faction re-scanning a sector for intel. Each scan is expensive, so this is the difference between a faction noticing a new station and noticing it much later.")]
		public float FactionIntelScanInterval = 2f;

		[Tooltip("Hard cap on heatmap cells recomputed per second. The heatmap repaints itself continuously, and without a cap a large sector makes the repaint cost more than the game.")]
		public int MaxHeatmapCellsUpdatedPerSecond = 10;

		[Tooltip("Seconds between heatmap dissipation passes, where old contacts fade out. Despite the name this is an interval, not a rate - it also stops contacts lingering far longer than intended.")]
		public float HeatmapDissipationUpdateFrequency = 10f;

		[Tooltip("Budget for how many candidate sectors the Empire AI examines per second when looking to expand. Low because an expansion search is an expensive comparison against a long list.")]
		public float EmpireAIExpansionSearchItemsPerSecond = 4f;

		[Tooltip("Budget for how many destinations a passenger group weighs per second when choosing where to travel. A pure frame-time control - the results are the same, just resolved over more frames.")]
		public float PassengerGroupDestinationSearchItemsPerSecond = 8f;

		[Tooltip("Seconds a faction's cached best trader stays valid before the AI looks for a new one. A long cache means AI fleets head for a trader that has since moved or died.")]
		public float FactionTraderTargetCacheMaxAge = 120f;

		[Tooltip("Turns off ship colliders in the active sector beyond a distance from the camera, so the simulation does not waste time resolving collisions nobody can see. Off leaves every collider live, which is expensive in a crowded sector.")]
		public bool DisableCollidersInActiveSector = true;

		[Tooltip("Distance in metres from the camera within which colliders are always on. Below this the ship is on screen, so it must collide properly whatever the setting above says.")]
		public float DisableCollidersInActiveSectorLowerDistance = 2000f;

		[Tooltip("Distance in metres from the camera beyond which colliders are switched off. Colliders stay live throughout the band between this and LowerDistance, which is the taper that avoids ships clipping as they cross the threshold.")]
		public float DisableCollidersInActiveSectorUpperDistance = 2200f;

		[Tooltip("Budget for how many candidate targets an NPC pilot scores per second. Scoring is per target, so a busy sector queues rather than stalling.")]
		public float NpcPilotTargetScoresPerSecond = 20f;

		[Tooltip("Budget for how many candidate targets auto-turrets score per second. Shared across every turret in the sector, which is why it is modest - there are many turrets and few pilots.")]
		public float AutoTurretTargetScoresPerSecond = 20f;

		[Tooltip("Budget for how many asteroid fields an AI weighs per second when deciding what to mine. Only consulted once a pilot has settled on mining at all.")]
		public int AIMineSearchSearchesPerSecond = 30;

		[Tooltip("Multiple of a light's own range out to which it is still lit and drawn. Cheap light culling - a default-range light covers a lot of empty space that costs a shadow pass for nothing.")]
		public float LightingFxMaxCameraDistanceMultiplier = 5f;

		[Tooltip("Distance in metres from the camera beyond which cargo the player spat out stops being simulated as a physical object and is handled as a plain despawning pod. Keeps debris from dragging physics work along behind the player.")]
		public float EjectedCargoPhysicsMaxRangeFromCamera = 1500f;

		[Tooltip("Distance in metres from the camera beyond which countermeasure particles are not drawn. A distance cull on a burst effect that fires often in a fight.")]
		public float CountermeasureParticlesMaxDistance = 1000f;
	}
}
