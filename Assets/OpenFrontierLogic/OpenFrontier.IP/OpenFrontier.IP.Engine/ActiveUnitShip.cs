using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class ActiveUnitShip : MonoBehaviour
	{
		private float currentTurn;

		private float desiredTurn;

		private Unit unit;

		private ActiveUnit activeUnit;

		private EngineASX Engine => EngineASX.Instance;

		public Unit Unit
		{
			get
			{
				return unit;
			}
			set
			{
				unit = value;
			}
		}

		public float DesiredTurn
		{
			get
			{
				return desiredTurn;
			}
			set
			{
				desiredTurn = Mathf.Clamp(value, -1f, 1f);
			}
		}

		public float CurrentTurn
		{
			get
			{
				return currentTurn;
			}
			set
			{
				currentTurn = value;
			}
		}

		public void Init()
		{
			activeUnit = GetComponent<ActiveUnit>();
			unit = activeUnit.Unit;
		}

		private void ApplyTurn()
		{
			if (Mathf.Abs(currentTurn) > 0.01f)
			{
				float num = currentTurn * Time.deltaTime;
				Vector3 eulerAngles = unit.transform.localRotation.eulerAngles;
				float z = 0f;
				if (Engine.GameSettings.UnitRollEnabled && activeUnit.LastDistanceFromCamera > 0f && activeUnit.LastDistanceFromCamera < Engine.PerformanceSettings.UnitRollMaxDistance)
				{
					z = ApplyUnitTurnRoll(eulerAngles.z);
				}
				unit.transform.localEulerAngles = new Vector3(0f, eulerAngles.y + num, z);
			}
		}

		private void MoveTurnToDesired()
		{
			float num = desiredTurn * unit.UnitClass.turnRate;
			if (!(Mathf.Abs(num - currentTurn) > 0.001f))
			{
				return;
			}
			float num2 = unit.UnitClass.TurnAcceleration * Time.deltaTime;
			if (currentTurn < num)
			{
				currentTurn += num2;
				if (currentTurn > num)
				{
					currentTurn = num;
				}
			}
			else
			{
				currentTurn -= num2;
				if (currentTurn < num)
				{
					currentTurn = num;
				}
			}
		}

		private float ApplyUnitTurnRoll(float currentAngleZ)
		{
			float num = (0f - currentTurn) / Engine.GameSettings.ActiveUnitMaxRollTurnValue;
			if (Mathf.Abs(num) > 1f)
			{
				num = Mathf.Sign(num);
			}
			float b = num * Engine.GameSettings.ActiveUnitMaxRollAngle;
			return Mathf.Lerp(Maths.WrapValue(currentAngleZ, -180f, 180f), b, Time.deltaTime * Engine.GameSettings.ActiveUnitRollLerp);
		}

		private void FixedUpdate()
		{
			if (unit != null && !unit.IsDestroyed)
			{
				ApplyTurn();
			}
		}

		private void Update()
		{
			if (unit != null && unit.Sector != null && !unit.IsDestroyed && unit.UnitType == UnitType.Ship)
			{
				MoveTurnToDesired();
				if (unit.Components.EngineComponent != null)
				{
					UpdateEngineComponent(unit.Components.EngineComponent);
				}
			}
		}

		public void RemoveForces()
		{
			CurrentTurn = 0f;
			DesiredTurn = 0f;
			unit.ClearZAndXRotation();
		}

		private void UpdateEngineComponent(UnitEngineComponent unitEngine)
		{
			if (unitEngine.IsPoweredAndEnergySupplied && Unit != null && Unit.IsActiveInEngine && Unit.RBody != null)
			{
				unitEngine.ApplyForce();
			}
		}

		internal void OnUnitInactive()
		{
			RemoveForces();
		}
	}
}
