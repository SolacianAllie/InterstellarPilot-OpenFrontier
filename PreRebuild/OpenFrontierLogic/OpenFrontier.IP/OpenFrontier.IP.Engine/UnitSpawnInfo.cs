using System;
using System.Collections.Generic;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	[Serializable]
	[RequireComponent(typeof(UnitComponentHolder))]
	public class UnitSpawnInfo : MonoBehaviour
	{
		public List<ComponentBaySpawnData> ComponentBaySpawnData = new List<ComponentBaySpawnData>();

		public float InitialHullNormalized = 1f;

		public float[] ShieldNormalizedValues = new float[6];

		public bool StartWithChargedComponents = true;

		public void Apply()
		{
			UnitComponentHolder component = GetComponent<UnitComponentHolder>();
			Apply(component);
		}

		public void Apply(UnitComponentHolder unitComponents)
		{
			foreach (ComponentBaySpawnData componentBaySpawnDatum in ComponentBaySpawnData)
			{
				if (componentBaySpawnDatum != null && componentBaySpawnDatum.TargetBay != null && componentBaySpawnDatum.TargetBay.InstalledComponent != null)
				{
					componentBaySpawnDatum.TargetBay.InstalledComponent.HealthNormalized = componentBaySpawnDatum.InitialCondition;
				}
			}
			unitComponents.Unit.Destructable.CurrentHealth = InitialHullNormalized * unitComponents.UnitClass.maxHealth;
			if (StartWithChargedComponents)
			{
				ComponentBase[] componentsInChildren = unitComponents.GetComponentsInChildren<ComponentBase>(includeInactive: true);
				for (int i = 0; i < componentsInChildren.Length; i++)
				{
					componentsInChildren[i].RechargeFull();
				}
			}
			if (unitComponents.ShieldComponent != null && ShieldNormalizedValues != null)
			{
				for (int j = 0; j < Math.Min(6, ShieldNormalizedValues.Length); j++)
				{
					unitComponents.ShieldComponent.SetNormalizedShieldPoints(j, ShieldNormalizedValues[j]);
				}
			}
		}
	}
}
