using OpenFrontier.IP.Engine.GasClouds;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Core.Units
{
	public class UnitGasCloud : MonoBehaviour
	{
		public GasCloudClass GasCloudClass;

		[SerializeField]
		private Unit unit;

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

		public void ApplyRadius()
		{
			if (unit == null)
			{
				Init();
			}
			SphereCollider component = GetComponent<SphereCollider>();
			if (component == null)
			{
				Debug.LogError("Expecting to have sphere collider", this);
			}
			component.radius = unit.Radius;
		}

		public void Init()
		{
			if (unit == null)
			{
				unit = GetComponent<Unit>();
			}
		}

		private void OnTriggerEnter(Collider other)
		{
			Unit component = other.GetComponent<Unit>();
			if (component != null && component.Sector == unit.Sector)
			{
				if (LogWrapper.LogMsgs)
				{
					LogWrapper.Log($"Unit {component} entered gas cloud {this}", this, 2);
				}
				component.ChangeGasCloud(this);
			}
		}

		private void OnTriggerExit(Collider other)
		{
			Unit component = other.GetComponent<Unit>();
			if (component != null && component.UnitGasCloud == this)
			{
				if (LogWrapper.LogMsgs)
				{
					LogWrapper.Log($"Unit {component} exited gas cloud {this}", this, 2);
				}
				component.ChangeGasCloud(null);
			}
		}

		public float GetDetectionRangeMultiplier()
		{
			return GasCloudClass.DetectionRangeMultiplier;
		}
	}
}
