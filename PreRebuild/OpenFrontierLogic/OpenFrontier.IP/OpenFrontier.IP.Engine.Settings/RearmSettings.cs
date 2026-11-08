using UnityEngine;

namespace OpenFrontier.IP.Engine.Settings
{
	public class RearmSettings : MonoBehaviour
	{
		public float MaxRearmPriority = 0.75f;

		public float EquipmentRequiredThreshold = 0.8f;

		public float EstimatedCostPerCargoUnit = 100f;

		public float RearmPriorityThreshold = 0.25f;
	}
}
