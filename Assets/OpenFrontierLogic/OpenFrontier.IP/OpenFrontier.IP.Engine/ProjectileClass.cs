using OpenFrontier.IP.Common;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class ProjectileClass : MonoBehaviour
	{
		public float EffectiveFiringRangeStationery = 10000f;

		public float EffectiveFiringRangeAgainstMobile = 10000f;

		public bool IsWeapon = true;

		public string Name;

		public bool IsConventionalWeapon = true;

		public CargoClass AmmoClass;

		public int AmmoRequirement = 1;

		public int BurstRounds = 1;

		public float BurstTimeBetweenRounds = 0.2f;

		public bool CollisionEnabled = true;

		public bool CreateExplosionWhenExpired;

		public float Damage = 10f;

		public float DamageFuzziness = 0.1f;

		public int DefaultAmmoComplement = 3;

		public bool DestroyOnCollide;

		public float DestroyOnCollideDelayTime = 2f;

		public ExplosionClass ExplosionClass;

		public float HomingAquiredTargetThreshold = 0.5f;

		public bool HomingCanLoseTarget = true;

		public float HomingLostTargetDotThreshold = 0.25f;

		public bool IsHoming;

		/// <summary>
		/// Half-angle of the seeker cone, in degrees - how far off its nose the
		/// target has to be before this missile stops being able to see it.
		/// </summary>
		/// <remarks>
		/// This is the degrees equivalent of the two dot thresholds below, and
		/// it is the one to author: 0.5 is not obviously 60 degrees, and every
		/// missile in the game wants a different number.
		///
		/// It is a half-angle, measured from the missile's nose, so 90 means it
		/// sees 90 degrees either side of straight ahead and 180 degrees
		/// across in total.
		///
		/// It decides two things, because they are the same thing. The seeker
		/// cannot see past it - and the launching ship cannot fire the missile
		/// at a target outside it, because a missile leaves along the hull's
		/// nose and would spend the whole flight unable to acquire. So the
		/// launch arc is read from here rather than stored on the turret.
		///
		/// Zero means unset, which is what every prefab written before this
		/// field existed reads, and zero means NO cone - the missile sees in
		/// every direction exactly as it always did, and imposes no launch
		/// requirement. That is deliberate: it makes the field safe to add to
		/// existing content, and it means a missile that predates seekers keeps
		/// behaving like one.
		/// </remarks>
		public float SeekerConeAngle;

		/// <summary>
		/// The wider cone a blind missile has to get the target back inside to
		/// re-acquire. Zero means the same as <see cref="SeekerConeAngle"/>.
		/// </summary>
		/// <remarks>
		/// Without the gap a missile sitting exactly on the edge of its cone
		/// flickers between locked and blind every evaluation, and the
		/// dead-reckoning below turns that flicker into a random walk.
		/// </remarks>
		public float SeekerReacquireAngle;

		/// <summary>
		/// Seconds the seeker may stay blind before the missile gives up on the
		/// target for good. Zero means no second chances - one blind evaluation
		/// ends it, which is what every missile did before seekers existed.
		/// </summary>
		public float SeekerLockPersistence;

		/// <summary>
		/// How fast, in metres per second, the blind missile's guess at where
		/// the target is wanders away from the truth.
		/// </summary>
		/// <remarks>
		/// This is what makes a broken lock cost something. A missile that
		/// loses its cone does not snap back on: it flies on the last position
		/// and velocity it knew, and gets it steadily more wrong.
		/// </remarks>
		public float SeekerBlindDrift;

		/// <summary>
		/// How often the seeker checks whether it can see the target, in
		/// seconds. Zero falls back to the default rather than to every frame.
		/// </summary>
		public float SeekerCheckInterval;

		/// <summary>
		/// How far off the launching ship's nose the target has to be for a
		/// semi-active seeker to hold it at all, in degrees. Zero means no arc
		/// was authored and the launcher is not consulted.
		/// </summary>
		/// <remarks>
		/// Only a semi-active seeker reads this, and it is not one of the ways
		/// to beat that type - it is the premise. A semi-active missile carries
		/// no radar of its own and listens to the echo off a target the
		/// launching ship is illuminating, so the moment that ship turns away
		/// there is nothing lighting the target up and the missile goes blind
		/// whether the target is cloaked or not.
		///
		/// It used to be named SeekerCloakedHoldArc and only consulted while
		/// the contact was cloaked, on the reasoning that the launcher picture
		/// was only interesting once cloak was in play. That was wrong: it made
		/// a semi-active missile track a target its firing ship had turned right
		/// around from, which is the one thing semi-active homing cannot do. The
		/// cloak rule - that the contact must have been seen before it went -
		/// is a separate test in MissileSeeker and always was.
		///
		/// An active seeker does not consult it at all. Its own transmitter
		/// lights the target up, so the launcher can do whatever it likes, and
		/// that is the whole practical difference between the two types and the
		/// reason a ship under ARH missiles has to fight them rather than turn
		/// away.
		///
		/// Zero is the default because that is what every prefab written before
		/// this field existed reads. It is read as "do not consult the launcher"
		/// rather than as "the target must be dead ahead", so a prefab that has
		/// never heard of the field keeps the behaviour it had.
		/// </remarks>
		public float SeekerLauncherArc;

		/// <summary>
		/// How much radiated heat a ship must be putting out before an infrared
		/// seeker can hold it. Zero means every ship counts.
		/// </summary>
		/// <remarks>
		/// Venting is visibility. This is measured as what a ship is actually
		/// throwing out - what it can shed, times how much it has to shed - and
		/// not as what it is storing or as what its engines are producing.
		///
		/// Both of those were tried and both are wrong in opposite directions.
		/// The store alone makes a well-cooled hull the dimmest thing in the
		/// sector. The engine's output alone makes a ship with no radiator the
		/// brightest, because it is running flat out with nowhere to put the heat
		/// - exactly backwards, since a shut radiator means nothing escapes.
		///
		/// So the trade is the interesting one: fitting a big thermal vent makes
		/// a ship a bright, obvious target to anything looking for heat, and
		/// fitting none makes it dark and unable to run hot for long.
		///
		/// Because every thruster's ThermalLoad is its own EnginePower times one
		/// constant, hull size sets the ceiling on how hot a ship can get: a
		/// scout tops out well below what a radiator can vent, so it can never
		/// out-radiate a battleship however well vented, while a battleship
		/// saturates and becomes the brightest thing in the sector.
		///
		/// It lives on the projectile class so a missile can be tuned to how good
		/// its seeker actually be. Zero is the default because that is what every
		/// prefab written before this field existed reads.
		/// </remarks>
		public float IrEngineHeatThreshold;

		/// <summary>
		/// How much of a signature is still visible when the target is flying
		/// straight at the seeker. 1 means aspect does not matter at all, which
		/// is what every prefab written before this field existed reads.
		/// </summary>
		/// <remarks>
		/// Notching, and it is the same mechanic for every seeker. What a
		/// sensor sees depends on which way the target is pointing: from astern
		/// you see the engines, from dead ahead you see the cold nose, and
		/// beam-on is somewhere between. Flying beam-on to an incoming missile
		/// is called notching, and it is the cheapest counter there is because
		/// it costs nothing - you are already turning.
		///
		/// It works on radar and on heat at once, for the same reason it works
		/// on both in reality: it is a property of the target's shape and where
		/// its heat is, not of the sensor. And it is a continuum, which is what
		/// makes it interesting - a seeker whose threshold sits near a hull's
		/// notched signature will hold sometimes and lose sometimes, so turning
		/// is a real answer rather than a guaranteed one.
		///
		/// Half is the figure worth starting from, which is what the reference
		/// this was modelled on uses for radar.
		/// </remarks>
		public float SignatureFrontFactor = 1f;

		/// <summary>
		/// How strong a radar return has to be for this seeker to hold it.
		/// Zero means any contact at all will do.
		/// </summary>
		/// <remarks>
		/// This is a signal floor rather than a signature floor, so what it
		/// really sets is a detection RANGE: the range works out at
		/// <see cref="RadarMaxRange"/> times the target's cross section to the
		/// quarter power, divided by this. Raising it shortens every range
		/// proportionally rather than carving out a band that nothing crosses.
		///
		/// That matters because notching now removes range instead of
		/// failing to cross a threshold - which is a far better mechanic than
		/// the one it replaced, where the whole ladder sat far above the line
		/// and turning achieved nothing on its own.
		///
		/// The cross section comes from <see cref="Unit.RadarCrossSection"/>
		/// and not from heat, which is the whole reason a radar seeker works at
		/// all. It is a separate field from <see cref="IrEngineHeatThreshold"/>
		/// because a radar return and a plume are different quantities on
		/// different scales, and because a good radar seeker should hold a
		/// return an IR seeker would find unremarkable.
		///
		/// Zero is the default, because that is what every prefab written before
		/// this field existed reads, and because it keeps radar seekers working
		/// exactly as they did until someone sets a number.
		/// </remarks>
		public float RadarSignatureThreshold;

		/// <summary>
		/// The range at which this seeker gets a full-strength return, in the
		/// same units as every other distance in the game. Zero disables the
		/// radar test entirely.
		/// </summary>
		/// <remarks>
		/// This is the range term, and it is what radar has that infrared does
		/// not. A heat seeker either sees a plume or does not, and distance
		/// changes nothing about the answer. A radar return weakens with
		/// distance as this over the range, so the same hull is a certain
		/// contact at two kilometres and no contact at all at ten.
		///
		/// Without it every signature in the game was heat, and a radar seeker
		/// could not tell a trade station from a fighter - because a trade
		/// station has no engines to be hot. That is what made radar feel
		/// broken rather than merely weak.
		///
		/// The return is compressed by a quarter power of the cross section,
		/// which is also the reference's shape, and is why a battleship reads as
		/// a few times a fighter on radar rather than a hundred times.
		/// </remarks>
		public float RadarMaxRange;

		public bool IsMine;

		/// <summary>
		/// How this missile finds its target, worst to best: Optical, IR, SARH,
		/// ARH. Decides the warning the player gets, and Optical gets none.
		/// </summary>
		/// <remarks>
		/// Defaults to Optical, so every missile that predates this field is
		/// untyped and nothing changes. The order is the order - a higher value
		/// wins when several missiles are locked on at once.
		/// </remarks>
		public MissileGuidanceType GuidanceType = MissileGuidanceType.Optical;

		public float MaxMoveRange = 200f;

		public float MiningDamageMultiplier = 0.1f;

		public float MoveSpeed = 100f;

		public bool ParentExplosionToTarget;

		public GameObject ProjectilePrefab;

		public bool RotateTowardsTarget = true;

		public bool SetDamageUsingEnergyCost = true;

		public ShieldDamageType ShieldDamageType;

		public float ThrustPower = 1f;

		public float MinThrust = 0.2f;

		public bool TimedDestruction;

		public float TimedExplosionInaccuracy = 30f;

		/// <summary>
		/// How fast the nose slews towards the flight path, in degrees a second.
		/// </summary>
		/// <remarks>
		/// Not how fast the missile turns - that is
		/// <see cref="LateralThrustPower"/> over the speed, and it is much the
		/// slower of the two. This is only the attitude, and it is deliberately
		/// quicker than the path so the missile visibly points where it is going
		/// to be rather than where it is, which is what a vehicle with jets at
		/// the front looks like.
		///
		/// It was three or four times quicker than it should have been when this
		/// was introduced, which put the Lancer's nose round a full circle in
		/// 1.3 seconds while its flight path could not turn through a right
		/// angle in two. A missile pointing hard one way and travelling another
		/// reads as broken rather than as manoeuvring.
		/// </remarks>
		public float TurnRate = 90f;

		/// <summary>
		/// How hard the sideways thrusters push, in the same units as
		/// <see cref="ThrustPower"/>. This is what turns the missile.
		/// </summary>
		/// <remarks>
		/// There is no air in a sector, so a missile cannot bank into a lift
		/// vector and have its nose dragged round behind it. It has small jets,
		/// and turning means firing them sideways and letting the velocity vector
		/// rotate over time while the nose follows it afterwards.
		///
		/// That ordering is the entire point. While the nose led and the thrust
		/// pushed along it, the missile's path was whatever its nose said and a
		/// launcher's inherited velocity was gone in a sixth of a second - there
		/// was nothing left to correct and the missile flew on rails. With the
		/// velocity leading, a missile fired off the side of a fast ship spends
		/// real flight time killing the sideways momentum it was handed, and a
		/// target that turns hard enough to stay outside the steering cone buys
		/// itself distance rather than a re-aim.
		///
		/// Compared with the main thruster it should be much the weaker of the
		/// two, and by a lot. A missile that can turn as fast as it accelerates
		/// is a missile that tracks, and a missile that tracks makes manoeuvring
		/// worthless.
		///
		/// This is the number that was wrong by three or four times over, and
		/// the reason is worth keeping. Set at roughly 0.6 of ThrustPower it gave
		/// every missile a path turn rate around 70 degrees a second, and the
		/// fastest real ships in the game turn at 60 to 78 - so a missile was
		/// out-turning the ship it was chasing and jinking did nothing at all.
		///
		/// The reference's equivalent is a g-limit, and it caps the turn rate at
		/// 9.81 times the g-limit over the speed, which falls as the missile
		/// goes faster: a constant lateral acceleration already turns a path more
		/// slowly the quicker you are going, and that is the whole of the effect.
		/// At their speeds and a g-limit of nine it comes out near 17 degrees a
		/// second, an order of magnitude under what this was set to.
		///
		/// What is wanted instead is a missile that turns slower than the ships
		/// it is chasing, so that a pilot who turns can open the range, and
		/// slower than nothing - a missile still has to beat a battleship, which
		/// turns at 14 degrees a second.
		/// </remarks>
		public float LateralThrustPower = 1f;

		/// <summary>
		/// How long a missile may have nothing at all to fly at before it gives
		/// up and detonates. Zero means it flies until it runs out of fuel.
		/// </summary>
		/// <remarks>
		/// What this replaces is worth recording, because the obvious version of
		/// it detonates every missile in the game on its first frame.
		///
		/// The reference's rule is a speed floor, and it works there because their
		/// motors burn out on fuel - a dead motor is a real state, and a missile
		/// coasting on a dead motor falls through the floor. Ours has no such
		/// state: thrust is modulated between MinThrust and one and never to
		/// zero, so a missile always converges on some fraction of ThrustPower and
		/// never crosses a speed floor for that reason.
		///
		/// On top of that the launch phase is the worst possible place for a speed
		/// test. Guidance does not activate immediately, and a missile spends that
		/// window at MinThrust, so its speed is heading for a fifth of
		/// ThrustPower while any sensible floor sits above that. Every missile
		/// would be below its own floor before it had done anything.
		///
		/// What is actually true here is that a missile can be a rock - fired at a
		/// target it never sees, never lases, or never has a launcher picture on -
		/// and then it has nothing to steer by for the rest of its life. That is
		/// worth ending and it can be asked directly instead of inferred from
		/// speed.
		///
		/// Six seconds is long enough that a missile which briefly loses its
		/// picture - which every seeker does, and which is the entire point of
		/// them - is in no danger, and short enough that a dud does not cruise the
		/// sector for the rest of the engagement still hitting things.
		/// </remarks>
		public float SelfDestructDelay;

		/// <summary>
		/// Seconds after firing before this missile can hit anything at all.
		/// </summary>
		/// <remarks>
		/// Spawn protection, and the reason a missile does not kill the ship that
		/// fired it. One second, which is the reference's own figure for the arming
		/// time and a long time at these speeds - a missile from the fastest ship
		/// in the game has covered a hundred metres by then.
		///
		/// It is not a damage immunity so much as a recognition that a missile is
		/// spawned inside the launcher and has not cleared it yet. The reference
		/// pairs it with a distance test, and so does this: see
		/// <see cref="TangibleRangeFromLauncher"/>. Both have to be satisfied, so
		/// the longer of the two governs - and the distance is measured from the
		/// muzzle rather than from the launcher, or a bay on a large hull would
		/// start out already clear.
		/// </remarks>
		public float TangibleDelay;

		/// <summary>
		/// How far from the launching ship this missile must be before it can
		/// hit anything. Zero means distance is not tested.
		/// </summary>
		/// <remarks>
		/// Both this and <see cref="TangibleDelay"/> have to be satisfied, so the
		/// longer of the two governs. Fifty metres is the reference's figure for
		/// the infrared types and it is a sensible one for a game where a fighter
		/// is about a hundred metres long.
		/// </remarks>
		public float TangibleRangeFromLauncher = 50f;

		/// <summary>
		/// How close the commanded direction has to be to where the missile is
		/// actually going, in degrees, before it stops steering entirely.
		/// </summary>
		/// <remarks>
		/// Once a missile is genuinely on its target there is nothing left for
		/// the thrusters to do, and continuing to correct costs it speed. This is
		/// the reference's reachedOnTarget, and it matters more here than it does
		/// there: with lateral thrusters the missile is always firing something,
		/// and a missile that never stops correcting weaves down its own flight
		/// path and arrives slower than one that commits.
		/// </remarks>
		public float SteeringCommitAngle = 10f;

		/// <summary>
		/// How long the commanded direction has to stay inside
		/// <see cref="SteeringCommitAngle"/> before the missile commits.
		/// </summary>
		/// <remarks>
		/// The hold is what stops a missile committing the instant it clips
		/// inside the angle while it is still swinging past the target on a wide
		/// turn. Two seconds is long enough that a target which crosses the
		/// missile's nose briefly does not get it to give up the approach.
		/// </remarks>
		public float SteeringCommitTime = 2f;

		public bool InheritParentVelocity;

		public float InheritParentVelocityMultiplier = 1f;

		public float InaccuracyMultiplier = 1f;

		/// <summary>
		/// Whether this missile solves for an interception point rather than
		/// flying at where its target is.
		/// </summary>
		/// <remarks>
		/// This is the honest way to author a dumb rocket, and it is the only
		/// flag involved: a missile with it off aims at the ship's present
		/// position and misses by however far the ship travels in the missile's
		/// time of flight, which is why the Small Rocket is the worst weapon in
		/// the game and is still worth carrying for a stationary target.
		///
		/// There used to be a companion range field limiting how far out the
		/// solution was used, with the missile falling back on the ship's
		/// position beyond it. That is gone. A range limit on a lead solution
		/// does not make long shots harder, it makes them impossible - past the
		/// limit the missile was aiming at empty space where the ship had been,
		/// and the further the range the more room the ship had to be somewhere
		/// else. See Missile.SeekDirection.
		/// </remarks>
		public bool MissileIntelligentGuidance = true;

		public float MinTimeBeforeGuidanceActivation = 1f;

		public float MaxTimeBeforeGuidanceActivation = 2f;

		public float MaxDuration;

		public float GetFuzzyDamage()
		{
			return Mathf.Lerp(Damage * (1f - DamageFuzziness), Damage * (1f + DamageFuzziness), Random.value);
		}

		public float GetMissileMaxSpd()
		{
			Rigidbody component = ProjectilePrefab.GetComponent<Rigidbody>();
			return UnitEngineClass.CalculateMaxSpeed(ThrustPower, component.linearDamping, component.mass);
		}
	}
}
