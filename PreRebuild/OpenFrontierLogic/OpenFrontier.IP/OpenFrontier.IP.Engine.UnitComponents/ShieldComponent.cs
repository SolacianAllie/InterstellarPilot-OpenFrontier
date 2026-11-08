using UnityEngine;

namespace OpenFrontier.IP.Engine.UnitComponents
{
	public class ShieldComponent : ComponentBase
	{
		public const int ForwardShieldIndex = 0;

		private const float restoreShieldsInterval = 0.5f;

		private const float timeBetweenShieldUpdates = 3f;

		[SerializeField]
		private float[] currentShieldPoints = new float[6];

		private float lastShieldUpdateTime;

		public ShieldClass ShieldClass;

		private float[] shieldPointRestoreTimes = new float[6];

		private float currentTotalShieldPoints;

		public override ComponentClass ComponentClass => ShieldClass;

		public bool IsFullyCharged => currentTotalShieldPoints >= ShieldClass.TotalCapacity;

		public float CurrentTotalShieldPoints => currentTotalShieldPoints;

		public float GetNormalizedShieldCharge()
		{
			return currentTotalShieldPoints / ShieldClass.TotalCapacity;
		}

		public void SetShieldsPoints(int index, float shieldPoints)
		{
			float num = currentShieldPoints[index];
			if (shieldPoints <= 0f)
			{
				shieldPoints = 0f;
			}
			else if (shieldPoints > ShieldClass.Capacities[index])
			{
				shieldPoints = ShieldClass.Capacities[index];
			}
			currentShieldPoints[index] = shieldPoints;
			if (num >= 0f && IsShieldDepleted(index))
			{
				shieldPointRestoreTimes[index] = CalculateShieldRestoreTime(index);
			}
			UpdateCurrentTotalShieldPoints();
		}

		public void DepleteAllShields()
		{
			for (int i = 0; i < 6; i++)
			{
				DepleteShield(i);
			}
		}

		public void DepleteShield(int i)
		{
			SetShieldsPoints(i, 0f);
		}

		public float GetShieldPoints(int index)
		{
			return currentShieldPoints[index];
		}

		public void SetNormalizedShieldPoints(int index, float value)
		{
			SetShieldsPoints(index, value * ShieldClass.Capacities[index]);
		}

		public void RechargeFull(int index)
		{
			SetNormalizedShieldPoints(index, 1f);
		}

		public void RechargeAllFull()
		{
			for (int i = 0; i < 6; i++)
			{
				SetNormalizedShieldPoints(i, 1f);
			}
		}

		public float GetShieldPointNormalized(int index)
		{
			if (ShieldClass.Capacities[index] > 0f)
			{
				return currentShieldPoints[index] / ShieldClass.Capacities[index];
			}
			return 0f;
		}

		public float CalculateShieldRestoreTime(int index)
		{
			if (ShieldClass.Capacities[index] > 0f)
			{
				float t = Mathf.Clamp01(ShieldClass.Capacities[index] / Engine.GameSettings.MaxShieldRestoreDelayValue);
				float num = Mathf.Lerp(Engine.GameSettings.MinShieldRestoreDelay, Engine.GameSettings.MaxShieldRestoreDelay, t);
				return Time.time + num;
			}
			return 0f;
		}

		public void RestoreShield(int index)
		{
			if (ShieldClass.Capacities[index] > 0f && IsShieldDepleted(index))
			{
				SetShieldsPoints(index, Engine.GameSettings.ShieldRestoreNormalizedValue * ShieldClass.Capacities[index] * ShieldClass.ShieldRestoreMultiplier);
				if (Unit != null && Unit.IsActiveInEngine)
				{
					UnitComponents.PlayShieldsUpAudio();
				}
			}
		}

		public void ChangeShieldPoints(int index, float charge)
		{
			SetShieldsPoints(index, currentShieldPoints[index] + charge);
		}

		public bool IsShieldDepleted(int shieldIndex)
		{
			return currentShieldPoints[shieldIndex] <= 0f;
		}

		public bool IsAnyShieldDamaged()
		{
			return !IsFullyCharged;
		}

		public bool IsShieldDamaged(int shieldIndex)
		{
			return currentShieldPoints[shieldIndex] < ShieldClass.Capacities[shieldIndex];
		}

		public override bool RequiresRecharge()
		{
			return currentTotalShieldPoints < ShieldClass.TotalCapacity;
		}

		protected override void rechargeTick(float elapsedTime)
		{
			if (Faction != null)
			{
				if (lastShieldUpdateTime == 0f)
				{
					lastShieldUpdateTime = Time.time;
				}
				float num = Time.time - lastShieldUpdateTime;
				if (num > 3f)
				{
					RegenerateShields(num);
					RestoreDepletedShields();
					lastShieldUpdateTime = Time.time;
				}
			}
		}

		protected override void init()
		{
			base.init();
			UpdateCurrentTotalShieldPoints();
		}

		protected override void onAttachedToUnit(UnitComponentHolder unit)
		{
			base.onAttachedToUnit(unit);
			unit.ShieldComponent = this;
			unit.InvalidateCombatRating();
		}

		protected override void onDetachedFromUnit(UnitComponentHolder unit)
		{
			base.onDetachedFromUnit(unit);
			unit.ShieldComponent = null;
			unit.InvalidateCombatRating();
		}

		protected override void rechargeFull()
		{
			for (int i = 0; i < 6; i++)
			{
				RechargeFull(i);
			}
		}

		protected override void removeCharge()
		{
			for (int i = 0; i < 6; i++)
			{
				SetShieldsPoints(i, 0f);
			}
		}

		private void UpdateCurrentTotalShieldPoints()
		{
			currentTotalShieldPoints = CalculateCurrentTotalShieldPoints();
			UnitComponentHolder unitComponentHolder = UnitComponents;
			if (unitComponentHolder != null && currentTotalShieldPoints < ShieldClass.TotalCapacity)
			{
				unitComponentHolder.anyComponentRequiresRecharge = true;
			}
		}

		private void RegenerateShields(float elapsedTime)
		{
			float num = 0f;
			GameSettings gameSettings = Engine.GameSettings;
			float normalizedDamage = NormalizedDamage;
			for (int i = 0; i < 6; i++)
			{
				num = ShieldClass.Capacities[i];
				if (num > 0f && !IsShieldDepleted(i) && currentShieldPoints[i] < num)
				{
					float a = num - currentShieldPoints[i];
					float b = gameSettings.ShieldRelativeRegenRate * num * ShieldClass.RegenRateMultiplier * EnergySupply * elapsedTime;
					float num2 = Mathf.Min(Mathf.Min(a, b), UnitComponents.CapacitorCharge / ShieldClass.RegenEnergyCost);
					num2 -= normalizedDamage * gameSettings.ComponentDamageEffectSettings.DamagedShieldRechargeAffect * num2;
					ChangeShieldPoints(i, num2);
					UnitComponents.CapacitorCharge -= num2 * ShieldClass.RegenEnergyCost;
				}
			}
		}

		private void RestoreDepletedShields()
		{
			for (int i = 0; i < 6; i++)
			{
				if (IsShieldDepleted(i) && Time.time > shieldPointRestoreTimes[i])
				{
					RestoreShield(i);
				}
			}
		}

		private float CalculateCurrentTotalShieldPoints()
		{
			float num = 0f;
			for (int i = 0; i < 6; i++)
			{
				num += currentShieldPoints[i];
			}
			return num;
		}

		public float GetTotalCapacity()
		{
			return ShieldClass.TotalCapacity;
		}

		public float GetTotalDepletedShieldPoints()
		{
			return GetTotalCapacity() - currentTotalShieldPoints;
		}
	}
}
