using System.Collections.Generic;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class AICombatNavigator : MonoBehaviour
	{
		private AICombatNavigationMode navigationMode;

		private float lastTimeCachedWeapons = float.MinValue;

		private bool hasRearFacingTurrets;

		private int? strongestShieldIndex;

		private Vector3 maintainDistanceVector = Vector3.zero;

		private float combatVectorMaxSetTime = 8f;

		private float combatVectorMinSetTime = 3f;

		private float nextMaintainDistanceVectorTime;

		private NpcPilot npcPilot;

		private float timeInLastCombatRun;

		private float maxTimeSinceCombatRunLower = 2f;

		private float maxTimeSinceCombatRunUpper = 50f;

		private float attackRunRotationYFudgeDegrees;

		private Quaternion avoidRotationYFudge = Quaternion.identity;

		private const float strongestShieldConcentration = 0.75f;

		private const float maxAvoidRandomAngle = 30f;

		private const float minTimeInState = 2f;

		private float minDistanceFromTarget = 50f;

		private float avoidTargetTimeoutDistance = 100f;

		private const float maxAttackRunRotationFudgeDegrees = 6f;

		private const float maintainDistanceMaxDuration = 20f;

		private const float closeDistanceMaxFudgeDistance = 145f;

		private Vector3 closeDistanceFudgeOffset = Vector3.zero;

		private const float maintainDistanceMinDuration = 4f;

		private float maintainDistanceTimeout;

		private float avoidTargetTimeoutTime;

		private float strongestShieldTimeoutTime;

		private const float strongestShieldMaxDuration = 20f;

		private const float strongestShieldMinDuration = 12f;

		private float avoidTargetMaxDuration = 10f;

		private float avoidCooldownTime;

		private float attackRunTimeoutTime;

		private const float minAttackRunDuration = 5f;

		private const float maxAttackRunDuration = 20f;

		private const float avoidCooldownDuration = 10f;

		private float lastStateChangeTime;

		private float lastStrongestShieldCalculation;

		private float randomSpeedMultiplier = 1f;

		private float constantNeedForSpeed = 1f;

		private List<TurretComponent> attackRunTurrets = new List<TurretComponent>(6);

		private float lastCalculatedAttackRunRotationFudge;

		private float nextRandomizeSpeedTime;

		private bool CanChangeState => ElapsedSinceLastStateChangeTime > 2f;

		public float ElapsedSinceLastStateChangeTime => Time.time - lastStateChangeTime;

		public AICombatNavigationMode NavigationMode
		{
			get
			{
				return navigationMode;
			}
			set
			{
				if (navigationMode != value)
				{
					navigationMode = value;
					switch (navigationMode)
					{
					case AICombatNavigationMode.None:
						UpdateCloseDistanceFudge();
						break;
					case AICombatNavigationMode.AvoidTarget:
					{
						avoidTargetTimeoutTime = Time.time + avoidTargetMaxDuration;
						float y = Random.Range(-30f, 30f);
						avoidRotationYFudge = Quaternion.Euler(0f, y, 0f);
						break;
					}
					case AICombatNavigationMode.MaintainPreferredDistance:
						AssignNewMaintainDistanceVector();
						maintainDistanceTimeout = Time.time + Random.Range(4f, 20f);
						break;
					case AICombatNavigationMode.AttackRun:
						CalculateAttackRunRotationFudge();
						attackRunTimeoutTime = Time.time + 20f;
						break;
					case AICombatNavigationMode.StrongestShield:
						strongestShieldTimeoutTime = Time.time + Random.Range(12f, 20f);
						break;
					case AICombatNavigationMode.CloseDistance:
						UpdateCloseDistanceFudge();
						break;
					}
					lastStateChangeTime = Time.time;
				}
			}
		}

		public NpcPilot NpcPilot
		{
			get
			{
				return npcPilot;
			}
			set
			{
				npcPilot = value;
			}
		}

		private float MaxAttackDistanceThreshold
		{
			get
			{
				if (npcPilot.CurrentUnitComponents.LastConventionalTurretCount > 0)
				{
					return NpcPilot.CurrentUnitComponents.HighestRangedTurret * 0.8f;
				}
				return NpcPilot.CurrentUnit.UnitClass.AIMaxAttackDist * 0.75f;
			}
		}

		private float MinAttackDistanceThreshold
		{
			get
			{
				if (npcPilot.CurrentUnitComponents.LastConventionalTurretCount > 0)
				{
					return Mathf.Max(50f, NpcPilot.CurrentUnitComponents.HighestRangedTurret * 0.65f);
				}
				return NpcPilot.CurrentUnit.UnitClass.AIMaxAttackDist * 0.5f;
			}
		}

		private bool CanAvoidTarget
		{
			get
			{
				if (npcPilot.CombatTarget != null)
				{
					return Time.time > avoidCooldownTime;
				}
				return false;
			}
		}

		public Unit CombatTarget => NpcPilot.CombatTarget;

		public EngineASX Engine => NpcPilot.Engine;

		public bool IsAvoidingCombatTarget => NavigationMode == AICombatNavigationMode.AvoidTarget;

		private void CalculateAttackRunRotationFudge()
		{
			lastCalculatedAttackRunRotationFudge = Time.time;
			attackRunRotationYFudgeDegrees = GetAttackRunRotationYFudge();
		}

		public void Init()
		{
			float num = (float)(npcPilot.Person.Seed % 100) / 100f;
			constantNeedForSpeed = 0.7f + num * 0.3f;
		}

		public void Tick()
		{
			if (Time.time > lastTimeCachedWeapons + 10f)
			{
				CacheWeaponStats();
			}
			if (navigationMode != AICombatNavigationMode.None)
			{
				UpdateNavigationMode();
			}
			else
			{
				NavigationMode = GetNewNavigationMode();
			}
			if (npcPilot.CurrentUnit.IsInActiveSector)
			{
				if (Time.time > nextRandomizeSpeedTime)
				{
					AssignRandomSpeed();
				}
			}
			else
			{
				randomSpeedMultiplier = 1f;
			}
		}

		private void AssignRandomSpeed()
		{
			float value = Random.value;
			if (value > 0.9f)
			{
				randomSpeedMultiplier = 0f;
				nextRandomizeSpeedTime = Time.time + Random.Range(2f, 3.5f);
			}
			else if (value > 0.6f)
			{
				randomSpeedMultiplier = 1f;
				nextRandomizeSpeedTime = Time.time + Random.Range(3f, 6f);
			}
			else
			{
				randomSpeedMultiplier = Random.Range(0.5f, 1f);
				nextRandomizeSpeedTime = Time.time + Random.Range(2f, 5f);
			}
		}

		private AICombatNavigationMode GetNewNavigationMode()
		{
			if (IsTooFarAwayFromTarget())
			{
				return AICombatNavigationMode.CloseDistance;
			}
			if (ShouldAvoidTarget())
			{
				return AICombatNavigationMode.AvoidTarget;
			}
			if (!npcPilot.LowCapacitorCooldownActive && attackRunTurrets.Count > 0)
			{
				float num = timeInLastCombatRun + Mathf.Lerp(maxTimeSinceCombatRunUpper, maxTimeSinceCombatRunLower, GetAttackRunNeed());
				if (Time.time > num && AnyAttackRunTurretIsCharged())
				{
					return AICombatNavigationMode.AttackRun;
				}
			}
			if (npcPilot.LowCapacitorCooldownActive || Random.value > npcPilot.Person.Aggression * 0.85f)
			{
				strongestShieldIndex = TryGetStrongestShieldIndex();
				lastStrongestShieldCalculation = Time.time;
				if (strongestShieldIndex.HasValue)
				{
					return AICombatNavigationMode.StrongestShield;
				}
			}
			return AICombatNavigationMode.MaintainPreferredDistance;
		}

		private void UpdateCloseDistanceFudge()
		{
			float num = 30f + Random.value * 145f;
			closeDistanceFudgeOffset = Geometry.RandomXZUnitVector() * num;
		}

		private bool IsHeadingTowardsTarget()
		{
			Vector3 position = npcPilot.Sector.ToWorldPosition(npcPilot.LastKnownCombatTargetSectorPosition);
			if (Vector3.Dot(Vector3.Normalize(npcPilot.CurrentUnitComponents.transform.InverseTransformPoint(position)), Vector3.forward) > 0.5f)
			{
				return true;
			}
			return false;
		}

		private int? TryGetStrongestShieldIndex()
		{
			ShieldComponent shieldComponent = npcPilot.CurrentUnitComponents.ShieldComponent;
			if (shieldComponent != null)
			{
				float num = float.MaxValue;
				float num2 = float.MinValue;
				float num3 = float.MinValue;
				int value = -1;
				float num4 = float.MinValue;
				for (int i = 0; i < 6; i++)
				{
					float shieldPointNormalized = shieldComponent.GetShieldPointNormalized(i);
					float shieldPoints = shieldComponent.GetShieldPoints(i);
					if (shieldPoints > num3)
					{
						num3 = shieldPoints;
					}
					if (shieldPointNormalized > num2)
					{
						num2 = shieldPointNormalized;
					}
					if (shieldPointNormalized < num)
					{
						num = shieldPointNormalized;
					}
					float num5 = 0f;
					for (int j = -1; j <= 1; j++)
					{
						int index = EngineASX.WrapShieldIndex(i + j);
						float shieldPointNormalized2 = shieldComponent.GetShieldPointNormalized(index);
						shieldPointNormalized2 = ((j == 0) ? (shieldPointNormalized2 * 0.75f) : (shieldPointNormalized2 * 0.125f));
						num5 += shieldPointNormalized2;
					}
					num5 /= 3f;
					if (num5 > num4)
					{
						num4 = num5;
						value = i;
					}
				}
				if (num3 > 25f && num < 0.7f && num2 > 0.05f && num2 - num > 0.05f)
				{
					return value;
				}
			}
			return null;
		}

		private void UpdateNavigationMode()
		{
			if (CanChangeState && navigationMode != AICombatNavigationMode.StrongestShield)
			{
				if (IsTooFarAwayFromTarget())
				{
					NavigationMode = AICombatNavigationMode.CloseDistance;
				}
				else if (ShouldAvoidTarget())
				{
					NavigationMode = AICombatNavigationMode.AvoidTarget;
				}
			}
			switch (navigationMode)
			{
			case AICombatNavigationMode.MaintainPreferredDistance:
				if (CanChangeState && Time.time > maintainDistanceTimeout)
				{
					NavigationMode = AICombatNavigationMode.None;
				}
				else if (Time.time > nextMaintainDistanceVectorTime)
				{
					AssignNewMaintainDistanceVector();
				}
				break;
			case AICombatNavigationMode.AvoidTarget:
				if (CanChangeState)
				{
					avoidCooldownTime = Time.time + 10f;
					if (Time.time > avoidTargetTimeoutTime || npcPilot.LastCombatTargetDistance > avoidTargetTimeoutDistance)
					{
						NavigationMode = AICombatNavigationMode.None;
					}
				}
				break;
			case AICombatNavigationMode.CloseDistance:
				if (CanChangeState && NpcPilot.LastCombatTargetDistance < MinAttackDistanceThreshold)
				{
					NavigationMode = AICombatNavigationMode.None;
				}
				break;
			case AICombatNavigationMode.AttackRun:
				timeInLastCombatRun = Time.time;
				if (CanChangeState && (Time.time > attackRunTimeoutTime || (ElapsedSinceLastStateChangeTime > 5f && MinAttackRunDistanceReached()) || !AnyAttackRunTurretIsCharged()))
				{
					NavigationMode = AICombatNavigationMode.None;
				}
				else if (Time.time > lastCalculatedAttackRunRotationFudge + 1f)
				{
					CalculateAttackRunRotationFudge();
				}
				break;
			case AICombatNavigationMode.StrongestShield:
				if (CanChangeState && Time.time > strongestShieldTimeoutTime)
				{
					NavigationMode = AICombatNavigationMode.None;
				}
				else if (Time.time - lastStrongestShieldCalculation > 1f)
				{
					strongestShieldIndex = TryGetStrongestShieldIndex();
					lastStrongestShieldCalculation = Time.time;
					if (!strongestShieldIndex.HasValue)
					{
						NavigationMode = AICombatNavigationMode.None;
					}
				}
				break;
			}
		}

		private bool ShouldAvoidTarget()
		{
			if (npcPilot.Settings.CombatEfficiency < 0.9f)
			{
				if (CanAvoidTarget && IsHeadingTowardsTarget())
				{
					return IsTooCloseToTarget();
				}
				return false;
			}
			return false;
		}

		private bool AnyAttackRunTurretIsCharged()
		{
			foreach (TurretComponent attackRunTurret in attackRunTurrets)
			{
				if ((!attackRunTurret.UsesAmmo || attackRunTurret.HasRequiredAmmo) && attackRunTurret.HasMinimumEnergyCharge)
				{
					return true;
				}
			}
			return false;
		}

		private bool MinAttackRunDistanceReached()
		{
			return npcPilot.LastCombatTargetDistance < NpcPilot.Engine.GameSettings.AiUnitControllerSettings.MinAttackRunDistance;
		}

		private bool IsTooCloseToTarget()
		{
			if (NpcPilot.LastCombatTargetDistance - NpcPilot.CurrentUnit.UnitClass.ShieldRingRadius - NpcPilot.CombatTarget.UnitClass.ShieldRingRadius < minDistanceFromTarget)
			{
				return true;
			}
			return false;
		}

		private bool IsTooFarAwayFromTarget()
		{
			if (NpcPilot.LastCombatTargetDistance > MaxAttackDistanceThreshold)
			{
				return true;
			}
			return false;
		}

		public void ArrivedAtNavpoint()
		{
			if (NavigationMode == AICombatNavigationMode.MaintainPreferredDistance)
			{
				AssignNewMaintainDistanceVector();
			}
		}

		private void AssignNewMaintainDistanceVector()
		{
			float num = Mathf.Lerp(MinAttackDistanceThreshold, MaxAttackDistanceThreshold, 0.5f);
			maintainDistanceVector = Geometry.RandomXZUnitVector() * num;
			nextMaintainDistanceVectorTime = Time.time + Random.Range(combatVectorMinSetTime, combatVectorMaxSetTime);
		}

		private float GetAttackRunRotationYFudge()
		{
			float normalizedValue = 0f;
			float normalizedValue2 = 0f;
			int strongestOfForwardShields = NpcPilot.CurrentUnit.GetStrongestOfForwardShields(out normalizedValue);
			NpcPilot.CurrentUnit.GetWeakestOfForwardShield(out normalizedValue2);
			float num = Random.Range(-6f, 6f);
			if (strongestOfForwardShields != 0 && Mathf.Min(normalizedValue, normalizedValue2) < 0.5f && Mathf.Abs(normalizedValue - normalizedValue2) > 0.05f)
			{
				switch (strongestOfForwardShields)
				{
				case 1:
					return -60f + num;
				case 5:
					return 60f + num;
				}
			}
			return num;
		}

		public void SetNavPointFromCombat(ref NpcPilotNavpoint navpoint)
		{
			navpoint.Arrive = false;
			navpoint.ThrottleDotThreshold = 0f;
			navpoint.ArrivalThreshold = 50f;
			switch (navigationMode)
			{
			case AICombatNavigationMode.AvoidTarget:
			{
				Vector3 vector2 = Vector3.Normalize(NpcPilot.CurrentUnit.SectorPosition - NpcPilot.LastKnownCombatTargetSectorPosition) * 1000f;
				vector2 = avoidRotationYFudge * vector2;
				navpoint.SectorPosition = NpcPilot.CurrentUnit.SectorPosition + vector2;
				break;
			}
			case AICombatNavigationMode.AttackRun:
			{
				Vector3 sectorPosition2 = npcPilot.CurrentUnit.SectorPosition;
				Vector3 vector3 = NpcPilot.LastKnownCombatTargetSectorPosition - sectorPosition2;
				if (npcPilot.CurrentUnit.IsInActiveSector)
				{
					Quaternion quaternion2 = Quaternion.Euler(0f, attackRunRotationYFudgeDegrees * 0.4f, 0f);
					navpoint.SectorPosition = sectorPosition2 + quaternion2 * vector3;
				}
				else
				{
					navpoint.SectorPosition = npcPilot.LastKnownCombatTargetSectorPosition - vector3.normalized * Random.Range(50f, 150f);
				}
				break;
			}
			case AICombatNavigationMode.None:
			case AICombatNavigationMode.CloseDistance:
				navpoint.SectorPosition = NpcPilot.LastKnownCombatTargetSectorPosition + closeDistanceFudgeOffset;
				break;
			case AICombatNavigationMode.MaintainPreferredDistance:
				navpoint.SectorPosition = NpcPilot.LastKnownCombatTargetSectorPosition + maintainDistanceVector;
				break;
			case AICombatNavigationMode.StrongestShield:
				if (strongestShieldIndex.HasValue)
				{
					Vector3 sectorPosition = npcPilot.CurrentUnit.SectorPosition;
					Vector3 vector = Vector3.Normalize(NpcPilot.LastKnownCombatTargetSectorPosition - sectorPosition);
					float num = (float)strongestShieldIndex.Value / 6f * 360f;
					Quaternion quaternion = Quaternion.Euler(0f, 0f - num, 0f);
					navpoint.SectorPosition = sectorPosition + quaternion * vector * 1000f;
				}
				break;
			}
		}

		public float GetAttackRunNeed()
		{
			if (!hasRearFacingTurrets)
			{
				return 1f;
			}
			if (Random.value > npcPilot.Person.Aggression * 0.8f)
			{
				return GetMaxForwardShieldNormallized();
			}
			return 1f;
		}

		private float GetMaxForwardShieldNormallized()
		{
			ShieldComponent shieldComponent = npcPilot.CurrentUnitComponents.ShieldComponent;
			if (shieldComponent != null)
			{
				float num = float.MinValue;
				for (int i = -1; i <= 1; i++)
				{
					int index = EngineASX.WrapShieldIndex(i);
					float shieldPointNormalized = shieldComponent.GetShieldPointNormalized(index);
					if (shieldPointNormalized > num)
					{
						num = shieldPointNormalized;
					}
				}
				return num;
			}
			return float.MinValue;
		}

		public float GetPreferredThrottle()
		{
			if (npcPilot.LowCapacitorCooldownActive)
			{
				return Mathf.Lerp(0.15f, 0.6f, constantNeedForSpeed) * randomSpeedMultiplier;
			}
			switch (navigationMode)
			{
			case AICombatNavigationMode.AttackRun:
				if (npcPilot.LastCombatTargetDistance < 150f)
				{
					return Mathf.Lerp(0.15f, 0.9f, constantNeedForSpeed) * randomSpeedMultiplier;
				}
				return Mathf.Lerp(0.8f, 1f, constantNeedForSpeed);
			case AICombatNavigationMode.AvoidTarget:
				return 1f;
			case AICombatNavigationMode.CloseDistance:
				return 1f;
			case AICombatNavigationMode.StrongestShield:
				return Mathf.Lerp(0.25f, 1f, constantNeedForSpeed) * randomSpeedMultiplier;
			case AICombatNavigationMode.MaintainPreferredDistance:
				_ = NpcPilot.CombatTarget != null;
				return Mathf.Lerp(0.6f, 1f, constantNeedForSpeed) * randomSpeedMultiplier;
			default:
				return 0.75f;
			}
		}

		public void CacheWeaponStats()
		{
			lastTimeCachedWeapons = Time.time;
			CompileTurretStats();
		}

		private void CompileTurretStats()
		{
			attackRunTurrets.Clear();
			hasRearFacingTurrets = false;
			foreach (TurretComponent turret in npcPilot.CurrentUnitComponents.Turrets)
			{
				if (turret.TurretClass.AIConventionalWeapon)
				{
					if (turret.IsLocalDirectionInFiringArc(Vector3.forward))
					{
						attackRunTurrets.Add(turret);
					}
					if (!hasRearFacingTurrets && (!turret.UsesAmmo || turret.HasRequiredAmmo) && turret.IsLocalDirectionInFiringArc(Vector3.back))
					{
						hasRearFacingTurrets = true;
					}
				}
			}
		}
	}
}
